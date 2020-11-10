namespace Service
{
    public interface IFileManager
    {
        byte[] ReadFile(string path);
        void WriteFile(string path, byte[] data);
    }
}
