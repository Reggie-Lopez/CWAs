using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.RenderWordTemplateForTransaction
{
    public class RenderWordTemplateForTransaction : IPlugin
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
                //get word template from action param
                var wordTemplateName = (string)context.InputParameters["WordTemplateName"];
                var wordTemplateId = GetWordTemplateID(service, wordTemplateName); 
                
                _trace.Trace($"Word Template ID: {wordTemplateId}");

                //get case from caseid provided in action param
                var caseId = (string)context.InputParameters["CaseId"];
                var caseEr = new EntityReference("incident", Guid.Parse(caseId));

                _trace.Trace($"Case ID: {caseId}");

                //get record info for word template
                var templateRecordId = (string)context.InputParameters["RecordId"];
                var templateRecordType = (string)context.InputParameters["RecordType"];
                var templateRecordER = new EntityReference(templateRecordType, Guid.Parse(templateRecordId));

                _trace.Trace($"Record ID: {templateRecordId}");

                //get if they want a word doc or PDF
                var wordOrPdf = (string)context.InputParameters["WordOrPdf"];

                _trace.Trace($"Word or PDF: {wordOrPdf}");

                //create new transaction record, associate to case
                var transactionId = CreateNewTransactionRecord(service, caseEr);

                _trace.Trace($"Transaction ID: {transactionId}");

                //render word template
                var renderedWordTemplate = GenerateWordOrPDFFromWordTemplate(service, wordTemplateId, templateRecordER.LogicalName, templateRecordER.Id, transactionId, wordOrPdf.ToLower());

                _trace.Trace($"Rendered Word Template: {renderedWordTemplate}");

                if (renderedWordTemplate != null)
                {
                    //create a note with the rendered word template (pdf)
                    CreateNoteForTransaction(service, transactionId, renderedWordTemplate, wordTemplateName);
                }
            }
            catch (Exception ex)
            {
                _trace.Trace("RenderWordTemplateForTransaction: Exception caught");
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

        private void CreateNoteForTransaction(IOrganizationService service, Guid transactionId, byte[] renderedWordTemplate, string wordTemplateName)
        {
            //create new note using the attachment properties
            Entity note = new Entity("annotation");
            note.Attributes["objectid"] = new EntityReference("som_transaction", transactionId);
            note.Attributes["objecttypecode"] = "som_transaction"; ;
            note.Attributes["subject"] = wordTemplateName;
            note.Attributes["documentbody"] = Convert.ToBase64String(renderedWordTemplate);
            note.Attributes["mimetype"] = @"application/pdf";
            note.Attributes["filename"] = wordTemplateName;
            service.Create(note);

        }

        private Guid CreateNewTransactionRecord(IOrganizationService service, EntityReference caseEr)
        {
            //get contact from case
            var caseRecContact = service.Retrieve("incident", caseEr.Id, new ColumnSet("primarycontactid"));
            var contactER = caseRecContact.GetAttributeValue<EntityReference>("primarycontactid");

            //create new Transaction
            Entity newTransaction = new Entity("som_transaction");
            newTransaction["som_case"] = caseEr;
            newTransaction["som_contact"] = contactER;
            var transactionId = service.Create(newTransaction);

            return transactionId;
        }


        private Guid GetWordTemplateID(IOrganizationService service, string wordTemplateName)
        {
            QueryExpression query = new QueryExpression("documenttemplate");
            query.ColumnSet.AddColumns("documenttemplateid");
            query.Criteria.AddCondition("name", ConditionOperator.Equal, wordTemplateName);
            EntityCollection templates = service.RetrieveMultiple(query);

            if (templates.Entities.Count == 0)
                throw new Exception($"No template found with name {wordTemplateName}");
            
            if (templates.Entities.Count > 1)
                throw new Exception($"More than one template found with name {wordTemplateName}");
            
            return templates.Entities[0].Id;
        }

        public byte[] GenerateWordOrPDFFromWordTemplate(IOrganizationService service, Guid? wordTemplateId, string entityName, Guid entityId, Guid transactionId, string wordOrPdf)
        {
            //rendering as word creates a note on the 'target' entity. that message does not give u the ability ot specify note text or a title
            if (wordOrPdf == "word")
            {
                OrganizationRequest req = new OrganizationRequest("SetWordTemplate");
                req["Target"] = new EntityReference("som_transaction", transactionId);
                req["SelectedTemplate"] = new EntityReference("documenttemplate", (Guid)wordTemplateId);
                service.Execute(req);
            }


            if (wordOrPdf == "pdf")
            {
                //get the Entity Type code from entity name
                var entityTypeCode = GetObjectTypeCodeOfEntity(service, entityName);

                //generate word template as pdf, it returns bytes. use that to attach to a note on the transaction
                OrganizationRequest exportPdfAction = new OrganizationRequest("ExportPdfDocument");
                exportPdfAction["EntityTypeCode"] = entityTypeCode;
                exportPdfAction["SelectedTemplate"] = new EntityReference("documenttemplate", (Guid)wordTemplateId);
                exportPdfAction["SelectedRecords"] = "[\'{" + entityId + "}\']";
                OrganizationResponse convertPdfResponse = service.Execute(exportPdfAction);

                return convertPdfResponse["PdfFile"] as byte[];
            }

            return null;
        }


        public int GetObjectTypeCodeOfEntity(IOrganizationService service, string entityName)
        {
            RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Entity,
                LogicalName = entityName
            };

            RetrieveEntityResponse retrieveAccountEntityResponse = (RetrieveEntityResponse)service.Execute(retrieveEntityRequest);
            EntityMetadata AccountEntity = retrieveAccountEntityResponse.EntityMetadata;
            return (int)retrieveAccountEntityResponse.EntityMetadata.ObjectTypeCode;
        }





    }
}