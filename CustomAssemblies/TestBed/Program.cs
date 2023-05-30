using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace TestBed
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Connection

            var url = ConfigurationManager.AppSettings["CRM_URL"];
            var clientId = ConfigurationManager.AppSettings["CRM_ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["CRM_ClientSecret"];

            var connectionString = "authtype=ClientSecret;" +
                $"Url={url};" +
                $"ClientId={clientId};" +
                $"ClientSecret={clientSecret};";

            var serviceClient = new CrmServiceClient(connectionString);

            var service = serviceClient.OrganizationWebProxyClient
                ?? (IOrganizationService)serviceClient.OrganizationServiceProxy;

            if (service == null) throw new Exception($"The connection could not be established to {url}. Connection string: '{connectionString}'");

            #endregion


        }
    }
}
