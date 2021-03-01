using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using Service.TransactionEvent;

namespace Service.Tests.TransactionEvent
{
    public class AdaptationRequestControllerTests
    {
        public class ProcessRequestMethod : AdaptationRequestControllerTests
        {
            private const string ReplyTo = "I AM THE REPLY TO";
            private const string InputPath = "I AM THE INPUT PATH";
            private const string OutputPath = "I AM THE OUTPUT PATH";
            private const bool GenerateReport = true;

            private readonly string _fileId = Guid.NewGuid().ToString();
            private readonly string _policyId = Guid.NewGuid().ToString();
            private readonly DateTime _timeStamp = DateTime.UtcNow;

            private Mock<IFileProcessor> _mockFileProcessor;
            private Mock<ILogger<AdaptationRequestController>> _mockLogger;

            private Mock<Action<string, string, string, Dictionary<string, string>>> _mockOnFinishEvent;
            private Mock<Action<string, string, bool>> _mockOnFailedEvent;
            private Mock<Action<string, string, string, string, string>> _mockOnArchiveEvent;
            private Mock<Func<byte[], string, FileType, Dictionary<string, string>, DateTime, Task<string>>> _mockOnUnmanagedEvent;
            private Mock<Func<byte[], string, FileType, Dictionary<string, string>, DateTime, Task<string>>> _mockOnBlockedEvent;

            private AdaptationRequestController _adaptationRequestController;
            private AdaptationContext _context;

            [SetUp]
            public void Setup()
            {
                _mockFileProcessor = new Mock<IFileProcessor>();
                _mockLogger = new Mock<ILogger<AdaptationRequestController>>();

                _mockOnFinishEvent = new Mock<Action<string, string, string, Dictionary<string, string>>>();
                _mockOnFailedEvent = new Mock<Action<string, string, bool>>();
                _mockOnArchiveEvent = new Mock<Action<string, string, string, string, string>>();
                _mockOnUnmanagedEvent = new Mock<Func<byte[], string, FileType, Dictionary<string, string>, DateTime, Task<string>>>();
                _mockOnBlockedEvent = new Mock<Func<byte[], string, FileType, Dictionary<string, string>, DateTime, Task<string>>>();

                _adaptationRequestController = new AdaptationRequestController(
                    _mockFileProcessor.Object,
                    _mockLogger.Object);

                _context = new AdaptationContext
                {
                    FileId = _fileId,
                    PolicyId = _policyId,
                    TimeStamp = _timeStamp,
                    ReplyTo = ReplyTo,
                    InputPath = InputPath,
                    OutputPath = OutputPath,
                    GenerateErrorReport = GenerateReport,
                    OnFinishEvent = _mockOnFinishEvent.Object,
                    OnFailedEvent = _mockOnFailedEvent.Object,
                    OnArchiveEvent = _mockOnArchiveEvent.Object,
                    OnUnmanagedEvent = _mockOnUnmanagedEvent.Object,
                    OnBlockedEvent = _mockOnBlockedEvent.Object
                };
            }

            [Test]
            public async Task Unknown_FileType_Calls_OnUnmanagedEvent_And_Outcome_Is_Used_For_OnFinishEvent()
            {
                // Arrange
                const string expectedOutcome = FileOutcome.Replace;
                const FileType returnedFileType = FileType.Unknown;

                var fileBytes = Encoding.UTF8.GetBytes("I AM THE FILE");

                _mockFileProcessor.Setup(s => s.HandleNewFileRead(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DateTime>())).Returns(fileBytes);

                _mockFileProcessor
                    .Setup(s => s.HandleFileTypeDetection(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Returns(returnedFileType);

                _mockOnUnmanagedEvent
                    .Setup(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(),
                        It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(expectedOutcome));

                // Act
                await _adaptationRequestController.ProcessRequest(_context);

                // Assert
                _mockOnUnmanagedEvent.Verify(x => x(
                    It.Is<byte[]>(file => file.Where((b, i) => b == fileBytes[i]).Count() == fileBytes.Length), 
                    It.Is<string>(fileId => fileId == _fileId), 
                    It.Is<FileType>(fileType => fileType == returnedFileType),
                    It.IsAny<Dictionary<string, string>>(), 
                    It.Is<DateTime>(timeStamp => timeStamp == _timeStamp)), Times.Once);

                _mockOnFinishEvent.Verify(x => x(
                    It.Is<string>(outcome => outcome == expectedOutcome),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<string>(replyTo => replyTo == ReplyTo),
                    It.IsAny<Dictionary<string, string>>()), Times.Once);

                _mockOnArchiveEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                _mockOnBlockedEvent.Verify(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()), Times.Never);
                _mockOnFailedEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }

            [Test]
            public async Task OnFailedEvent_Is_Called_When_OnUnmanagedEvent_Returns_Failed_Outcome()
            {
                // Arrange
                const string expectedOutcome = FileOutcome.Failed;
                const FileType returnedFileType = FileType.Unknown;

                var fileBytes = Encoding.UTF8.GetBytes("I AM THE FILE");

                _mockFileProcessor.Setup(s => s.HandleNewFileRead(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DateTime>())).Returns(fileBytes);

                _mockFileProcessor
                    .Setup(s => s.HandleFileTypeDetection(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Returns(returnedFileType);

                _mockOnUnmanagedEvent
                    .Setup(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(),
                        It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(expectedOutcome));

                // Act
                await _adaptationRequestController.ProcessRequest(_context);

                // Assert
                _mockOnUnmanagedEvent.Verify(x => x(
                    It.Is<byte[]>(file => file.Where((b, i) => b == fileBytes[i]).Count() == fileBytes.Length),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<FileType>(fileType => fileType == returnedFileType),
                    It.IsAny<Dictionary<string, string>>(),
                    It.Is<DateTime>(timeStamp => timeStamp == _timeStamp)), Times.Once);

                _mockOnFinishEvent.Verify(x => x(
                    It.Is<string>(outcome => outcome == expectedOutcome),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<string>(replyTo => replyTo == ReplyTo),
                    It.IsAny<Dictionary<string, string>>()), Times.Once);

                _mockOnFailedEvent.Verify(x => x(
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<string>(output => output == OutputPath),
                    It.Is<bool>(errorReport => errorReport == GenerateReport)), Times.Once);

                _mockOnArchiveEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                _mockOnBlockedEvent.Verify(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()), Times.Never);
            }

            [TestCase(FileType.Zip)]
            [TestCase(FileType.Rar)]
            [TestCase(FileType.Gzip)]
            [TestCase(FileType.Tar)]
            [TestCase(FileType.SevenZip)]
            public async Task Archive_FileType_Calls_OnArchiveEvent_Only(FileType archiveFileType)
            {
                // Arrange
                const string expectedOutcome = FileOutcome.Replace;

                var fileBytes = Encoding.UTF8.GetBytes("I AM THE FILE");

                _mockFileProcessor.Setup(s => s.HandleNewFileRead(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DateTime>())).Returns(fileBytes);

                _mockFileProcessor
                    .Setup(s => s.HandleFileTypeDetection(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Returns(archiveFileType);

                _mockOnUnmanagedEvent
                    .Setup(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(),
                        It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(expectedOutcome));

                // Act
                await _adaptationRequestController.ProcessRequest(_context);

                // Assert
                _mockOnArchiveEvent.Verify(x => x(
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<string>(fileType => fileType == archiveFileType.ToString()),
                    It.Is<string>(input => input == InputPath),
                    It.Is<string>(output => output == OutputPath),
                    It.Is<string>(replyTo => replyTo == ReplyTo)), Times.Once);

                _mockOnUnmanagedEvent.Verify(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()), Times.Never);
                _mockOnFinishEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
                _mockOnBlockedEvent.Verify(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()), Times.Never);
                _mockOnFailedEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }

            [Test]
            public async Task FailedRebuild_Calls_OnBlockedEvent_And_Outcome_Is_Used_For_OnFinishEvent()
            {
                // Arrange
                const string expectedOutcome = FileOutcome.Unmodified;
                const FileType returnedFileType = FileType.Doc;

                var fileBytes = Encoding.UTF8.GetBytes("I AM THE FILE");

                _mockFileProcessor.Setup(s => s.HandleNewFileRead(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DateTime>())).Returns(fileBytes);

                _mockFileProcessor
                    .Setup(s => s.HandleFileTypeDetection(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Returns(returnedFileType);

                _mockFileProcessor.Setup(s => s.HandleRebuild(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(), 
                        It.IsAny<string>(), It.IsAny<ContentManagementFlags>(), It.IsAny<DateTime>()))
                    .Returns(FileOutcome.Failed);

                _mockOnBlockedEvent
                    .Setup(s => s(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(),
                        It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(expectedOutcome));

                // Act
                await _adaptationRequestController.ProcessRequest(_context);

                // Assert
                _mockOnBlockedEvent.Verify(x => x(
                    It.Is<byte[]>(file => file.Where((b, i) => b == fileBytes[i]).Count() == fileBytes.Length),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<FileType>(fileType => fileType == returnedFileType),
                    It.IsAny<Dictionary<string, string>>(),
                    It.Is<DateTime>(timeStamp => timeStamp == _timeStamp)), Times.Once);

                _mockOnFinishEvent.Verify(x => x(
                    It.Is<string>(outcome => outcome == expectedOutcome),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<string>(replyTo => replyTo == ReplyTo),
                    It.IsAny<Dictionary<string, string>>()), Times.Once);

                _mockOnUnmanagedEvent.Verify(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()), Times.Never);
                _mockOnArchiveEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                _mockOnFailedEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }

            [Test]
            public async Task OnFailedEvent_Is_Called_When_OnBlockedEvent_Returns_Failed_Outcome()
            {
                // Arrange
                const string expectedOutcome = FileOutcome.Failed;
                const FileType returnedFileType = FileType.Doc;

                var fileBytes = Encoding.UTF8.GetBytes("I AM THE FILE");

                _mockFileProcessor.Setup(s => s.HandleNewFileRead(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DateTime>())).Returns(fileBytes);

                _mockFileProcessor
                    .Setup(s => s.HandleFileTypeDetection(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Returns(returnedFileType);

                _mockFileProcessor.Setup(s => s.HandleRebuild(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(),
                        It.IsAny<string>(), It.IsAny<ContentManagementFlags>(), It.IsAny<DateTime>()))
                    .Returns(FileOutcome.Failed);

                _mockOnBlockedEvent
                    .Setup(s => s(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(),
                        It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(expectedOutcome));

                // Act
                await _adaptationRequestController.ProcessRequest(_context);

                // Assert
                _mockOnBlockedEvent.Verify(x => x(
                    It.Is<byte[]>(file => file.Where((b, i) => b == fileBytes[i]).Count() == fileBytes.Length),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<FileType>(fileType => fileType == returnedFileType),
                    It.IsAny<Dictionary<string, string>>(),
                    It.Is<DateTime>(timeStamp => timeStamp == _timeStamp)), Times.Once);

                _mockOnFinishEvent.Verify(x => x(
                    It.Is<string>(outcome => outcome == expectedOutcome),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<string>(replyTo => replyTo == ReplyTo),
                    It.IsAny<Dictionary<string, string>>()), Times.Once);

                _mockOnFailedEvent.Verify(x => x(
                    It.Is<string>(fileId => fileId == _fileId), 
                    It.Is<string>(output => output == OutputPath), 
                    It.Is<bool>(errorReport => errorReport == GenerateReport)), Times.Once);

                _mockOnUnmanagedEvent.Verify(x => x(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime>()), Times.Never);
                _mockOnArchiveEvent.Verify(x => x(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }

            [Test]
            public async Task OptionalHeaders_Are_Passed_To_OnFinishedEvent()
            {
                // Arrange
                const string headerKey = "outcome-header-Content-Type";
                const string headerMimeType = "text/html";
                const string expectedOutcome = FileOutcome.Replace;
                const FileType returnedFileType = FileType.Doc;

                _context.OptionalHeaders = new Dictionary<string, string> {{headerKey, headerMimeType}};

                var fileBytes = Encoding.UTF8.GetBytes("I AM THE FILE");

                _mockFileProcessor.Setup(s => s.HandleNewFileRead(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DateTime>())).Returns(fileBytes);

                _mockFileProcessor
                    .Setup(s => s.HandleFileTypeDetection(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Returns(returnedFileType);

                _mockFileProcessor.Setup(s => s.HandleRebuild(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<FileType>(),
                        It.IsAny<string>(), It.IsAny<ContentManagementFlags>(), It.IsAny<DateTime>()))
                    .Returns(FileOutcome.Replace);

                // Act
                await _adaptationRequestController.ProcessRequest(_context);

                // Assert
                _mockOnFinishEvent.Verify(x => x(
                    It.Is<string>(outcome => outcome == expectedOutcome),
                    It.Is<string>(fileId => fileId == _fileId),
                    It.Is<string>(replyTo => replyTo == ReplyTo),
                    It.Is<Dictionary<string, string>>(dict => dict.ContainsKey(headerKey) && dict[headerKey] == headerMimeType)));
            }
        }
    }
}