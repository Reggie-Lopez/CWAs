using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System.Activities;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.CWA.GetSystemSettingValue
{
    public class GetSystemSettingValue : CodeActivity
    {
        [Input("System Setting Name")]
        [RequiredArgument]
        public InArgument<string> SystemSettingName { get; set; }

        [Input("'Value1' or 'Value2'")]
        [RequiredArgument]
        public InArgument<string> Value1OrValue2 { get; set; }

        [Output("System Setting Value")]
        public OutArgument<string> SystemSettingValue { get; set; }

        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var service = executionContext.GetExtension<IOrganizationServiceFactory>().CreateOrganizationService(context.UserId);

            try
            {
                var userId = context.UserId;
                var sysSettingName = SystemSettingName.Get(executionContext) ?? "";
                var val1OrVal2 = Value1OrValue2.Get(executionContext) ?? "";

                if (string.IsNullOrEmpty(sysSettingName) || string.IsNullOrEmpty(val1OrVal2)) throw new InvalidPluginExecutionException("All of the Input Parameters have not been set on the workflow step.");

                //using the 'sysSettingName', get the value of the system setting record
                var sysSettingVal = FindSystemSettingRecord(service, sysSettingName, val1OrVal2);

                //return either the value1 or value2 of the systems etting record
                SystemSettingValue.Set(executionContext, sysSettingVal);
                return;

            }
            catch (Exception ex)
            {
                service.Create(new Entity("som_logentry")
                {
                    ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ["som_name"] = ex.Message,
                    ["som_details"] = ex.StackTrace,
                    ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                    ["som_recordlogicalname"] = $"systemuser",
                    ["som_recordid"] = $"{context?.UserId}",
                });

                return;
            }
        }

        private string FindSystemSettingRecord(IOrganizationService service, string sysSettingName, string val1OrVal2)
        {
            var val1OrVal2Schema = "som_" + val1OrVal2;
            var fetchQueryForVal = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' no-lock='true' distinct='false'>
                                        <entity name='som_systemsetting'>
                                            <attribute name='som_systemsettingid' />
                                            <attribute name='{1}' />
                                            <filter><condition attribute='som_name' operator='eq' value='{0}' /></filter>                                               
                                        </entity>
                                    </fetch>";

            EntityCollection retSysSetting = service.RetrieveMultiple(new FetchExpression(string.Format(fetchQueryForVal, sysSettingName, val1OrVal2Schema.ToLower())));
            if (retSysSetting != null)
            {
                if (retSysSetting.Entities != null)
                {
                    if (retSysSetting.Entities.Any())
                    {
                        var val = retSysSetting.Entities[0].GetAttributeValue<string>(val1OrVal2Schema.ToLower());
                        return val;
                    }
                }
            }
            return null;
        }
            
    }
}