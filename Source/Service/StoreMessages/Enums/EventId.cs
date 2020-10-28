namespace Service.StoreMessages.Enums
{
    public enum EventId
    {
        Unknown = 0x00,
        NewDocument = 0x10,
        FileTypeDetected = 0x20,
        UnmanagedFiletypeAction = 0x30,
        RebuildStarted = 0x40,
        BlockedFileTypeAction = 0x50,
        RebuildCompleted = 0x60,
        AnalysisCompleted = 0x70,
        NCFSStartedEvent = 0x80,
        NCFSCompletedEvent = 0x90
    }
}
