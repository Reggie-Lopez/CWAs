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

            //create new Transaction record, keep GUID
            var transactionId = CreateTransactionRecord(service);

            //loop through each attachmentid in list and create a note, attach file
            CreateNotesWithAttachment(service, transactionId, attachmentIdList);

            //pass the transactionid as the output parameter back to the JS that called the action
            context.OutputParameters["TransactionId"] = transactionId;
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
                    _trace.Trace("Error creating note/attachment: " + ex.Message);
                    throw ex;
                }
            }   
        }


        private Guid CreateTransactionRecord(IOrganizationService service)
        {
            try
            {
                //create new essentially-blank Transaction record, return the id
                var transactionRec = new Entity("som_transaction");
                transactionRec["som_commentsdescription"] = "This record was generated from an Email.";
                var transactionId = service.Create(transactionRec);

                return transactionId;
            }
            catch (Exception ex)
            {
                _trace.Trace("Error creating Transaction: " + ex.Message);
                throw ex;
            }
        }
    }    
}
