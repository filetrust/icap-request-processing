using Prometheus;

namespace Service.Prometheus
{
    public static class MetricsCounters
    {
        public static readonly Counter ProcCnt = Metrics.CreateCounter("gw_requestprocessing_proc_total", "Total number of processed files.",
            new CounterConfiguration
            {
                LabelNames = new[] { "outcome" }
            });

        public static readonly Histogram ProcTime = Metrics.CreateHistogram("gw_requestprocessing_proc_time", "Time taken to process file.");
    }
}
