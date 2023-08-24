using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.PopulateTransactionAuditQuestions
{
    public class PopulateTransactionAuditQuestions : IPlugin
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
                Entity target = new Entity();
                Entity targetPre = new Entity();

                _trace.Trace("Retrieving target entity.");

                if (context.PostEntityImages.Contains("PostImage") && context.PostEntityImages["PostImage"] is Entity)
                {

                    _trace.Trace("Retrieving post image.");
                    target = (Entity)context.PostEntityImages["PostImage"];
                    targetPre = (Entity)context.PreEntityImages["PreImage"];
                    if (target == null) return;
                }

                _trace.Trace("Checking message name.");
                var auditor = target.GetAttributeValue<EntityReference>("som_auditor");
                var documentType = target.GetAttributeValue<EntityReference>("som_documenttype");
                var auditorPre = targetPre.GetAttributeValue<EntityReference>("som_auditor");
                var documentTypePre = targetPre.GetAttributeValue<EntityReference>("som_documenttype");

                //document type/auditor. document type is a required field

                //if document type changes, always remove all audit questions
                _trace.Trace("Checking document type.");    
                if (documentTypePre != documentType)
                {
                    _trace.Trace("Removing all existing audit questions."); 
                    RemoveAllExistingAuditQuestions(service, target.Id);                   
                }
                else
                {

                    _trace.Trace("Checking if auditor changed.");   
                    //if document type does not change but the auditor DOES, then just return since the audit questions should stay the same. if auditor
                    if (auditorPre != auditor)
                    {
                        _trace.Trace("Checking if auditor is null.");
                        if (auditor == null)
                        {
                            _trace.Trace("Removing all existing audit questions.");
                            RemoveAllExistingAuditQuestions(service, target.Id);
                            return;
                        }
                        if (auditorPre != null && auditor != null) //if it changes from bob to bill (and the doc type doesnt change) then just return
                        {
                            return;
                        }
                    }
                }

                //if auditor is blank, return. code above will remove all audit questions, assuming document type changes
                _trace.Trace("Checking if auditor is null.");
                if (auditor == null) return;


                _trace.Trace("Checking if document type is null.");
                var query = new QueryExpression("som_question")
                {
                    ColumnSet = new ColumnSet("som_name", "som_errortype"),
                    LinkEntities =
                    {
                        new LinkEntity("som_question", "som_objective", "som_objective", "som_objectiveid", JoinOperator.Inner)
                        {
                            Columns = new ColumnSet(false),
                            LinkCriteria = new FilterExpression(LogicalOperator.And)
                            {
                                Conditions =
                                {
                                    new ConditionExpression("som_documenttype", ConditionOperator.Equal, documentType.Id)
                                }
                            }
                        }
                    }
                };

                _trace.Trace("Retrieving questions.");
                var questions = service.RetrieveMultiple(query)?.Entities?.ToList() ?? new List<Entity>();

                foreach (var question in questions)
                {
                    _trace.Trace("Creating audit question.");
                    var auditQuestion = new Entity("som_auditquestion")
                    {
                       
                        ["som_question"] = question.ToEntityReference(),
                        ["som_transaction"] = target.Id,
                        ["som_name"] = question.GetAttributeValue<string>("som_objective1.som_name"),
                        ["som_errortype"] = question.GetAttributeValue<string>("som_errortype"),
                    };

                    try
                    {
                        service.Create(auditQuestion);
                    }
                    catch (Exception ex)
                    {
                        _trace.Trace("PopulateTransactionAuditQuestions: Exception caught");
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
            }
            catch (Exception ex)
            {
                _trace.Trace("PopulateTransactionAuditQuestions: Exception caught");
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

        private void RemoveAllExistingAuditQuestions(IOrganizationService service, Guid id)
        {
            ConditionExpression cond = new ConditionExpression();
            cond.AttributeName = "som_transaction";
            cond.Operator = ConditionOperator.Equal;
            cond.Values.Add(id);

            FilterExpression filter = new FilterExpression();
            filter.Conditions.Add(cond);

            QueryExpression query = new QueryExpression("som_auditquestion");
            query.ColumnSet.AddColumns("som_auditquestionid");
            query.Criteria.AddFilter(filter);

            var auditquestions = service.RetrieveMultiple(query)?.Entities?.ToList() ?? new List<Entity>();

            foreach (var auditq in auditquestions)
            {
                service.Delete("som_auditquestion", auditq.Id);
            }
            return;
        }

    }    
}
