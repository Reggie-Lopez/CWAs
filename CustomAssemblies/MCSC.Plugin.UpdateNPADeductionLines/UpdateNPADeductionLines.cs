using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.UpdateNPADeductionLines
{
    public class UpdateNPADeductionLines : IPlugin
    {
        ITracingService _trace;
        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;
        public void Execute(IServiceProvider serviceProvider)
        {
            _trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);
            var __trace = executionContext.GetExtension<ITracingService>();
            
            try
            {
                Entity target = new Entity();
                Entity targetPre = new Entity();
               
                if (context.PostEntityImages.Contains("PostImage") && context.PostEntityImages["PostImage"] is Entity)
                {
                    target = (Entity)context.PostEntityImages["PostImage"];
                    targetPre = (Entity)context.PreEntityImages["PreImage"];
                    if (target == null) return;
                }

                var statusReason = target.GetAttributeValue<OptionSetValue>("statuscode");
                var addedToSpreadsheet = target.GetAttributeValue<bool>("som_addedtospreadsheet");


                //186690003 = processed
                if (statusReason.Value == 186690003)
                {                   
                    //set 'selected' field of the child NPA deduction lines to whatever the 'added to spreadsheet' value is
                    UpdateLines(service, target.Id, addedToSpreadsheet);  
                }
            }
            catch (Exception ex)
            {
                //create new instance of IOrganizationService
                var _service = executionContext.GetExtension<IOrganizationServiceFactory>().CreateOrganizationService(context.UserId);


                _service.Create(new Entity("som_logentry")
                {
                    ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ["som_name"] = ex.Message,
                    ["som_details"] = ex.StackTrace,
                    ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                    ["som_recordlogicalname"] = $"systemuser",
                    ["som_recordid"] = $"{context?.UserId}",
                });

                __trace.Trace("Entering catch block.");
                __trace.Trace(ex.ToString());
                __trace.Trace("Severity: " + LOG_ENTRY_SEVERITY_ERROR.ToString());
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        private void UpdateLines(IOrganizationService service, Guid id, bool addedToSpreadsheet)
        {
            ConditionExpression cond = new ConditionExpression();
            cond.AttributeName = "som_npagpa";
            cond.Operator = ConditionOperator.Equal;
            cond.Values.Add(id);

            FilterExpression filter = new FilterExpression();
            filter.Conditions.Add(cond);

            QueryExpression query = new QueryExpression("som_npadeductionline");
            query.ColumnSet.AddColumns("som_npadeductionlineid");
            query.Criteria.AddFilter(filter);

            var npaDeductionLines = service.RetrieveMultiple(query)?.Entities?.ToList() ?? new List<Entity>();

            foreach (var npaLine in npaDeductionLines)
            {
                npaLine["som_selected"] = addedToSpreadsheet;
                service.Update(npaLine);
            }
            return;
        }
    }
}
