using System.IO;

namespace Service
{
    public class LocalFileManager : IFileManager
    {
        public byte[] ReadFile(string path)
        {
            return File.ReadAllBytes(path);
        }

        public void WriteFile(string path, byte[] data)
        {
            File.WriteAllBytes(path, data);
        }
    }
}
