using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            _trace.Trace("Initializing services and context.");

            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);

            _trace.Trace("Entering try block.");

            try
            {
                var target = (Entity)context.InputParameters?["Target"];
                if (target == null) return;

                _trace.Trace("Retrieving target entity.");
                var caseRecord = service.Retrieve(target.LogicalName, target.Id, new ColumnSet("primarycontactid", "som_casetype"));

                var contactRef = caseRecord.GetAttributeValue<EntityReference>("primarycontactid");
                var caseTypeRef = caseRecord.GetAttributeValue<EntityReference>("som_casetype");

                if (caseTypeRef == null) return; //return if the case type is empty
                if (contactRef == null) return; //return if the contact is empty

                _trace.Trace("Validating case type and contact.");
                var contact = service.Retrieve(contactRef.LogicalName, contactRef.Id, new ColumnSet("som_processlevel"));
                var caseType = service.Retrieve("som_casetype", caseTypeRef.Id, new ColumnSet("som_name"));
                var caseTypeName = caseType.GetAttributeValue<string>("som_name");
                var processLevel = contact.GetAttributeValue<string>("som_processlevel");

                _trace.Trace("Retrieving contact and case type details.");
                var excludeProcessLevels = new List<string>()
                {

                };

                if (excludeProcessLevels.Contains(processLevel)) return;

                _trace.Trace("Checking exclusion rules.");
                var caseTypeNames = new List<string>()
                {
                    "LoA",
                    "Workers' Comp",
                    "Appeal"
                };

                if (!caseTypeNames.Contains(caseTypeName)) return;

                _trace.Trace("Build Case Assignment Query");
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

                _trace.Trace("Retreiving Case Assignments");
                var caseAssignments = service.RetrieveMultiple(caseAssignmentQuery)?.Entities;

                _trace.Trace("Evaluate Case Assignments Retreived for a single record");
                // If only one case assignment record is found, assign the case to the assignee
                if (caseAssignments != null && caseAssignments.Count == 1)
                {
                    var caseUpdate = new Entity(target.LogicalName, target.Id);
                    caseUpdate["ownerid"] = caseAssignments[0].GetAttributeValue<EntityReference>("som_assigneeid");
                    service.Update(caseUpdate);

                    return;
                }


                _trace.Trace("If multiple case assignments retrieved, Evaluate Teams");
                // If more than one case assignment record is found, check if any of them is a team
                var teamAssignments = caseAssignments?.Where(x => x.GetAttributeValue<EntityReference>("som_assigneeid").LogicalName == "team");


                _trace.Trace("If team found, assign to team");
                if (teamAssignments != null && teamAssignments.Any())
                {
                    // If a team is found, assign the case to the first team
                    var caseUpdate = new Entity(target.LogicalName, target.Id);
                    caseUpdate["ownerid"] = teamAssignments.First().GetAttributeValue<EntityReference>("som_assigneeid");
                    service.Update(caseUpdate);
                }
                else
                {
                    _trace.Trace("No Team found, assigning to user based on capacity");
                    // If no team is found, use the existing capacity logic to assign the case to a user
                    var caseWorkerCases = caseAssignments?.Where(x => x.GetAttributeValue<EntityReference>("som_assigneeid").LogicalName == "systemuser");

                    var caseWorkers = caseWorkerCases?.GroupBy(x => x.Id)?.Select(x => new
                    {
                        EntityObject = x.FirstOrDefault(),
                        CapacityAvailable = x.FirstOrDefault().GetAttributeValue<int>("som_capacity") - x.Count(),
                    });

                    var caseWorker = caseWorkers?.OrderByDescending(x => x.CapacityAvailable)?.Select(x => x.EntityObject)?.FirstOrDefault();


                    _trace.Trace("Assigning to Case Worker");
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
                _trace.Trace("CheckUserHasRoles: Exception caught");
                _trace.Trace("Entering catch block.");
                _trace.Trace(ex.ToString());
                _trace.Trace("Severity: " + LOG_ENTRY_SEVERITY_ERROR.ToString());
                _trace.Trace("Creating log entry");

                // Get the service factory
                var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Create new instance of IOrganizationService
                var logService = serviceFactory.CreateOrganizationService(context.UserId);

                logService.Create(new Entity("som_logentry")
                {
                    ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ["som_name"] = ex.Message,
                    ["som_details"] = ex.StackTrace,
                    ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                    ["som_recordlogicalname"] = $"systemuser",
                    ["som_recordid"] = $"{context?.UserId}",
                });

                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
