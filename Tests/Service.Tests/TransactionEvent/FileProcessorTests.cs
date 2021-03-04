using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using Service.Engine;
using Service.Messaging;
using Service.NCFS;
using Service.Storage;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using Service.TransactionEvent;
using Assert = NUnit.Framework.Assert;

namespace Service.Tests.TransactionEvent
{
    public class FileProcessorTests
    {
        public class HandleNewFileReadMethod : FileProcessorTests
        {
            private Mock<IGlasswallEngineService> _mockGlasswallEngineService;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<INcfsProcessor> _mockNcfsProcessor;
            private Mock<ILogger<FileProcessor>> _mockLogger;

            private FileProcessor _fileProcessor;

            [SetUp]
            public void Setup()
            {
                _mockGlasswallEngineService = new Mock<IGlasswallEngineService>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockNcfsProcessor = new Mock<INcfsProcessor>();
                _mockLogger = new Mock<ILogger<FileProcessor>>();

                _fileProcessor = new FileProcessor(
                    _mockGlasswallEngineService.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockNcfsProcessor.Object,
                    _mockLogger.Object);
            }

            [Test]
            public void NewDocumentEvent_Is_Sent_To_The_TransactionEventSender()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedPolicyId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                _mockFileManager.Setup(s => s.FileExists(It.IsAny<string>())).Returns(true);

                // Act
                _fileProcessor.HandleNewFileRead(expectedFileId, expectedPolicyId, "INPUT", expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<NewDocumentEvent>(nde =>
                    nde.Mode == RequestMode.Response &&
                    nde.PolicyId == expectedPolicyId &&
                    nde.FileId == expectedFileId &&
                    nde.Timestamp == expectedTimeStamp)));
            }

            [Test]
            public void FileNotFoundException_Is_Thrown_When_File_Does_Not_Exist()
            {
                // Arrange
                _mockFileManager.Setup(s => s.FileExists(It.IsAny<string>())).Returns(false);

                // Act

                // Assert
                Assert.Throws<FileNotFoundException>(() => _fileProcessor.HandleNewFileRead("FileId", "PolicyId", "Input", DateTime.UtcNow));
            }

            [Test]
            public void File_Is_Returned_When_File_Does_Exist()
            {
                // Arrange
                var expectedBytes = Encoding.UTF8.GetBytes("I AM THE FILE");

                _mockFileManager.Setup(s => s.FileExists(It.IsAny<string>())).Returns(true);
                _mockFileManager.Setup(s => s.ReadFile(It.IsAny<string>())).Returns(expectedBytes);

                // Act
                var result = _fileProcessor.HandleNewFileRead("FileId", "PolicyId", "Input", DateTime.UtcNow);

                // Assert
                Assert.That(result, Is.EqualTo(expectedBytes));
            }
        }

        public class HandleFileTypeDetectionMethod : FileProcessorTests
        {
            private Mock<IGlasswallEngineService> _mockGlasswallEngineService;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<INcfsProcessor> _mockNcfsProcessor;
            private Mock<ILogger<FileProcessor>> _mockLogger;

            private FileProcessor _fileProcessor;

            [SetUp]
            public void Setup()
            {
                _mockGlasswallEngineService = new Mock<IGlasswallEngineService>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockNcfsProcessor = new Mock<INcfsProcessor>();
                _mockLogger = new Mock<ILogger<FileProcessor>>();

                _fileProcessor = new FileProcessor(
                    _mockGlasswallEngineService.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockNcfsProcessor.Object,
                    _mockLogger.Object);
            }

            [Test]
            public void Outputted_FileType_Is_Sent_With_FileTypeDetectionEvent()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;
                var expectedFileType = FileType.Docx;

                var response = new FileTypeDetectionResponse(expectedFileType);

                _mockGlasswallEngineService.Setup(s => s.GetFileType(It.IsAny<byte[]>(), It.IsAny<string>()))
                    .Returns(response);

                // Act
                _fileProcessor.HandleFileTypeDetection(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<FileTypeDetectionEvent>(ftde =>
                    ftde.FileType == expectedFileType.ToString() &&
                    ftde.FileId == expectedFileId &&
                    ftde.Timestamp == expectedTimeStamp)));
            }

            [Test]
            public void Returned_FileType_Is_Correct()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;
                var expectedFileType = FileType.Bmp;

                var response = new FileTypeDetectionResponse(expectedFileType);

                _mockGlasswallEngineService.Setup(s => s.GetFileType(It.IsAny<byte[]>(), It.IsAny<string>()))
                    .Returns(response);

                // Act
                var result = _fileProcessor.HandleFileTypeDetection(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, expectedTimeStamp);

                // Assert
                Assert.That(result, Is.EqualTo(expectedFileType));
            }
        }

        public class HandleAnalysisMethod : FileProcessorTests
        {
            private Mock<IGlasswallEngineService> _mockGlasswallEngineService;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<INcfsProcessor> _mockNcfsProcessor;
            private Mock<ILogger<FileProcessor>> _mockLogger;

            private FileProcessor _fileProcessor;

            [SetUp]
            public void Setup()
            {
                _mockGlasswallEngineService = new Mock<IGlasswallEngineService>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockNcfsProcessor = new Mock<INcfsProcessor>();
                _mockLogger = new Mock<ILogger<FileProcessor>>();

                _fileProcessor = new FileProcessor(
                    _mockGlasswallEngineService.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockNcfsProcessor.Object,
                    _mockLogger.Object);
            }

            [Test]
            public void Analysis_Report_Is_Sent_To_AnalysisCompletedEvent()
            {
                // Arrange
                const string expectedReport = "I AM THE ANALYSIS REPORT";

                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                _mockGlasswallEngineService.Setup(s => s.AnalyseFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(expectedReport);

                // Act
                _fileProcessor.HandleAnalysis(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<AnalysisCompletedEvent>(ace =>
                    ace.FileId == expectedFileId &&
                    ace.Timestamp == expectedTimeStamp && 
                    ace.AnalysisReport == expectedReport)));
            }
        }

        public class HandleRebuildMethod : FileProcessorTests
        {
            private Mock<IGlasswallEngineService> _mockGlasswallEngineService;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<INcfsProcessor> _mockNcfsProcessor;
            private Mock<ILogger<FileProcessor>> _mockLogger;

            private FileProcessor _fileProcessor;

            [SetUp]
            public void Setup()
            {
                _mockGlasswallEngineService = new Mock<IGlasswallEngineService>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockNcfsProcessor = new Mock<INcfsProcessor>();
                _mockLogger = new Mock<ILogger<FileProcessor>>();

                _fileProcessor = new FileProcessor(
                    _mockGlasswallEngineService.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockNcfsProcessor.Object,
                    _mockLogger.Object);
            }

            [Test]
            public void RebuildStartingEvent_Is_Sent_To_The_TransactionEventSender()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                var contentManagementFlags = new ContentManagementFlags();

                _mockGlasswallEngineService.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContentManagementFlags>()))
                    .Returns((byte[]) null);

                // Act
                _fileProcessor.HandleRebuild(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, "OutputPath", contentManagementFlags, expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<RebuildStartingEvent>(rse =>
                    rse.FileId == expectedFileId &&
                    rse.Timestamp == expectedTimeStamp)));
            }

            [Test]
            public void Failed_Is_Returned_When_RebuildFile_Returns_Null()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                var contentManagementFlags = new ContentManagementFlags();

                _mockGlasswallEngineService.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContentManagementFlags>()))
                    .Returns((byte[])null);

                // Act
                var result = _fileProcessor.HandleRebuild(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, "OutputPath", contentManagementFlags, expectedTimeStamp);

                // Assert
                Assert.That(result, Is.EqualTo(FileOutcome.Failed));
            }

            [Test]
            public void Failed_Is_Returned_When_RebuildFile_Returns_Zero_Length_Array()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                var contentManagementFlags = new ContentManagementFlags();

                _mockGlasswallEngineService.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContentManagementFlags>()))
                    .Returns(new byte[0]);

                // Act
                var result = _fileProcessor.HandleRebuild(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, "OutputPath", contentManagementFlags, expectedTimeStamp);

                // Assert
                Assert.That(result, Is.EqualTo(FileOutcome.Failed));
            }

            [Test]
            public void RebuildCompletedEvent_Is_Sent_With_Failed_Outcome_When_File_IsNot_Rebuilt()
            {
                // Arrange
                const string expectedOutputPath = "I AM THE OUTPUT PATH";

                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                var contentManagementFlags = new ContentManagementFlags();

                _mockGlasswallEngineService.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContentManagementFlags>()))
                    .Returns(new byte[0]);

                // Act
                _fileProcessor.HandleRebuild(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, expectedOutputPath, contentManagementFlags, expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<RebuildCompletedEvent>(rce =>
                    rce.FileId == expectedFileId &&
                    rce.Timestamp == expectedTimeStamp &&
                    rce.GwOutcome == FileOutcome.Failed)));
            }

            [Test]
            public void File_Is_Written_To_Disc_When_Rebuild_Returns_File()
            {
                // Arrange
                const string expectedOutputPath = "I AM THE OUTPUT PATH";

                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                var rebuiltFile = Encoding.UTF8.GetBytes("I HAVE BEEN REBUILT");

                var contentManagementFlags = new ContentManagementFlags();

                _mockGlasswallEngineService.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContentManagementFlags>()))
                    .Returns(rebuiltFile);

                // Act
                _fileProcessor.HandleRebuild(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, expectedOutputPath, contentManagementFlags, expectedTimeStamp);

                // Assert
                _mockFileManager.Verify(s => s.WriteFile(
                    It.Is<string>(output => output == expectedOutputPath),
                    It.Is<byte[]>(report => report.Where((b, i) => b == rebuiltFile[i]).Count() == rebuiltFile.Length)));
            }

            [Test]
            public void Replace_Is_Returned_When_Rebuild_Returns_File()
            {
                // Arrange
                const string expectedOutputPath = "I AM THE OUTPUT PATH";

                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                var rebuiltFile = Encoding.UTF8.GetBytes("I HAVE BEEN REBUILT");

                var contentManagementFlags = new ContentManagementFlags();

                _mockGlasswallEngineService.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContentManagementFlags>()))
                    .Returns(rebuiltFile);

                // Act
                var result = _fileProcessor.HandleRebuild(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, expectedOutputPath, contentManagementFlags, expectedTimeStamp);

                // Assert
                Assert.That(result, Is.EqualTo(FileOutcome.Replace));
            }

            [Test]
            public void RebuildCompletedEvent_Is_Sent_With_Replace_Outcome_When_File_Is_Rebuilt()
            {
                // Arrange
                const string expectedOutputPath = "I AM THE OUTPUT PATH";

                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;

                var rebuiltFile = Encoding.UTF8.GetBytes("I HAVE BEEN REBUILT");

                var contentManagementFlags = new ContentManagementFlags();

                _mockGlasswallEngineService.Setup(s => s.RebuildFile(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContentManagementFlags>()))
                    .Returns(rebuiltFile);

                // Act
                _fileProcessor.HandleRebuild(Encoding.UTF8.GetBytes("Hello World"), expectedFileId, FileType.Doc, expectedOutputPath, contentManagementFlags, expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<RebuildCompletedEvent>(rce =>
                    rce.FileId == expectedFileId &&
                    rce.Timestamp == expectedTimeStamp &&
                    rce.GwOutcome == FileOutcome.Replace)));
            }
        }

        public class HandleUnmanagedFileMethod : FileProcessorTests
        {
            private Mock<IGlasswallEngineService> _mockGlasswallEngineService;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<INcfsProcessor> _mockNcfsProcessor;
            private Mock<ILogger<FileProcessor>> _mockLogger;

            private FileProcessor _fileProcessor;

            [SetUp]
            public void Setup()
            {
                _mockGlasswallEngineService = new Mock<IGlasswallEngineService>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockNcfsProcessor = new Mock<INcfsProcessor>();
                _mockLogger = new Mock<ILogger<FileProcessor>>();

                _fileProcessor = new FileProcessor(
                    _mockGlasswallEngineService.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockNcfsProcessor.Object,
                    _mockLogger.Object);
            }

            [Test]
            public async Task Optional_Headers_Are_Added_If_Returned_By_NCFS()
            {
                // Arrange
                const string expectedMimeType = "text/html";

                var file = Encoding.UTF8.GetBytes("I AM FILE");
                var optionalHeaders = new Dictionary<string, string>();

                var outcome = new NcfsOutcome
                {
                    NcfsDecision = NcfsDecision.Replace,
                    ReplacementMimeType = expectedMimeType
                };

                _mockNcfsProcessor.Setup(s => s.GetUnmanagedActionAsync(It.IsAny<DateTime>(), It.IsAny<string>(),
                        It.IsAny<FileType>())).Returns(Task.FromResult(outcome));

                // Act
                await _fileProcessor.HandleUnmanagedFile(file, "fileId", FileType.Doc, optionalHeaders, DateTime.UtcNow);

                // Assert
                Assert.That(optionalHeaders.Count, Is.EqualTo(1));
                Assert.That(optionalHeaders["outcome-header-Content-Type"], Is.EqualTo(expectedMimeType));
            }

            [TestCase(NcfsDecision.Block, FileOutcome.Failed)]
            [TestCase(NcfsDecision.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsDecision.Replace, FileOutcome.Replace)]
            public async Task UnmanagedFileTypeActionEvent_Is_Sent_With_The_Correct_Outcome(NcfsDecision decision, string expectedOutcome)
            {
                // Arrange
                var file = Encoding.UTF8.GetBytes("I AM FILE");
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;
                var optionalHeaders = new Dictionary<string, string>();

                var outcome = new NcfsOutcome
                {
                    NcfsDecision = decision
                };

                _mockNcfsProcessor.Setup(s => s.GetUnmanagedActionAsync(It.IsAny<DateTime>(), It.IsAny<string>(),
                    It.IsAny<FileType>())).Returns(Task.FromResult(outcome));

                // Act
                await _fileProcessor.HandleUnmanagedFile(file, expectedFileId, FileType.Doc, optionalHeaders, expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<UnmanagedFileTypeActionEvent>(umfa =>
                    umfa.FileId == expectedFileId &&
                    umfa.Timestamp == expectedTimeStamp &&
                    umfa.Action == expectedOutcome)));
            }

            [TestCase(NcfsDecision.Block, FileOutcome.Failed)]
            [TestCase(NcfsDecision.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsDecision.Replace, FileOutcome.Replace)]
            public async Task Correct_Status_Is_Returned(NcfsDecision decision, string expectedOutcome)
            {
                // Arrange
                var file = Encoding.UTF8.GetBytes("I AM FILE");
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;
                var optionalHeaders = new Dictionary<string, string>();

                var outcome = new NcfsOutcome
                {
                    NcfsDecision = decision
                };

                _mockNcfsProcessor.Setup(s => s.GetUnmanagedActionAsync(It.IsAny<DateTime>(), It.IsAny<string>(),
                    It.IsAny<FileType>())).Returns(Task.FromResult(outcome));

                // Act
                var status = await _fileProcessor.HandleUnmanagedFile(file, expectedFileId, FileType.Doc, optionalHeaders, expectedTimeStamp);

                // Assert
                Assert.That(status, Is.EqualTo(expectedOutcome));
            }
        }

        public class HandleBlockedFileMethod : FileProcessorTests
        {
            private Mock<IGlasswallEngineService> _mockGlasswallEngineService;
            private Mock<ITransactionEventSender> _mockTransactionEventSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<INcfsProcessor> _mockNcfsProcessor;
            private Mock<ILogger<FileProcessor>> _mockLogger;

            private FileProcessor _fileProcessor;

            [SetUp]
            public void Setup()
            {
                _mockGlasswallEngineService = new Mock<IGlasswallEngineService>();
                _mockTransactionEventSender = new Mock<ITransactionEventSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockNcfsProcessor = new Mock<INcfsProcessor>();
                _mockLogger = new Mock<ILogger<FileProcessor>>();

                _fileProcessor = new FileProcessor(
                    _mockGlasswallEngineService.Object,
                    _mockTransactionEventSender.Object,
                    _mockFileManager.Object,
                    _mockNcfsProcessor.Object,
                    _mockLogger.Object);
            }

            [Test]
            public async Task Optional_Headers_Are_Added_If_Returned_By_NCFS()
            {
                // Arrange
                const string expectedMimeType = "text/html";

                var file = Encoding.UTF8.GetBytes("I AM FILE");
                var optionalHeaders = new Dictionary<string, string>();

                var outcome = new NcfsOutcome
                {
                    NcfsDecision = NcfsDecision.Replace,
                    ReplacementMimeType = expectedMimeType
                };

                _mockNcfsProcessor.Setup(s => s.GetBlockedActionAsync(It.IsAny<DateTime>(), It.IsAny<string>(),
                        It.IsAny<FileType>())).Returns(Task.FromResult(outcome));

                // Act
                await _fileProcessor.HandleBlockedFile(file, "fileId", FileType.Doc, optionalHeaders, DateTime.UtcNow);

                // Assert
                Assert.That(optionalHeaders.Count, Is.EqualTo(1));
                Assert.That(optionalHeaders["outcome-header-Content-Type"], Is.EqualTo(expectedMimeType));
            }

            [TestCase(NcfsDecision.Block, FileOutcome.Failed)]
            [TestCase(NcfsDecision.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsDecision.Replace, FileOutcome.Replace)]
            public async Task BlockedFiletypeActionEvent_Is_Sent_With_The_Correct_Outcome(NcfsDecision decision, string expectedOutcome)
            {
                // Arrange
                var file = Encoding.UTF8.GetBytes("I AM FILE");
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;
                var optionalHeaders = new Dictionary<string, string>();

                var outcome = new NcfsOutcome
                {
                    NcfsDecision = decision
                };

                _mockNcfsProcessor.Setup(s => s.GetBlockedActionAsync(It.IsAny<DateTime>(), It.IsAny<string>(),
                    It.IsAny<FileType>())).Returns(Task.FromResult(outcome));

                // Act
                await _fileProcessor.HandleBlockedFile(file, expectedFileId, FileType.Doc, optionalHeaders, expectedTimeStamp);

                // Assert
                _mockTransactionEventSender.Verify(s => s.Send(It.Is<BlockedFiletypeActionEvent>(bfae =>
                    bfae.FileId == expectedFileId &&
                    bfae.Timestamp == expectedTimeStamp &&
                    bfae.Action == expectedOutcome)));
            }

            [TestCase(NcfsDecision.Block, FileOutcome.Failed)]
            [TestCase(NcfsDecision.Relay, FileOutcome.Unmodified)]
            [TestCase(NcfsDecision.Replace, FileOutcome.Replace)]
            public async Task Correct_Status_Is_Returned(NcfsDecision decision, string expectedOutcome)
            {
                // Arrange
                var file = Encoding.UTF8.GetBytes("I AM FILE");
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedTimeStamp = DateTime.UtcNow;
                var optionalHeaders = new Dictionary<string, string>();

                var outcome = new NcfsOutcome
                {
                    NcfsDecision = decision
                };

                _mockNcfsProcessor.Setup(s => s.GetBlockedActionAsync(It.IsAny<DateTime>(), It.IsAny<string>(),
                    It.IsAny<FileType>())).Returns(Task.FromResult(outcome));

                // Act
                var status = await _fileProcessor.HandleBlockedFile(file, expectedFileId, FileType.Doc, optionalHeaders, expectedTimeStamp);

                // Assert
                Assert.That(status, Is.EqualTo(expectedOutcome));
            }
        }
    }
}