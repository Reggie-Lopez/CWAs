using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.AssignCaseToCaseWorker
{
    public class AssignCaseToCaseWorker : IPlugin
    {
        ITracingService _trace;
        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;
        public void Execute(IServiceProvider serviceProvider)
        {
            _trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);

            try
            {
                var target = (Entity)context.InputParameters?["Target"];
                if (target == null) return;

                var caseRecord = service.Retrieve(target.LogicalName, target.Id, new ColumnSet("primarycontactid", "som_casetype"));

                var contactRef = caseRecord.GetAttributeValue<EntityReference>("primarycontactid");
                var caseTypeRef = caseRecord.GetAttributeValue<EntityReference>("som_casetype");

                if (caseTypeRef == null) return; //return if the case type is empty
                if (contactRef == null) return; //return if the contact is empty

                var contact = service.Retrieve(contactRef.LogicalName, contactRef.Id, new ColumnSet("som_processlevel"));
                var caseType = service.Retrieve("som_casetype", caseTypeRef.Id, new ColumnSet("som_name"));
                var caseTypeName = caseType.GetAttributeValue<string>("som_name");
                var processLevel = contact.GetAttributeValue<string>("som_processlevel");

                var excludeProcessLevels = new List<string>()
                {

                };

                if (excludeProcessLevels.Contains(processLevel)) return;

                var caseTypeNames = new List<string>()
                {
                    "LoA",
                    "Workers' Comp",
                    "Appeal"
                };

                if (!caseTypeNames.Contains(caseTypeName)) return;


                var caseAssignmentQuery = new QueryExpression("som_caseassignment")
                {
                    ColumnSet = new ColumnSet("som_assigneeid"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                {
                    new ConditionExpression("som_processlevel", ConditionOperator.Equal, processLevel),
                    new ConditionExpression("som_casetype", ConditionOperator.Equal, caseTypeRef.Id),
                }
                    }
                };

                var caseAssignments = service.RetrieveMultiple(caseAssignmentQuery)?.Entities;

                // If only one case assignment record is found, assign the case to the assignee
                if (caseAssignments != null && caseAssignments.Count == 1)
                {
                    var caseUpdate = new Entity(target.LogicalName, target.Id);
                    caseUpdate["ownerid"] = caseAssignments[0].GetAttributeValue<EntityReference>("som_assigneeid");
                    service.Update(caseUpdate);

                    return;
                }



                // If more than one case assignment record is found, check if any of them is a team
                var teamAssignments = caseAssignments?.Where(x => x.GetAttributeValue<EntityReference>("som_assigneeid").LogicalName == "team");

                if (teamAssignments != null && teamAssignments.Any())
                {
                    // If a team is found, assign the case to the first team
                    var caseUpdate = new Entity(target.LogicalName, target.Id);
                    caseUpdate["ownerid"] = teamAssignments.First().GetAttributeValue<EntityReference>("som_assigneeid");
                    service.Update(caseUpdate);
                }
                else
                {
                    // If no team is found, use the existing capacity logic to assign the case to a user
                    var caseWorkerCases = caseAssignments?.Where(x => x.GetAttributeValue<EntityReference>("som_assigneeid").LogicalName == "systemuser");

                    var caseWorkers = caseWorkerCases?.GroupBy(x => x.Id)?.Select(x => new
                    {
                        EntityObject = x.FirstOrDefault(),
                        CapacityAvailable = x.FirstOrDefault().GetAttributeValue<int>("som_capacity") - x.Count(),
                    });

                    var caseWorker = caseWorkers?.OrderByDescending(x => x.CapacityAvailable)?.Select(x => x.EntityObject)?.FirstOrDefault();

                    if (caseWorker != null)
                    {
                        var caseUpdate = new Entity(target.LogicalName, target.Id);
                        caseUpdate["ownerid"] = new EntityReference(caseWorker.LogicalName, caseWorker.Id);
                        service.Update(caseUpdate);
                    }
                }
            }
            catch (Exception ex)
            {
                // Write exception details to the plugin trace log
                _trace.Trace($"Exception: {ex.Message} \n{ex.StackTrace}");

                // Re-throw the exception to ensure it's caught by Dynamics 365
                throw;
            }
        }
    }
}
