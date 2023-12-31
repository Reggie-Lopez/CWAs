﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.AddCaseworkerToLoA
{
    public class AddCaseworkerToLoA : IPlugin
    {
        ITracingService _trace;
        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;
        public void Execute(IServiceProvider serviceProvider)
        {
            _trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);


            _trace.Trace("Entering try block.");
            try
            {
                _trace.Trace("Checking for Target Input Parameter.");
                if (context.InputParameters["Target"] == null) { throw new InvalidPluginExecutionException("The Target Input Parameter is missing for this plugin."); }
                if (!string.Equals(context.MessageName, "Update", StringComparison.CurrentCultureIgnoreCase)) return;
                if (context.PrimaryEntityName != "som_leaveofabsence") return;

                //loaId Target Parameter
                _trace.Trace("Getting Target Input Parameter.");
                var executingUser = context.UserId;
                var loaId = context.PrimaryEntityId;

                _trace.Trace("Checking if user is already a caseworker.");
                var userAlreadyCaseworker = CheckIfUserIsCaseworker(service, executingUser, loaId);
                if (!userAlreadyCaseworker)
                {
                    AssociateUserAndLoA(service, executingUser, loaId);
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
                    ["som_recordlogicalname"] = $"{context?.PrimaryEntityName}",
                    ["som_recordid"] = $"{context?.PrimaryEntityId}",
                });

                throw new InvalidPluginExecutionException(ex.Message);
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
