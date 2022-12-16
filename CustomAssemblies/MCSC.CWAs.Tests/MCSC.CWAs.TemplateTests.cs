using FakeXrmEasy;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

using TargetAssembly = MCSC.CWAs.Template;

namespace MCSC.CWAs.Tests
{
    public class TemplateTests : IDisposable
    {
        private readonly XrmFakedContext context;
        private readonly XrmFakedWorkflowContext executionContext;
        private readonly IOrganizationService service;

        private readonly ParameterCollection inputParameters;

        public TemplateTests()
        {
            // Setup
            context = new XrmFakedContext();
            executionContext = context.GetDefaultWorkflowContext();

            context.AddExecutionMock<DownloadReportDefinitionRequest>(DownloadReportDefinitionRequestMock);

            try
            {
                context.InitializeMetadata(Assembly.GetAssembly(typeof(TargetAssembly.DoSomething)));
            }
            catch (Exception ex)
            {
                if (ex is ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                }
            }

            service = context.GetOrganizationService();
            inputParameters = new ParameterCollection();
        }

        private DownloadReportDefinitionResponse DownloadReportDefinitionRequestMock(OrganizationRequest req)
        {
            return new DownloadReportDefinitionResponse()
            {
                ["BodyText"] = File.ReadAllText(@"..\..\..\ReportRenderer.Reports\bin\Debug\Test.rdl")
            };
        }

        public void Dispose()
        {
            // Tear Down
        }

        [Fact]
        public void WhenSendEmailIsRun_ThenEmailIsSent()
        {
            #region ARRANGE

            var primaryEntity = new Entity("contact")
            {
                ["firstname"] = "Test",
                ["lastname"] = "User"
            };
            primaryEntity.Id = service.Create(primaryEntity);

            var recipient = new Entity("contact")
            {
                ["emailaddress1"] = "recipient@gmail.com"
            };
            recipient.Id = service.Create(recipient);

            var sender = new Entity("systemuser", executionContext.UserId)
            {
                ["internalemailaddress"] = "sender@gmail.com"
            };
            service.Create(sender);

            var report = new Entity("report")
            {
                ["name"] = "Test"
            };
            report.Id = service.Create(report);

            executionContext.PrimaryEntityName = primaryEntity.LogicalName;
            executionContext.PrimaryEntityId = primaryEntity.Id;
            inputParameters.Add("Recipient", recipient.ToEntityReference());
            inputParameters.Add("ReportName", "Test");
            inputParameters.Add("Format", "PDF");

            #endregion

            #region ACT

            context.ExecuteCodeActivity<TargetAssembly.DoSomething>(executionContext, inputs: inputParameters.ToDictionary(x => x.Key, x => x.Value));

            #endregion

            #region ASSERT



            #endregion
        }
    }
}
