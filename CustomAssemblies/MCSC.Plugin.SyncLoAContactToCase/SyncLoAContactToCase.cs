using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.SyncLoAContactToCase
{
    public class SyncLoAContactToCase : IPlugin
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

                var caseRecord = service.Retrieve(target.LogicalName, target.Id, new ColumnSet("primarycontactid"));

                var contact = caseRecord.GetAttributeValue<EntityReference>("primarycontactid");
                if (contact == null) return;

                var query = new QueryExpression("som_leaveofabsense")
                {
                    ColumnSet = new ColumnSet(false),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("som_case", ConditionOperator.Equal, caseRecord.Id),
                            new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                        }
                    }
                };

                var loas = service.RetrieveMultiple(query)?.Entities?.ToList() ?? new List<Entity>();

                foreach (var loa in loas)
                {
                    try
                    {
                        var loaUpdate = new Entity(loa.LogicalName, loa.Id)
                        {
                            ["som_contact"] = contact
                        };

                        service.Update(loaUpdate);
                    }
                    catch (Exception ex)
                    {
                        service.Create(new Entity("som_logentry")
                        {
                            ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                            ["som_name"] = ex.Message,
                            ["som_details"] = ex.StackTrace,
                            ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                            ["som_recordlogicalname"] = $"{loa.LogicalName}",
                            ["som_recordid"] = $"{loa.Id}",
                        });
                    }
                }
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
