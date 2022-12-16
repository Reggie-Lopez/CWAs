using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace TestBed
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Connection

            var url = ConfigurationManager.AppSettings["dev"];
            var username = ConfigurationManager.AppSettings["CRM_User"];
            var password = ConfigurationManager.AppSettings["CRM_Password"];
            var appId = ConfigurationManager.AppSettings["CRM_AppId"];
            var redirectUri = ConfigurationManager.AppSettings["CRM_RedirectURI"];

            var connectionString = "authtype=OAuth;" +
                $"Username={username};" +
                $"Password={password};" +
                $"Url={url};" +
                $"AppId={appId};" +
                $"RedirectUri={redirectUri};" +
                $"LoginPrompt=Never;";

            var serviceClient = new CrmServiceClient(connectionString);

            var service = serviceClient.OrganizationWebProxyClient
                ?? (IOrganizationService)serviceClient.OrganizationServiceProxy;

            if (service == null) throw new Exception($"Connection to CRM could not be established.");

            #endregion

        }
    }
}
