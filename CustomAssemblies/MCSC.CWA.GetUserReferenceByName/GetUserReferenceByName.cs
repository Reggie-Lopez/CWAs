using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.CWA.GetUserReferenceByName
{
    public partial class GetUserReferenceByName : BaseWorkflow
    {
        //Accept full name string as a required input parameter
        [RequiredArgument]
        [Input("Full Name")]
        public InArgument<string> FullName { get; set; }

        //Return the user reference
        [Output("User Reference")]
        [ReferenceTarget("systemuser")]
        public OutArgument<EntityReference> UserReference { get; set; }
        
        protected override void ExecuteInternal(LocalWorkflowContext context)
        {
            //Get the full name from the input parameter
            string fullName = FullName.Get(context.CodeActivityContext);
            if (fullName != null)
            {
                  //Query the systemuser entity for the user with the specified full name
                QueryExpression query = new QueryExpression("systemuser");
                query.ColumnSet = new ColumnSet("systemuserid");
                query.Criteria.AddCondition("fullname", ConditionOperator.Equal, fullName);
                EntityCollection results = context.OrganizationService.RetrieveMultiple(query);

                //If a user was found, return the user reference
                if (results.Entities.Count > 0)
                {
                    EntityReference userReference = new EntityReference("systemuser", results.Entities[0].Id);
                    UserReference.Set(context.CodeActivityContext, userReference);
                }
                else
                {
                    //If no user was found, throw an exception
                    throw new InvalidPluginExecutionException("No user found with the specified full name.");
                }
            }

        }
    }
}

