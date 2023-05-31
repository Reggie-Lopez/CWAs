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
        public void Execute(IServiceProvider serviceProvider)
        {
            _trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);

            try
            {
                var target = (Entity)context.InputParameters?["Target"];
                if (target == null) return;

                var caseRecord = service.Retrieve(target.LogicalName, target.Id, new ColumnSet("som_casetype"));

                var caseType = caseRecord.GetAttributeValue<EntityReference>("som_casetype");
                if (caseType == null) return;

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
                            ["som_case"] = caseRecord.Id,
                            ["som_name"] = question.GetAttributeValue<string>("som_objective1.som_name"),
                            ["som_pointsmaximum"] = question.GetAttributeValue<string>("som_maxratingvalue")
                        };

                        service.Create(auditQuestion);
                    }
                    catch (Exception ex)
                    {
                        // Create Log Entry
                    }
                }
            }
            catch (Exception ex)
            {
                // Create Log Entry
            }
        }
    }    
}
