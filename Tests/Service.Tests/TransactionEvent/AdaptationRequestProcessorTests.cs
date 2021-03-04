using System;
using NUnit.Framework;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Moq;
using Service.Configuration;
using Service.ErrorReport;
using Service.Messaging;
using Service.Storage;
using Service.TransactionEvent;

namespace Service.Tests.TransactionEvent
{
    public class AdaptationRequestProcessorTests
    {
        public class ProcessMethod : AdaptationRequestProcessorTests
        {
            private Mock<IFileProcessor> _mockFileProcessor;
            private Mock<IOutcomeSender> _mockOutcomeSender;
            private Mock<IArchiveRequestSender> _mockArchiveRequestSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<IErrorReportGenerator> _mockErrorReportGenerator;
            private Mock<IAdaptationRequestController> _mockAdaptationRequestController;
            private Mock<IFileProcessorConfig> _mockConfig;

            private AdaptationRequestProcessor _adaptationRequestProcessor;

            [SetUp]
            public void Setup()
            {
                _mockFileProcessor = new Mock<IFileProcessor>();
                _mockOutcomeSender = new Mock<IOutcomeSender>();
                _mockArchiveRequestSender = new Mock<IArchiveRequestSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockErrorReportGenerator = new Mock<IErrorReportGenerator>();
                _mockAdaptationRequestController = new Mock<IAdaptationRequestController>();
                _mockConfig = new Mock<IFileProcessorConfig>();

                _adaptationRequestProcessor = new AdaptationRequestProcessor(
                    _mockFileProcessor.Object,
                    _mockOutcomeSender.Object,
                    _mockArchiveRequestSender.Object,
                    _mockFileManager.Object,
                    _mockErrorReportGenerator.Object,
                    _mockAdaptationRequestController.Object,
                    _mockConfig.Object);
            }

            [Test]
            public async Task Context_Is_Build_Correctly()
            {
                // Arrange
                const string expectedOutputPath = "I AM THE OUTPUT PATH";
                const string expectedInputPath = "I AM THE INPUT PATH";
                const string expectedReplyTo = "I AM THE REPLY TO";

                var expectedFileId = Guid.NewGuid().ToString();
                var expectedPolicyId = Guid.NewGuid();

                var expectedContentManagement = new ContentManagementFlags
                {
                    ExcelContentManagement = new ExcelContentManagement
                    {
                        DynamicDataExchange = ContentManagementFlagAction.Allow,
                        EmbeddedFiles = ContentManagementFlagAction.Allow,
                        EmbeddedImages = ContentManagementFlagAction.Allow,
                        ExternalHyperlinks = ContentManagementFlagAction.Allow,
                        InternalHyperlinks = ContentManagementFlagAction.Allow,
                        Macros = ContentManagementFlagAction.Allow,
                        Metadata = ContentManagementFlagAction.Allow,
                        ReviewComments = ContentManagementFlagAction.Allow
                    }
                };

                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);
                _mockConfig.SetupGet(s => s.PolicyId).Returns(expectedPolicyId);
                _mockConfig.SetupGet(s => s.OutputPath).Returns(expectedOutputPath);
                _mockConfig.SetupGet(s => s.InputPath).Returns(expectedInputPath);
                _mockConfig.SetupGet(s => s.GenerateReport).Returns(true);
                _mockConfig.SetupGet(s => s.ReplyTo).Returns(expectedReplyTo);
                _mockConfig.SetupGet(s => s.ContentManagementFlags).Returns(expectedContentManagement);

                // Act
                await _adaptationRequestProcessor.Process();

                // Assert
                _mockAdaptationRequestController.Verify(s => s.ProcessRequest(
                    It.Is<AdaptationContext>(ac => ac.GenerateErrorReport &&
                                                   ac.ContentManagementFlags == expectedContentManagement &&
                                                   ac.FileId == expectedFileId &&
                                                   ac.PolicyId == expectedPolicyId.ToString() &&
                                                   ac.InputPath == expectedInputPath &&
                                                   ac.OutputPath == expectedOutputPath &&
                                                   ac.ReplyTo == expectedReplyTo &&
                                                   ac.TimeStamp != DateTime.MinValue &&
                                                   ac.OptionalHeaders != null &&
                                                   ac.OnFinishEvent != null &&
                                                   ac.OnArchiveEvent != null &&
                                                   ac .OnBlockedEvent != null &&
                                                   ac.OnUnmanagedEvent != null &&
                                                   ac.OnFailedEvent != null)), Times.Once);
            }
        }
    }
}