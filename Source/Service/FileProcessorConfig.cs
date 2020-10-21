namespace Service
{
    public class FileProcessorConfig : IFileProcessorConfig
    {
        public string FileId { get; set; }
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string ReplyTo { get; set; }
    }
}