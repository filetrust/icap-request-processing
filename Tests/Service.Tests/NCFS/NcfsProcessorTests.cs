using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Service.Configuration;
using Service.ErrorReport;
using Service.Messaging;
using Service.NCFS;
using Service.Storage;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Tests.NCFS
{
    public class NcfsProcessorTests
    {
        public class GetUnmanagedActionAsyncMethod : NcfsProcessorTests
        {
            private Mock<INcfsClient> _mockNcfsClient;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<IFileProcessorConfig> _mockConfig;
            private Mock<ILogger<NcfsProcessor>> _mockLogger;

            private NcfsProcessor _ncfsProcessor;

            [SetUp]
            public void SetUp()
            {
                _mockNcfsClient = new Mock<INcfsClient>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockConfig = new Mock<IFileProcessorConfig>();
                _mockLogger = new Mock<ILogger<NcfsProcessor>>();

                _ncfsProcessor = new NcfsProcessor(
                    _mockNcfsClient.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockConfig.Object,
                    _mockLogger.Object);
            }

            [Test]
            public async Task NCFS_Events_Are_Sent_When_Action_Is_Refer()
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.UnprocessableFileTypeAction).Returns(NcfsOption.Refer);
                _mockNcfsClient.Setup(s => s.GetOutcome(It.IsAny<string>(), It.IsAny<FileType>())).Returns(Task.FromResult(new NcfsOutcome { NcfsDecision = NcfsDecision.Relay }));

                // Act
                await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, fileType);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsStartedEvent>()), Times.Once);
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsCompletedEvent>()), Times.Once);
            }

            [Test]
            public async Task NCFS_Events_AreNot_Sent_When_Action_IsNot_Refer()
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.UnprocessableFileTypeAction).Returns(NcfsOption.Block);

                // Act
                await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, fileType);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsStartedEvent>()), Times.Never);
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsCompletedEvent>()), Times.Never);
            }

            [TestCase(NcfsOption.Block, FileOutcome.Failed)]
            [TestCase(NcfsOption.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsOption.NotSet, FileOutcome.Unmodified)]
            public async Task Configured_Outcome_Is_Used_When_Actions_IsNot_Refer(NcfsOption option, string expectedOutcome)
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.UnprocessableFileTypeAction).Returns(option);

                // Act
                var result = await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, fileType);

                // Assert
                Assert.That(result.FileOutcome, Is.EqualTo(expectedOutcome));
            }

            [TestCase(NcfsDecision.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsDecision.Replace, FileOutcome.Replace)]
            [TestCase(NcfsDecision.Block, FileOutcome.Failed)]
            public async Task Ncfs_Api_Is_Used_When_Action_Is_Refer_And_Outcome_Is_Correct(NcfsDecision decision, string expectedOutcome)
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.UnprocessableFileTypeAction).Returns(NcfsOption.Refer);
                _mockNcfsClient.Setup(s => s.GetOutcome(It.IsAny<string>(), It.IsAny<FileType>())).Returns(Task.FromResult(new NcfsOutcome { NcfsDecision = decision, Base64Replacement = "REPLACEMENT" }));

                // Act
                var result = await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, fileType);

                // Assert
                Assert.That(result.FileOutcome, Is.EqualTo(expectedOutcome));
            }

            [Test]
            public async Task OutputFile_Is_Replaced_When_Ncfs_Api_Returns_Replacement()
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;
                var expectedReplacement = "I AM THE REPLACEMENT BASE64";
                var outputPath = "OUTPUT PATH";

                var replacementBytes = Encoding.UTF8.GetBytes(expectedReplacement);

                _mockConfig.SetupGet(s => s.UnprocessableFileTypeAction).Returns(NcfsOption.Refer);
                _mockConfig.SetupGet(s => s.OutputPath).Returns(outputPath);
                _mockNcfsClient.Setup(s => s.GetOutcome(It.IsAny<string>(), It.IsAny<FileType>())).Returns(Task.FromResult(new NcfsOutcome { NcfsDecision = NcfsDecision.Replace, Base64Replacement = expectedReplacement }));

                // Act
                var result = await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, fileType);

                // Assert
                _mockFileManager.Verify(s => s.WriteFile(
                    It.Is<string>(o => o == outputPath),
                    It.Is<byte[]>(file => file.Where((b, i) => b == replacementBytes[i]).Count() == replacementBytes.Length)), Times.Once);
            }
        }

        public class GetBlockedActionAsyncMethod : NcfsProcessorTests
        {
            private Mock<INcfsClient> _mockNcfsClient;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<IFileProcessorConfig> _mockConfig;
            private Mock<ILogger<NcfsProcessor>> _mockLogger;

            private NcfsProcessor _ncfsProcessor;

            [SetUp]
            public void SetUp()
            {
                _mockNcfsClient = new Mock<INcfsClient>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockConfig = new Mock<IFileProcessorConfig>();
                _mockLogger = new Mock<ILogger<NcfsProcessor>>();

                _ncfsProcessor = new NcfsProcessor(
                    _mockNcfsClient.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockConfig.Object,
                    _mockLogger.Object);
            }

            [Test]
            public async Task NCFS_Events_Are_Sent_When_Action_Is_Refer()
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.GlasswallBlockedFilesAction).Returns(NcfsOption.Refer);
                _mockNcfsClient.Setup(s => s.GetOutcome(It.IsAny<string>(), It.IsAny<FileType>())).Returns(Task.FromResult(new NcfsOutcome { NcfsDecision = NcfsDecision.Relay }));

                // Act
                await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, fileType);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsStartedEvent>()), Times.Once);
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsCompletedEvent>()), Times.Once);
            }

            [Test]
            public async Task NCFS_Events_AreNot_Sent_When_Action_IsNot_Refer()
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.GlasswallBlockedFilesAction).Returns(NcfsOption.Block);

                // Act
                await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, fileType);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsStartedEvent>()), Times.Never);
                _mockTransactionEventSender.Verify(s => s.Send(It.IsAny<NcfsCompletedEvent>()), Times.Never);
            }

            [TestCase(NcfsOption.Block, FileOutcome.Failed)]
            [TestCase(NcfsOption.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsOption.NotSet, FileOutcome.Unmodified)]
            public async Task Configured_Outcome_Is_Used_When_Actions_IsNot_Refer(NcfsOption option, string expectedOutcome)
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.GlasswallBlockedFilesAction).Returns(option);

                // Act
                var result = await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, fileType);

                // Assert
                Assert.That(result.FileOutcome, Is.EqualTo(expectedOutcome));
            }

            [TestCase(NcfsDecision.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsDecision.Replace, FileOutcome.Replace)]
            [TestCase(NcfsDecision.Block, FileOutcome.Failed)]
            public async Task Ncfs_Api_Is_Used_When_Action_Is_Refer_And_Outcome_Is_Correct(NcfsDecision decision, string expectedOutcome)
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;

                _mockConfig.SetupGet(s => s.GlasswallBlockedFilesAction).Returns(NcfsOption.Refer);
                _mockNcfsClient.Setup(s => s.GetOutcome(It.IsAny<string>(), It.IsAny<FileType>())).Returns(Task.FromResult(new NcfsOutcome { NcfsDecision = decision, Base64Replacement = "REPLACEMENT" }));

                // Act
                var result = await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, fileType);

                // Assert
                Assert.That(result.FileOutcome, Is.EqualTo(expectedOutcome));
            }

            [Test]
            public async Task OutputFile_Is_Replaced_When_Ncfs_Api_Returns_Replacement()
            {
                // Arrange
                var timestamp = DateTime.UtcNow;
                var base64File = "Base64FileString";
                var fileType = FileType.Doc;
                var expectedReplacement = "I AM THE REPLACEMENT BASE64";
                var outputPath = "OUTPUT PATH";

                var replacementBytes = Encoding.UTF8.GetBytes(expectedReplacement);

                _mockConfig.SetupGet(s => s.GlasswallBlockedFilesAction).Returns(NcfsOption.Refer);
                _mockConfig.SetupGet(s => s.OutputPath).Returns(outputPath);
                _mockNcfsClient.Setup(s => s.GetOutcome(It.IsAny<string>(), It.IsAny<FileType>())).Returns(Task.FromResult(new NcfsOutcome { NcfsDecision = NcfsDecision.Replace, Base64Replacement = expectedReplacement }));

                // Act
                var result = await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, fileType);

                // Assert
                _mockFileManager.Verify(s => s.WriteFile(
                    It.Is<string>(o => o == outputPath),
                    It.Is<byte[]>(file => file.Where((b, i) => b == replacementBytes[i]).Count() == replacementBytes.Length)), Times.Once);
            }
        }
    }
}
