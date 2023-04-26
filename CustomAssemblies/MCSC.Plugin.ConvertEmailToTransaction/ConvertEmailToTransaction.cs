using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCSC.Plugin.ConvertEmailToTransaction
{
    public class ConvertEmailToTransaction : IPlugin
    {
        ITracingService _trace;
        public void Execute(IServiceProvider serviceProvider)
        {
            _trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);

            //confirm inputparameters exist
            if (context.InputParameters == null) { throw new InvalidPluginExecutionException("Input Parameters are not being passed into this plugin."); }
            if (context.InputParameters["AttachmentIdList"] == null) { throw new InvalidPluginExecutionException("The AttachmentIdList Input Parameter is missing for this plugin."); }

            //email Target Parameter
            var emailId = (EntityReference)context.InputParameters["Target"];

            //attachment InputParameter
            var attachmentIds = context.InputParameters["AttachmentIdList"].ToString();
            var attachmentIdList = new List<Guid>();
            var attIdsSplit = attachmentIds.Split(',');

            foreach (var attId in attIdsSplit) { attachmentIdList.Add(Guid.Parse(attId)); }

            var transactionId = (string)context.InputParameters["ExistingTransactionId"];

            //check if ExistingTransactionId is provided
            if (transactionId == null)
            {
                //if there is not an ExistingTransactionId provided, then create new Transaction record, return GUID as Output
                transactionId = CreateTransactionRecord(service, emailId); 
            }
            else
            {                
                //update existing transaction to populate the email lookup
                //doing it like this so it's only one audit rec created instead of a create and THEN an update
                var transRec = new Entity("som_transaction");
                transRec["som_transactionid"] = new EntityReference("som_transaction", Guid.Parse(transactionId));
                transRec["som_email"] = emailId;
                service.Create(transRec);
            }         

            //loop through each attachmentid in list and create a note, attach file
            CreateNotesWithAttachment(service, Guid.Parse(transactionId), attachmentIdList);

            //pass the transactionid as the output parameter back to the JS that called the action
            context.OutputParameters["RedirectTransactionId"] = transactionId;
        }


        private void CreateNotesWithAttachment(IOrganizationService service, Guid transactionId, List<Guid> attachmentIdList)
        {
            foreach (var id in attachmentIdList)
            {
                try
                {
                    //retrieve properties of the attachment
                    var retAttachment = service.Retrieve("activitymimeattachment", id, new ColumnSet("filename", "mimetype", "body", "subject"));

                    var subject = "";
                    if (retAttachment.Contains("subject")) { subject = retAttachment["subject"].ToString(); }

                    var test = retAttachment["filename"].ToString();

                    //create new note using the attachment properties
                    var note = new Entity("annotation");
                    note["objectid"] = new EntityReference("som_transaction", transactionId);
                    note["objecttypecode"] = "som_transaction";
                    note["subject"] = subject;
                    note["filename"] = retAttachment["filename"].ToString();
                    note["mimetype"] = retAttachment["mimetype"].ToString();
                    note["documentbody"] = retAttachment["body"];
                    service.Create(note);
                }
                catch (Exception ex)
                {
                    _trace.Trace("Error creating note/attachment on Transaction (" + transactionId.ToString() + "): " + ex.Message);
                    throw ex;
                }
            }   
        }


        private string CreateTransactionRecord(IOrganizationService service, EntityReference emailId)
        {
            try
            {
                //create new essentially-blank Transaction record, return the id
                var transRec = new Entity("som_transaction");
                transRec["som_commentsdescription"] = "This record was generated from an Email.";
                transRec["som_email"] = emailId;
                var transactionId = service.Create(transRec);

                return transactionId.ToString();
            }
            catch (Exception ex)
            {
                _trace.Trace("Error creating Transaction: " + ex.Message);
                throw ex;
            }
        }
    }    
}
