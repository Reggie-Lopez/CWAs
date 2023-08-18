using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System.Activities;
using Microsoft.Xrm.Sdk.Query;
using System.Diagnostics;
using System.IdentityModel.Metadata;
using System.Workflow.Runtime.Tracking;

namespace MCSC.CWA.GetSystemSettingRecord
{
    public class GetSystemSettingRecord : CodeActivity
    {
        [Input("System Setting Name")]
        [RequiredArgument]
        public InArgument<string> SystemSettingName { get; set; }

        [Output("System Setting Record")]
        [ReferenceTarget("som_systemsetting")]
        public OutArgument<EntityReference> SystemSettingRecord { get; set; }

        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var service = executionContext.GetExtension<IOrganizationServiceFactory>().CreateOrganizationService(context.UserId);
            var __trace = executionContext.GetExtension<ITracingService>();

            try
            {
                var userId = context.UserId;
                var sysSettingName = SystemSettingName.Get(executionContext) ?? "";

                if (string.IsNullOrEmpty(sysSettingName)) throw new InvalidPluginExecutionException("All of the Input Parameters have not been set on the workflow step.");

                //using the 'sysSettingName', get the system setting record
                var sysSettingRec = FindSystemSettingRecord(service, sysSettingName);

                //return system setting record
                SystemSettingRecord.Set(executionContext, sysSettingRec);
                return;

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

        private EntityReference FindSystemSettingRecord(IOrganizationService service, string sysSettingName)
        {
            var fetchQueryForVal = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' no-lock='true' distinct='false'>
                                        <entity name='som_systemsetting'>
                                            <attribute name='som_systemsettingid' />
                                            <filter><condition attribute='som_name' operator='eq' value='{0}' /></filter>                                               
                                        </entity>
                                    </fetch>";

            EntityCollection retSysSetting = service.RetrieveMultiple(new FetchExpression(string.Format(fetchQueryForVal, sysSettingName)));
            if (retSysSetting != null)
            {
                if (retSysSetting.Entities != null)
                {
                    if (retSysSetting.Entities.Any())
                    {
                        var val = retSysSetting.Entities[0].ToEntityReference();
                        return val;
                    }
                }
            }
            return null;
        }

    }
}