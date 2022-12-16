using FakeXrmEasy;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Moq;
using System;
using System.IO;
using Xunit;

using TargetAssembly = MCSC.Plugins.Template;

namespace MCSC.Plugins.Tests
{
    public class TemplateTests : IDisposable
    {
        private readonly XrmFakedContext context;
        private readonly XrmFakedPluginExecutionContext executionContext;
        private readonly IOrganizationService service;

        private readonly ParameterCollection inputParameters;

        public TemplateTests()
        {
            // Setup
            context = new XrmFakedContext();
            executionContext = context.GetDefaultPluginContext();

            context.AddExecutionMock<DownloadReportDefinitionRequest>(DownloadReportDefinitionRequestMock);
            context.AddExecutionMock<RetrieveAttributeRequest>(RetrieveAttributeRequestMock);

            context.InitializeMetadata(System.Reflection.Assembly.GetAssembly(typeof(TargetAssembly.DoSomething)));
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

        private RetrieveAttributeResponse RetrieveAttributeRequestMock(OrganizationRequest req)
        {
            var optionSetMetadata = new OptionSetMetadata();
            optionSetMetadata.Options.Add(new OptionMetadata()
            {
                Value = 0,
                Label = new Label()
                {
                    UserLocalizedLabel = new LocalizedLabel()
                    {
                        Label = "PDF"
                    }
                }
            });

            var mock = new Mock<EnumAttributeMetadata>();
            mock.Object.OptionSet = optionSetMetadata;

            var response = new RetrieveAttributeResponse()
            {
                ["AttributeMetadata"] = mock.Object
            };

            return response;
        }

        public void Dispose()
        {
            // Tear Down
        }

        [Fact]
        public void WhenReportSendIsCreated_ThenEmailIsSent()
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

            var target = new Entity("new_reportsend")
            {
                ["new_primaryentityname"] = primaryEntity.LogicalName,
                ["new_primaryentityid"] = primaryEntity.Id.ToString(),
                ["new_reportname"] = report.GetAttributeValue<string>("name"),
                ["new_format"] = new OptionSetValue(0),
                ["new_recipient"] = recipient.ToEntityReference(),
            };
            target.Id = service.Create(target);

            inputParameters.Add("Target", target.ToEntityReference());
            executionContext.MessageName = "Create";
            executionContext.InputParameters = inputParameters;

            #endregion

            #region ACT

            context.ExecutePluginWith<TargetAssembly.DoSomething>(executionContext);

            #endregion

            #region ASSERT

            

            #endregion
        }
    }
}
