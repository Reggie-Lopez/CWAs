using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            try
            {
                var target = (Entity)context.InputParameters?["Target"];
                if (target == null) return;

                var transaction = service.Retrieve(target.LogicalName, target.Id, new ColumnSet("som_auditor", "som_documenttype"));

                var auditor = transaction.GetAttributeValue<EntityReference>("som_auditor");
                if (auditor == null) return;

                var documentType = transaction.GetAttributeValue<EntityReference>("som_documenttype");
                if (documentType == null) return;

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

                var questions = service.RetrieveMultiple(query)?.Entities?.ToList() ?? new List<Entity>();

                foreach (var question in questions)
                {
                    var auditQuestion = new Entity("som_auditquestion")
                    {
                        ["som_question"] = question.ToEntityReference(),
                        ["som_transaction"] = transaction.Id,
                        ["som_name"] = question.GetAttributeValue<string>("som_objective1.som_name"),
                        ["som_errortype"] = question.GetAttributeValue<string>("som_errortype"),
                    };

                    try
                    {
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
    }    
}
