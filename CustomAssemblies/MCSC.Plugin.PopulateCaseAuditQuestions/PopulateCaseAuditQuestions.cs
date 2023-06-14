using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

                var auditor = target.GetAttributeValue<EntityReference>("som_auditor");
                var caseType = target.GetAttributeValue<EntityReference>("som_casetype");
                var auditorPre = targetPre.GetAttributeValue<EntityReference>("som_auditor");
                var caseTypePre = targetPre.GetAttributeValue<EntityReference>("som_casetype");

                //case type/auditor
                //case type/auditor

                //if case type changes, always remove all audit questions
                if (caseTypePre != caseType)
                {
                    RemoveAllExistingAuditQuestions(service, target.Id);
                }
                else
                {
                    //if case type does not change but the auditor DOES, then just return since the audit questions should stay the same. if auditor
                    if (auditorPre != auditor)
                    {
                        if (auditor == null)
                        {
                            RemoveAllExistingAuditQuestions(service, target.Id);
                            return;
                        }
                        if (auditorPre != null && auditor != null) //if it changes from bob to bill (and the doc type doesnt change) then just return
                        {
                            return;
                        }
                    }
                }

                //if auditor is blank, return. code above will remove all audit questions, assuming case type changes
                if (auditor == null) return;

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

                var questions = service.RetrieveMultiple(query)?.Entities?.ToList() ?? new List<Entity>();

                foreach (var question in questions)
                {
                    try
                    {
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
                        service.Create(new Entity("som_logentry")
                        {
                            ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                            ["som_name"] = ex.Message,
                            ["som_details"] = ex.StackTrace,
                            ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                            ["som_recordlogicalname"] = $"{question.LogicalName}",
                            ["som_recordid"] = $"{question.Id}",
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
