
using Service.Messaging;
using System;
using System.Threading.Tasks;
using Service.ErrorReport;
using System.Text;
using Microsoft.Extensions.Logging;
using Service.Prometheus;
using Prometheus;
using Service.Configuration;
using Service.Storage;

namespace Service.TransactionEvent
{
    public class Processor : IProcessor
    {
        private readonly IAdaptationRequestProcessor _adaptationRequestProcessor;
        private readonly IOutcomeSender _outcomeSender;
        private readonly IFileManager _fileManager;
        private readonly IErrorReportGenerator _errorReportGenerator;
        private readonly IFileProcessorConfig _config;
        private readonly ILogger<Processor> _logger;

        private readonly TimeSpan _processingTimeoutDuration;

        public Processor(IAdaptationRequestProcessor transactionProcessor, IOutcomeSender outcomeSender, IFileManager fileManager, 
            IErrorReportGenerator errorReportGenerator, IFileProcessorConfig config, ILogger<Processor> logger)
        {
            _adaptationRequestProcessor = transactionProcessor ?? throw new ArgumentNullException(nameof(transactionProcessor));
            _outcomeSender = outcomeSender ?? throw new ArgumentNullException(nameof(outcomeSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _errorReportGenerator = errorReportGenerator ?? throw new ArgumentNullException(nameof(errorReportGenerator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _processingTimeoutDuration = _config.ProcessingTimeoutDuration;
        }

        public void Process()
        {
            using (MetricsCounters.ProcTime.NewTimer())
            {
                var task = Task.Run(() => _adaptationRequestProcessor.Process());

                try
                {
                    bool isCompletedSuccessfully = task.Wait(_processingTimeoutDuration);

                    if (!isCompletedSuccessfully)
                    {
                        MetricsCounters.ProcCnt.WithLabels(Labels.Timeout).Inc();
                        _logger.LogError($"File Id: {_config.FileId} Processing exceeded {_processingTimeoutDuration}s");
                        _fileManager.DeleteFile(_config.OutputPath);
                        CreateErrorReport();
                        _outcomeSender.Send(FileOutcome.Failed, _config.FileId, _config.ReplyTo);
                    }
                }
                catch (Exception e)
                {
                    MetricsCounters.ProcCnt.WithLabels(Labels.Exception).Inc();
                    _logger.LogError($"File Id: {_config.FileId} Processing threw exception {e.Message}");
                    _fileManager.DeleteFile(_config.OutputPath);
                    CreateErrorReport();
                    _outcomeSender.Send(FileOutcome.Failed, _config.FileId, _config.ReplyTo);
                }
            }
        }

        private void CreateErrorReport()
        {
            if (!_config.GenerateReport) return;
            var report = _errorReportGenerator.CreateReport(_config.FileId);
            _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(report));
        }
    } 
}