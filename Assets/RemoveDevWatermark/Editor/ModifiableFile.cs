using System.IO;

namespace RemoveDevWatermark.Editor
{
    public class ModifiableFile : IModifiableDocument
    {
        public string Path { get; }

        public ModifiableFile(string path)
        {
            Path = path;
        }

        public bool Validate()
        {
            return File.Exists(Path);
        }

        public byte[] ReadAllBytes()
        {
            return File.ReadAllBytes(Path);
        }

        public void WriteAllBytes(byte[] bytes)
        {
            File.WriteAllBytes(Path, bytes);
        }
    }
}