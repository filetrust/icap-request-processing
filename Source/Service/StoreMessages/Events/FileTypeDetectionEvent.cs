using System;

namespace Service.StoreMessages.Events
{
    public class FileTypeDetectionEvent : Event
    {
        public FileTypeDetectionEvent(string fileType, string fileId, DateTime timestamp) : base(Enums.EventId.FileTypeDetected, fileId, timestamp)
        {
            this.FileType = fileType;
        }

        public string FileType { get; }
    }
}
