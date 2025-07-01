using System.IO;
using System.IO.Compression;

namespace RemoveDevWatermark.Editor
{
    public class ModifiableZip : IModifiableDocument
    {
        private readonly string _zipFilePath;
        private readonly string _entryPath;

        public string Path => $"{_zipFilePath}/{_entryPath}";

        public ModifiableZip(string zipFilePath, string entryPath)
        {
            _zipFilePath = zipFilePath;
            _entryPath = entryPath;
        }
        
        public bool Validate()
        {
            if (!File.Exists(_zipFilePath))
            {
                return false;
            }

            using var archive = ZipFile.Open(_zipFilePath, ZipArchiveMode.Read);
            var entry = archive.GetEntry(_entryPath);
            return entry != null;
        }

        public byte[] ReadAllBytes()
        {
            using var archive = ZipFile.Open(_zipFilePath, ZipArchiveMode.Read);
            var entry = archive.GetEntry(_entryPath)!;
            using var stream = entry.Open();
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        public void WriteAllBytes(byte[] bytes)
        {
            using var archive = ZipFile.Open(_zipFilePath, ZipArchiveMode.Update);
            var entry = archive.GetEntry(_entryPath)!;
            using var stream = entry.Open();
            stream.SetLength(0);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}