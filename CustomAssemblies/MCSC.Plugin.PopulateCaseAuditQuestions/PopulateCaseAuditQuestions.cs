using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.PopulateCaseAuditQuestions
{
    public class PopulateCaseAuditQuestions : IPlugin
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
                var caseType = target.GetAttributeValue<EntityReference>("som_casetype");
                var auditorPre = targetPre.GetAttributeValue<EntityReference>("som_auditor");
                var caseTypePre = targetPre.GetAttributeValue<EntityReference>("som_casetype");

                //case type/auditor
                //case type/auditor

                //if case type changes, always remove all audit questions

                _trace.Trace("Checking case type and auditor.");
                if (caseTypePre != caseType)
                {
                    _trace.Trace("Removing all existing audit questions.");
                    RemoveAllExistingAuditQuestions(service, target.Id);
                }
                else
                {

                    //if case type does not change but the auditor DOES, then just return since the audit questions should stay the same. if auditor
                    _trace.Trace("Checking if auditor changed.");
                    if (auditorPre != auditor)
                    {
                        if (auditor == null)
                        {
                            _trace.Trace("Removing all existing audit questions.");
                            RemoveAllExistingAuditQuestions(service, target.Id);
                            return;
                        }
                        if (auditorPre != null && auditor != null) //if it changes from bob to bill (and the doc type doesnt change) then just return
                        {
                            _trace.Trace("Auditor changed, but case type did not. Returning.");
                            return;
                        }
                    }
                }

                //if auditor is blank, return. code above will remove all audit questions, assuming case type changes
                _trace.Trace("Checking if auditor is null.");
                if (auditor == null) return;

                _trace.Trace("Populating audit questions.");
                var query = new QueryExpression("som_question")
                {
                    ColumnSet = new ColumnSet("som_name", "som_maxratingvalue"),
                    LinkEntities =
                    {
                        new LinkEntity("som_question", "som_objective", "som_objective", "som_objectiveid", JoinOperator.Inner)
                        {
                            Columns = new ColumnSet(false),
                            LinkCriteria = new FilterExpression(LogicalOperator.And)
                            {
                                Conditions =
                                {
                                    new ConditionExpression("som_casetype", ConditionOperator.Equal, caseType.Id)
                                }
                            }
                        }
                    }
                };

                _trace.Trace("Retrieving questions.");
                var questions = service.RetrieveMultiple(query)?.Entities?.ToList() ?? new List<Entity>();

                _trace.Trace("Checking if questions were retrieved.");
                foreach (var question in questions)
                {
                    try
                    {
                        _trace.Trace("Creating audit question.");
                        var auditQuestion = new Entity("som_auditquestion")
                        {
                            ["som_question"] = question.ToEntityReference(),
                            ["som_case"] = target.Id,
                            ["som_name"] = question.GetAttributeValue<string>("som_objective1.som_name"),
                            ["som_pointsmaximum"] = question.GetAttributeValue<string>("som_maxratingvalue")
                        };

                        service.Create(auditQuestion);
                    }
                    catch (Exception ex)
                    {
                        _trace.Trace("PopulateCaseAuditQuestions: Exception caught");
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
                            ["som_recordlogicalname"] = $"systemuser",
                            ["som_recordid"] = $"{context?.UserId}",
                        });

                        throw new InvalidPluginExecutionException(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.Trace("PopulateCaseAuditQuestions: Exception caught");
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
                    ["som_recordlogicalname"] = $"systemuser",
                    ["som_recordid"] = $"{context?.UserId}",
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
