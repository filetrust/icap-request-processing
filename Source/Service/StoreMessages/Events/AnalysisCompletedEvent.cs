using System;

namespace Service.StoreMessages.Events
{
    public class AnalysisCompletedEvent : Event
    {
        public AnalysisCompletedEvent(string analysisReport, string fileId, DateTime timestamp) : base(Enums.EventId.AnalysisCompleted, fileId, timestamp)
        {
            this.AnalysisReport = analysisReport;
        }

        public string AnalysisReport { get; }
    }
}
