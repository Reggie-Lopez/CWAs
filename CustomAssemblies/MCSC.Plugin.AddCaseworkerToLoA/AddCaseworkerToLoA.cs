using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.AddCaseworkerToLoA
{
    public class AddCaseworkerToLoA : IPlugin
    {
        ITracingService _trace;
        public void Execute(IServiceProvider serviceProvider)
        {
            _trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);

            if (context.InputParameters["Target"] == null) { throw new InvalidPluginExecutionException("The Target Input Parameter is missing for this plugin."); }
            if (!string.Equals(context.MessageName, "Update", StringComparison.CurrentCultureIgnoreCase)) return;
            if (context.PrimaryEntityName != "som_leaveofabsence") return;

            //loaId Target Parameter
            var executingUser = context.UserId;
            var loaId = context.PrimaryEntityId;

            var userAlreadyCaseworker = CheckIfUserIsCaseworker(service, executingUser, loaId);
            if (!userAlreadyCaseworker)
            {
                AssociateUserAndLoA(service, executingUser, loaId);
            }

        }

        private void AssociateUserAndLoA(IOrganizationService service, Guid executingUser, Guid loaId)
        {
            Relationship relationship = new Relationship("som_som_leaveofabsence_systemuser");
            EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
            EntityReference secondaryEntity = new EntityReference("systemuserid", executingUser);
            relatedEntities.Add(secondaryEntity);
            service.Associate("som_leaveofabsence", loaId, relationship, relatedEntities);
        }

        private bool CheckIfUserIsCaseworker(IOrganizationService service, Guid executingUser, Guid loaId)
        {
            QueryExpression query = new QueryExpression("som_som_leaveofabsence_systemuser");
            query.ColumnSet.AddColumns("systemuserid", "som_leaveofabsenceid");
            query.Criteria = new FilterExpression();
            query.Criteria.AddCondition("systemuserid", ConditionOperator.Equal, executingUser);
            query.Criteria.AddCondition("som_leaveofabsenceid", ConditionOperator.Equal, loaId);

            EntityCollection rec = service.RetrieveMultiple(query);
            
            if (rec.Entities.Count > 0)
                return true;

            return false;
        }
    }













}
