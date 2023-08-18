using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.CaseDetailTransactionDocuments
{
    public class CreateTransactionDocuments : IPlugin
    {
        private List<string> validCaseDetailEntities = new List<string> { "som_appeal" };
        const int LOG_ENTRY_SEVERITY_ERROR = 186_690_001;

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var orgService = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory)))
                .CreateOrganizationService(context.UserId);
            var __trace = executionContext.GetExtension<ITracingService>();

            if (!context.InputParameters.Contains("Target"))
            {
                throw new Exception("Target is not provided.");
            }

            Entity target = context.InputParameters["Target"] is Entity ? (Entity)context.InputParameters["Target"] : null;

            if (!(target is Entity))
            {
                throw new InvalidPluginExecutionException("Invalid Target type provided.");
            }

            var isValid = validCaseDetailEntities.Contains(target.LogicalName);

            if (!isValid)
            {
                throw new InvalidPluginExecutionException("Target type provided not configured.");
            }

            if (context.MessageName.ToUpper() == "UPDATE")
            {
                var portalAcknowledgement = target.GetAttributeValue<bool>("som_portalacknowledgement");
                if (portalAcknowledgement)
                {
                    try
                    {
                        Execute(orgService, target);
                    }
                    catch (Exception ex)
                    {
                        //create new instance of IOrganizationService
                        var _service = executionContext.GetExtension<IOrganizationServiceFactory>().CreateOrganizationService(context.UserId);


                        _service.Create(new Entity("som_logentry")
                        {
                            ["som_source"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                            ["som_name"] = ex.Message,
                            ["som_details"] = ex.StackTrace,
                            ["som_severity"] = new OptionSetValue(LOG_ENTRY_SEVERITY_ERROR),
                            ["som_recordlogicalname"] = $"systemuser",
                            ["som_recordid"] = $"{context?.UserId}",
                        });

                        __trace.Trace("Entering catch block.");
                        __trace.Trace(ex.ToString());
                        __trace.Trace("Severity: " + LOG_ENTRY_SEVERITY_ERROR.ToString());
                        throw new InvalidPluginExecutionException(ex.Message);
                    }
                    

                }
            }
        }

        private void Execute(IOrganizationService orgService, Entity target) 
        {
            //Get Case Detail Info
            var caseDetail = orgService.Retrieve(target.LogicalName, target.Id, new ColumnSet("som_contactid","som_caseid"));
            var contactRef = caseDetail.GetAttributeValue<EntityReference>("som_contactid");
            var caseRef = caseDetail.GetAttributeValue<EntityReference>("som_caseid");

            //Get the attachments associated with this case details
            string fetchXml = @"<fetch> " +
                                "  <entity name='annotation'>  " +
                                "    <attribute name='annotationid' /> " +
                                "    <attribute name='createdby' /> " +
                                "    <attribute name='createdon' /> " +
                                "    <attribute name='documentbody' /> " +
                                "    <attribute name='filename' /> " +
                                "    <attribute name='filesize' /> " +
                                "    <attribute name='isdocument' /> " +
                                "    <attribute name='mimetype' /> " +
                                "    <attribute name='notetext' /> " +
                                "    <attribute name='objectid' /> " +
                                "    <attribute name='objecttypecode' /> " +
                                "    <attribute name='subject' /> " +
                                "    <filter> " +
                                "      <condition attribute='objectid' operator='eq' value='7aeda9a5-592e-ee11-a81c-001dd80690bf' /> " +
                                "    </filter>  " +
                                "  </entity>  " +
                                "</fetch>";

            var annotations = orgService.RetrieveMultiple(new FetchExpression(fetchXml))?.Entities?.ToList() ?? new List<Entity>();

            foreach (var entity in annotations)
            {
               
                var documentBody = entity.GetAttributeValue<string>("documentbody");
                var subject = entity.GetAttributeValue<string>("subject");
                var mimeType = entity.GetAttributeValue<string>("mimetype");
                var filename = entity.GetAttributeValue<string>("filename");
                var createdOn = entity.GetAttributeValue<DateTime>("createdon");
                var transactionId = CreateNewTransactionRecord(orgService, caseRef, contactRef, createdOn);

                CreateNoteForTransaction(orgService, transactionId, documentBody, subject, mimeType, filename);
            } 

        }

        private Guid CreateNewTransactionRecord(IOrganizationService orgService, EntityReference caseRef, EntityReference contactRef, DateTime createdOn)
        {
            //create new Transaction
            Entity newTransaction = new Entity("som_transaction");
            newTransaction["som_case"] = caseRef;
            newTransaction["som_contact"] = contactRef;

            //lookup values are static. 
            newTransaction["som_position"] = new EntityReference("position", new Guid("c67a4308-58bc-ed11-83ff-001dd806a8e5"));
            newTransaction["som_documenttype"] = new EntityReference("som_transactiontype", new Guid("d9ab44cb-7626-ee11-9965-001dd804c883"));
            newTransaction["som_transactiontype"] = new EntityReference("som_transactiontype", new Guid("1695c777-64d9-ed11-a7c7-001dd806a8e5"));
            newTransaction["som_documentreceived"] = true;
            newTransaction["som_documentreceivedon"] = createdOn;

            var transactionId = orgService.Create(newTransaction);

            return transactionId;
        }

        private void CreateNoteForTransaction(IOrganizationService orgService, Guid transactionId, string documentBody, string subject, string mimeType, string filename)
        {
            Entity note = new Entity("annotation");
            note.Attributes["objectid"] = new EntityReference("som_transaction", transactionId);
            note.Attributes["objecttypecode"] = "som_transaction"; ;
            note.Attributes["subject"] = subject;
            note.Attributes["documentbody"] = documentBody;
            note.Attributes["mimetype"] = mimeType;
            note.Attributes["filename"] = filename;
            orgService.Create(note);
        }
    }
}
