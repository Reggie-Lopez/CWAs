using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.AssignCaseToCaseWorker
{
    public class AssignCaseToCaseWorker : IPlugin
    {
        ITracingService _trace;
        public void Execute(IServiceProvider serviceProvider)
        {
            _trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);

            var target = (Entity)context.InputParameters?["Target"];
            if (target == null) return;

            var caseRecord = service.Retrieve(target.LogicalName, target.Id, new ColumnSet("primarycontactid", "som_casetype"));

            var contactRef = caseRecord.GetAttributeValue<EntityReference>("primarycontactid");
            var contact = service.Retrieve(contactRef.LogicalName, contactRef.Id, new ColumnSet("som_processlevel"));
            var processLevel = contact.GetAttributeValue<string>("som_processlevel");

            var excludeProcessLevels = new List<string>()
            {

            };

            if (excludeProcessLevels.Contains(processLevel)) return;

            var caseTypeRef = caseRecord.GetAttributeValue<EntityReference>("som_casetype");
            var caseType = service.Retrieve(contactRef.LogicalName, contactRef.Id, new ColumnSet("som_name"));
            var caseTypeName = caseType.GetAttributeValue<string>("som_name");

            var caseTypeNames = new List<string>()
            {
                "Leave of Absense",
                "Workers' Compensation"
            };

            if (!caseTypeName.Contains(caseTypeName)) return;

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
                                new ConditionExpression("som_casetype", ConditionOperator.Equal, caseType.Id),
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

            IEnumerable<dynamic> caseWorkers = caseWorkerCases?.GroupBy(x => x.Id)?.Select(x => new
            {
                LogicalName = x.FirstOrDefault().LogicalName,
                Id = x.Key,
                TotalCapacity = x.FirstOrDefault().GetAttributeValue<int>("som_capacity"),
                CapacityAvailable = x.FirstOrDefault().GetAttributeValue<int>("som_capacity") - x.Count(),
                CaseCount = x.Count()
            });

            var caseWorker = caseWorkers?.OrderByDescending(x => x.CapacityAvailable)?.FirstOrDefault();

            var caseUpdate = new Entity(caseRecord.LogicalName, caseRecord.Id)
            {
                ["ownerid"] = new EntityReference(caseWorker.LogicalName, caseWorker.Id)
            };

            try
            {
                service.Update(caseUpdate);
                caseWorker.CapacityAvailable--;
            }
            catch (Exception ex)
            {
                //TODO: Error Handling
            }
        }
    }
}
