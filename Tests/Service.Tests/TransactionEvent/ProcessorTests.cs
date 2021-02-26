using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Service.Configuration;
using Service.ErrorReport;
using Service.Messaging;
using Service.Prometheus;
using Service.Storage;
using Service.TransactionEvent;

namespace Service.Tests.TransactionEvent
{
    public class ProcessorTests
    {
        public class ProcessMethod : ProcessorTests
        {
            private Mock<IAdaptationRequestProcessor> _mockAdaptationRequestProcessor;
            private Mock<IOutcomeSender> _mockOutcomeSender;
            private Mock<IFileManager> _mockFileManager;
            private Mock<IErrorReportGenerator> _mockErrorReportGenerator;
            private Mock<IFileProcessorConfig> _mockConfig;
            private Mock<ILogger<Processor>> _mockLogger;

            private Processor _processor;

            [SetUp]
            public void Setup()
            {
                _mockAdaptationRequestProcessor = new Mock<IAdaptationRequestProcessor>();
                _mockOutcomeSender = new Mock<IOutcomeSender>();
                _mockFileManager = new Mock<IFileManager>();
                _mockErrorReportGenerator = new Mock<IErrorReportGenerator>();
                _mockConfig = new Mock<IFileProcessorConfig>();
                _mockLogger = new Mock<ILogger<Processor>>();

                _mockConfig.SetupGet(s => s.ProcessingTimeoutDuration).Returns(TimeSpan.FromSeconds(1));

                _processor = new Processor(
                    _mockAdaptationRequestProcessor.Object,
                    _mockOutcomeSender.Object,
                    _mockFileManager.Object,
                    _mockErrorReportGenerator.Object,
                    _mockConfig.Object,
                    _mockLogger.Object);
            }

            [TearDown]
            public void TearDown()
            {
                MetricsCounters.ProcCnt.WithLabels(Labels.Exception).Dispose();
                MetricsCounters.ProcCnt.WithLabels(Labels.Timeout).Dispose();
            }

            [Test]
            public void Long_Running_Process_Should_Clear_Output_Store()
            {
                // Arrange
                _mockAdaptationRequestProcessor.Setup(s => s.Process())
                    .Callback(() => Task.Delay(TimeSpan.FromMinutes(10)).Wait());
                _mockConfig.SetupGet(s => s.GenerateReport).Returns(false);

                // Act
                _processor.Process();

                // Assert
                _mockFileManager.Verify(m => m.DeleteFile(It.IsAny<string>()), Times.Once, "Store should be cleared in event of long running process");
            }

            [Test]
            public void Long_Running_Process_Should_Send_Failed_Outcome()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedReplyTo = "Reply Here";

                _mockAdaptationRequestProcessor.Setup(s => s.Process())
                    .Callback(() => Task.Delay(TimeSpan.FromMinutes(10)).Wait());
                _mockConfig.SetupGet(s => s.GenerateReport).Returns(false);
                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);
                _mockConfig.SetupGet(s => s.ReplyTo).Returns(expectedReplyTo);

                // Act
                _processor.Process();

                // Assert
                _mockOutcomeSender.Verify(m => m.Send(
                    It.Is<string>(status => status == FileOutcome.Failed),
                    It.Is<string>(id => id == expectedFileId),
                    It.Is<string>(replyTo => replyTo == expectedReplyTo),
                    It.IsAny<Dictionary<string,string>>()),Times.Once, "Failed outcome should be sent in event of long running process");
            }

            [Test]
            public void Long_Running_Process_Should_Generate_Error_Report_If_Configured()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedOutputPath = "Place Error Report Here";
                var generatedReport = "I am the report";

                _mockAdaptationRequestProcessor.Setup(s => s.Process())
                    .Callback(() => Task.Delay(TimeSpan.FromMinutes(10)).Wait());

                _mockErrorReportGenerator.Setup(s => s.CreateReport(It.IsAny<string>())).Returns(generatedReport);

                var expectedBytes = Encoding.UTF8.GetBytes(generatedReport);

                _mockConfig.SetupGet(s => s.GenerateReport).Returns(true);
                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);
                _mockConfig.SetupGet(s => s.OutputPath).Returns(expectedOutputPath);

                // Act
                _processor.Process();

                // Assert
                _mockErrorReportGenerator.Verify(m => m.CreateReport(
                    It.Is<string>(id => id == expectedFileId)), Times.Once, "Error Report should be generated in event of long running process");

                _mockFileManager.Verify(s => s.WriteFile(
                    It.Is<string>(path => path == expectedOutputPath),
                    It.Is<byte[]>(report => report.Where((b, i) => b == expectedBytes[i]).Count() == expectedBytes.Length)), Times.Once);
            }

            [Test]
            public void Long_Running_Process_Should_Increment_Counter_With_Timeout_Label()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();

                _mockAdaptationRequestProcessor.Setup(s => s.Process())
                    .Callback(() => Task.Delay(TimeSpan.FromMinutes(10)).Wait());

                _mockConfig.SetupGet(s => s.GenerateReport).Returns(false);
                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);

                // Act
                _processor.Process();

                // Assert
                Assert.That(MetricsCounters.ProcCnt.WithLabels(Labels.Timeout).Value, Is.EqualTo(1));
            }

            [Test]
            public void Exception_Thrown_Should_Clear_Output_Store()
            {
                // Arrange
                _mockAdaptationRequestProcessor.Setup(s => s.Process()).Throws(new Exception());
                _mockConfig.SetupGet(s => s.GenerateReport).Returns(false);

                // Act
                _processor.Process();

                // Assert
                _mockFileManager.Verify(m => m.DeleteFile(It.IsAny<string>()), Times.Once, "Store should be cleared in event of an exception");
            }

            [Test]
            public void Exception_Thrown_Should_Send_Failed_Outcome()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedReplyTo = "Reply Here";

                _mockAdaptationRequestProcessor.Setup(s => s.Process()).Throws(new Exception());
                _mockConfig.SetupGet(s => s.GenerateReport).Returns(false);
                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);
                _mockConfig.SetupGet(s => s.ReplyTo).Returns(expectedReplyTo);

                // Act
                _processor.Process();

                // Assert
                _mockOutcomeSender.Verify(m => m.Send(
                    It.Is<string>(status => status == FileOutcome.Failed),
                    It.Is<string>(id => id == expectedFileId),
                    It.Is<string>(replyTo => replyTo == expectedReplyTo),
                    It.IsAny<Dictionary<string, string>>()), Times.Once, "Failed outcome should be sent in event of an exception");
            }

            [Test]
            public void Exception_Thrown_Should_Generate_Error_Report_If_Configured()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();
                var expectedOutputPath = "Place Error Report Here";
                var generatedReport = "I am the report";

                _mockAdaptationRequestProcessor.Setup(s => s.Process()).Throws(new Exception());

                _mockErrorReportGenerator.Setup(s => s.CreateReport(It.IsAny<string>())).Returns(generatedReport);

                var expectedBytes = Encoding.UTF8.GetBytes(generatedReport);

                _mockConfig.SetupGet(s => s.GenerateReport).Returns(true);
                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);
                _mockConfig.SetupGet(s => s.OutputPath).Returns(expectedOutputPath);

                // Act
                _processor.Process();

                // Assert
                _mockErrorReportGenerator.Verify(m => m.CreateReport(
                    It.Is<string>(id => id == expectedFileId)), Times.Once, "Error Report should be generated in event of an exception");

                _mockFileManager.Verify(s => s.WriteFile(
                    It.Is<string>(path => path == expectedOutputPath),
                    It.Is<byte[]>(report => report.Where((b, i) => b == expectedBytes[i]).Count() == expectedBytes.Length)), Times.Once);

                var test = MetricsCounters.ProcCnt.WithLabels(Labels.Exception).Value;
                var tes2t = MetricsCounters.ProcCnt.WithLabels(Labels.Timeout).Value;
            }

            [Test]
            public void Exception_Thrown_Should_Increment_Counter_With_Exception_Label()
            {
                // Arrange
                var expectedFileId = Guid.NewGuid().ToString();

                _mockAdaptationRequestProcessor.Setup(s => s.Process()).Throws(new Exception());

                _mockConfig.SetupGet(s => s.GenerateReport).Returns(false);
                _mockConfig.SetupGet(s => s.FileId).Returns(expectedFileId);

                // Act
                _processor.Process();

                // Assert
                Assert.That(MetricsCounters.ProcCnt.WithLabels(Labels.Exception).Value, Is.EqualTo(1));
            }
        }
    }
}
