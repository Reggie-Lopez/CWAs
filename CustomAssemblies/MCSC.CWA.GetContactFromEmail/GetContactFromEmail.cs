using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System.Activities;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.CWA.GetContactFromEmail
{
    public class GetContactFromEmail : CodeActivity
    {
        [Input("Email")]
        [RequiredArgument]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> Email { get; set; }

        [Input("Field To Search (subject, body) ")]
        [RequiredArgument]
        public InArgument<string> FieldToSearch { get; set; }

        [Input("Identifier")]
        [RequiredArgument]
        public InArgument<string> Identifier { get; set; }

        [Input("Number of Chars")]
        [RequiredArgument]
        public InArgument<string> NumberOfChars { get; set; }

        [Output("Found Contact")]
        [ReferenceTarget("contact")]
        public OutArgument<EntityReference> FoundContact { get; set; }

        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var service = executionContext.GetExtension<IOrganizationServiceFactory>().CreateOrganizationService(context.UserId);

            try
            {
                var userId = context.UserId;
                var fieldToSearch = FieldToSearch.Get(executionContext) ?? "";
                var identifier = Identifier.Get(executionContext) ?? "";
                var numChars = NumberOfChars.Get(executionContext) ?? "-1";
                var email = Email.Get(executionContext) ?? null;

                if (string.IsNullOrEmpty(fieldToSearch) || string.IsNullOrEmpty(identifier) || numChars == "-1" || email == null) throw new InvalidPluginExecutionException("All of the Input Parameters have not been set on the workflow step.");

                //using the 'field to search' and the email, get the value of the field
                var fieldToSearchValue = GetValueOfFieldToSearch(service, email, fieldToSearch);
                if (string.IsNullOrEmpty(fieldToSearchValue)) throw new InvalidPluginExecutionException("The 'Field To Search' provided is invalid or no Email found.");

                //using the 'identifier', get the employee id
                var splFieldValue = fieldToSearchValue.Split(new string[] { identifier }, StringSplitOptions.None);

                //check that the identifier is actually found in the field's value
                if (splFieldValue.Length == 1) throw new InvalidPluginExecutionException("The 'Identifier' value does not exist in the string for the field specified.");

                //use string manipulation by using the split string and number of characters to get the employee id 
                var actualValWanted = splFieldValue[1].TrimStart().Substring(0, int.Parse(numChars));

                // search for a contact using that employee id
                var getContactFromVal = GetContact(service, actualValWanted);
                if (getContactFromVal == null) throw new InvalidPluginExecutionException("No Contact found from the value in the field's specified.");

                //return the ER of that contact
                FoundContact.Set(executionContext, getContactFromVal);
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

        private EntityReference GetContact(IOrganizationService service, string actualValWanted)
        {
            var fetchQueryForVal = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' no-lock='true' distinct='false'>
                                        <entity name='contact'>
                                            <attribute name='contactid' />
                                            <attribute name='fullname' />
                                            <filter><condition attribute='som_eid' operator='eq' value='{0}' /></filter>                                               
                                        </entity>
                                    </fetch>";

            EntityCollection retSysSetting = service.RetrieveMultiple(new FetchExpression(string.Format(fetchQueryForVal, actualValWanted)));
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

        private string GetValueOfFieldToSearch(IOrganizationService service, EntityReference email, string fieldToSearch)
        {
            string attributeToUse = null;
            switch (fieldToSearch.ToLower())
            {
                case "subject":
                    attributeToUse = "subject";
                    break;
                case "body":
                    attributeToUse = "body";
                    break;
                default:
                    break;
            }
            if (string.IsNullOrEmpty(attributeToUse)) return null;

            var fetchQueryForVal = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' no-lock='true' distinct='false'>
                                        <entity name='email'>
                                            <attribute name='{0}' />
                                            <filter><condition attribute='activityid' operator='eq' value='{1}' /></filter>                                               
                                        </entity>
                                    </fetch>";

            EntityCollection retSysSetting = service.RetrieveMultiple(new FetchExpression(string.Format(fetchQueryForVal, attributeToUse, email.Id)));
            if (retSysSetting != null)
            {
                if (retSysSetting.Entities != null)
                {
                    if (retSysSetting.Entities.Any())
                    {
                        var val = retSysSetting.Entities[0].GetAttributeValue<string>(attributeToUse);                        
                        return val;
                    }
                }
            }
            return null;
        }
    }
}