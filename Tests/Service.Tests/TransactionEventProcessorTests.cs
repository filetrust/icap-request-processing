using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NUnit.Framework;
using Service.Messaging;
using Service.StoreMessages.Enums;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Service.Tests
{
    [TestClass]
    public class TransactionEventProcessorTests
    {
        public class ProcessMethodTests : TransactionEventProcessorTests
        {
            private Mock<IGlasswallFileProcessor> _mockGlasswallFileProcessor;
            private Mock<IGlasswallVersionService> _mockGlasswallVersionService;
            private Mock<IOutcomeSender> _mockOutcomeSender;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IArchiveRequestSender> _mockArchiveRequestSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<IFileProcessorConfig> _mockConfig;

            private TransactionEventProcessor _transactionEventProcessor;

            [SetUp]
            public void SetUp()
            {
                _mockGlasswallFileProcessor = new Mock<IGlasswallFileProcessor>();
                _mockGlasswallVersionService = new Mock<IGlasswallVersionService>();
                _mockOutcomeSender = new Mock<IOutcomeSender>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockArchiveRequestSender = new Mock<IArchiveRequestSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockConfig = new Mock<IFileProcessorConfig>();

                _mockFileManager.Setup(s => s.ReadFile(It.IsAny<string>())).Returns(Encoding.UTF8.GetBytes("Hello World"));
                _mockConfig.SetupGet(s => s.ProcessingTimeoutDuration).Returns(TimeSpan.FromSeconds(1));

                _transactionEventProcessor = new TransactionEventProcessor(
                    _mockGlasswallFileProcessor.Object,
                    _mockGlasswallVersionService.Object,
                    _mockOutcomeSender.Object,
                    _mockTransactionEventSender.Object,
                    _mockArchiveRequestSender.Object,
                    _mockFileManager.Object,
                    _mockConfig.Object);
            }

            [TestCase(NcfsOption.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsOption.Block, FileOutcome.Failed)]
            public void Correct_Outcome_Is_Sent_When_FileType_Is_Unknown(NcfsOption unprocessableAction, string expected)
            {
                // Arrange
                _mockGlasswallFileProcessor.Setup(s => s.GetFileType(It.IsAny<byte[]>())).Returns(new FileTypeDetectionResponse(FileType.Unknown));
                _mockConfig.SetupGet(s => s.UnprocessableFileTypeAction).Returns(unprocessableAction);

                // Act
                _transactionEventProcessor.Process();

                // Assert
                _mockOutcomeSender.Verify(s => s.Send(
                    It.Is<string>(status => status == expected),
                    It.IsAny<string>(),
                    It.IsAny<string>()));
            }

            [TestCase(NcfsOption.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsOption.Block, FileOutcome.Failed)]
            public void Correct_Outcome_Is_Sent_When_File_Is_Not_Rebuilt(NcfsOption blockAction, string expected)
            {
                // Arrange
                _mockGlasswallFileProcessor.Setup(s => s.GetFileType(It.IsAny<byte[]>())).Returns(new FileTypeDetectionResponse(FileType.Doc));
                _mockGlasswallFileProcessor.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>())).Returns((byte[])null);
                _mockConfig.SetupGet(s => s.GlasswallBlockedFilesAction).Returns(blockAction);

                // Act
                _transactionEventProcessor.Process();

                // Assert
                _mockOutcomeSender.Verify(s => s.Send(
                    It.Is<string>(status => status == expected),
                    It.IsAny<string>(),
                    It.IsAny<string>()));
            }

            [Test]
            public void Correct_Outcome_Is_Sent_When_File_Is_Rebuilt()
            {
                // Arrange
                const string expected = FileOutcome.Replace;

                _mockGlasswallFileProcessor.Setup(s => s.GetFileType(It.IsAny<byte[]>())).Returns(new FileTypeDetectionResponse(FileType.Doc));
                _mockGlasswallFileProcessor.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(Encoding.UTF8.GetBytes("Rebuilt File"));
                _mockConfig.SetupGet(s => s.PolicyId).Returns(Guid.NewGuid());

                // Act
                _transactionEventProcessor.Process();

                // Assert
                _mockOutcomeSender.Verify(s => s.Send(
                    It.Is<string>(status => status == expected),
                    It.IsAny<string>(),
                    It.IsAny<string>()));
            }

            [TestCase(FileType.Zip)]
            [TestCase(FileType.Tar)]
            [TestCase(FileType.Rar)]
            [TestCase(FileType.SevenZip)]
            public void ArchiveRequest_Is_Sent_When_FileType_Is_An_Archive(FileType fileType)
            {
                // Arrange
                const string expectedFileId = "FileId1";
                const string expectedReplyTo = "ReplyToMe";
                const string expectedInput = "InputPathHere";
                const string expectedOutput = "OutputPathHere";

                _mockGlasswallFileProcessor.Setup(s => s.GetFileType(It.IsAny<byte[]>())).Returns(new FileTypeDetectionResponse(fileType));
                _mockConfig.SetupGet(s => s.PolicyId).Returns(Guid.NewGuid());
                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);
                _mockConfig.SetupGet(s => s.InputPath).Returns(expectedInput);
                _mockConfig.SetupGet(s => s.OutputPath).Returns(expectedOutput);
                _mockConfig.SetupGet(s => s.ReplyTo).Returns(expectedReplyTo);

                // Act
                _transactionEventProcessor.Process();

                // Assert
                _mockArchiveRequestSender.Verify(s => s.Send(
                    It.Is<string>(id => id == expectedFileId),
                    It.Is<string>(ft => ft == fileType.ToString()),
                    It.Is<string>(input => input == expectedInput),
                    It.Is<string>(output => output == expectedOutput),
                    It.Is<string>(replyTo => replyTo == expectedReplyTo)));
            }

            [Test]
            public void Long_Running_Process_Should_Clear_Output_Store()
            {
                _mockGlasswallFileProcessor.Setup(s => s.GetFileType(It.IsAny<byte[]>())).Returns(new FileTypeDetectionResponse(FileType.Doc));
                _mockGlasswallFileProcessor.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>()))
                    .Callback((byte[] f, string t) => Task.Delay(TimeSpan.FromMinutes(10)).Wait());
                _mockFileManager.Setup(m => m.DeleteFile(It.IsAny<string>()));
                _mockFileManager.Setup(m => m.FileExists(It.IsAny<string>())).Returns(true);

                _mockConfig.SetupGet(s => s.PolicyId).Returns(Guid.NewGuid());

                 _transactionEventProcessor.Process();

                _mockFileManager.Verify(m => m.DeleteFile(It.IsAny<string>()), Times.Once, "Store should be cleared in event of long running process");
            }

            [Test]
            public void Exception_Thrown_In_Process_Should_Clear_Output_Store()
            {
                _mockGlasswallFileProcessor.Setup(s => s.GetFileType(It.IsAny<byte[]>())).Returns(new FileTypeDetectionResponse(FileType.Doc));
                _mockGlasswallFileProcessor.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>()))
                    .Throws(new Exception());
                _mockFileManager.Setup(m => m.DeleteFile(It.IsAny<string>()));
                _mockFileManager.Setup(m => m.FileExists(It.IsAny<string>())).Returns(true);

                _mockConfig.SetupGet(s => s.PolicyId).Returns(Guid.NewGuid());

                _transactionEventProcessor.Process();

                _mockFileManager.Verify(m => m.DeleteFile(It.IsAny<string>()), Times.Once, "Store should be cleared in event of long running process");
            }
        }
    }
}
