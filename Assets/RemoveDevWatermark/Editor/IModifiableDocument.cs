namespace RemoveDevWatermark.Editor
{
    public interface IModifiableDocument
    {
        string Path { get; }
        bool Validate();
        byte[] ReadAllBytes();
        void WriteAllBytes(byte[] bytes);
    }
}