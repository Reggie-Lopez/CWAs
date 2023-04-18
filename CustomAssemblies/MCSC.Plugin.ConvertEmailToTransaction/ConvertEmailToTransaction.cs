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
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory)))
                .CreateOrganizationService(context.UserId);

            try
            {
                //confirm inputparameters exist
                if (context.InputParameters == null) { throw new InvalidPluginExecutionException("Input Parameters are not being passed into this plugin."); }
                if (context.InputParameters["EmailId"] == null) { throw new InvalidPluginExecutionException("The EmailId Input Parameter is missing for this plugin."); }
                if (context.InputParameters["AttachmentIdList"] == null) { throw new InvalidPluginExecutionException("The AttachmentIdList Input Parameter is missing for this plugin."); }

                //email InputParameter
                var emailId = Guid.Parse(context.InputParameters["EmailId"].ToString());  

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
            catch (Exception ex)
            {
                trace.Trace("Error: " + ex.Message);
                throw ex;
            }
        }

        private void CreateNotesWithAttachment(IOrganizationService service, Guid transactionId, List<Guid> attachmentIdList)
        {
            foreach (var id in attachmentIdList)
            {
                var retAttachment = service.Retrieve("activitymimeattachment", id, new ColumnSet( "objecttypecode", "filename", "mimetype", "subject", "body", "attachmentid" ));
                var note = new Entity();
                note["objectid"] = new EntityReference("som_transaction", transactionId);
                note["objecttypecode"] = 11003;
                note["subject"] = retAttachment["subject"].ToString();
                note["filename"] = retAttachment["filename"].ToString();
               // note["mimetype"] = 
            }
            
        }

        private Guid CreateTransactionRecord(IOrganizationService service)
        {
            var transactionRec = new Entity();
            transactionRec["som_commentsdescription"] = "This record was generated from an Email.";
            var transactionId = service.Create(transactionRec);

            return transactionId;
        }
    }
}
