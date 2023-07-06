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

                var caseWorkerCaseQuery = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet("som_capacity"),
                    LinkEntities =
                    {
                        new LinkEntity("systemuser", "som_caseassignment", "systemuserid", "som_assigneeid", JoinOperator.Inner)
                        {
                            Columns = new ColumnSet(false),
                            LinkCriteria = new FilterExpression(LogicalOperator.And)
                            {
                                Conditions =
                                {
                                    new ConditionExpression("som_processlevel", ConditionOperator.Equal, processLevel),
                                    new ConditionExpression("som_casetype", ConditionOperator.Equal, caseTypeRef.Id),
                                }
                            }
                        },
                        new LinkEntity("systemuser", "incident", "systemuserid", "ownerid", JoinOperator.LeftOuter)
                        {
                            Columns = new ColumnSet(false),
                            LinkCriteria = new FilterExpression(LogicalOperator.And)
                            {
                                Conditions =
                                {
                                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                }
                            }
                        }
                    }
                }; 
                var caseWorkerCases = service.RetrieveMultiple(caseWorkerCaseQuery)?.Entities;

                var caseWorkers = caseWorkerCases?.GroupBy(x => x.Id)?.Select(x => new
                {
                    EntityObject = x.FirstOrDefault(),
                    CapacityAvailable = x.FirstOrDefault().GetAttributeValue<int>("som_capacity") - x.Count(),
                });

                var caseWorker = caseWorkers?.OrderByDescending(x => x.CapacityAvailable)?.Select(x => x.EntityObject)?.FirstOrDefault();

                var caseUpdate = new Entity(target.LogicalName, target.Id);
                caseUpdate["ownerid"] = new EntityReference(caseWorker.LogicalName, caseWorker.Id);
                service.Update(caseUpdate);               

            }
            catch (Exception ex)
            {
                service.Create(new Entity("som_logentry")
                {
                    ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ["som_name"] = ex.Message,
                    ["som_details"] = ex.StackTrace,
                    ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                    ["som_recordlogicalname"] = $"{context?.PrimaryEntityName}",
                    ["som_recordid"] = $"{context?.PrimaryEntityId}",
                });
            }
        }
    }
}
