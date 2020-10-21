namespace Service
{
    public interface IFileProcessorConfig
    {
        public string FileId { get; }
        public string InputPath { get; }
        public string OutputPath { get; }
        public string ReplyTo { get; }
    }
}