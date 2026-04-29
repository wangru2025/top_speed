namespace TopSpeed.Runtime
{
    public interface IDocumentOpener
    {
        bool TryOpenDocument(string path, string contentType, out string errorMessage);
    }
}
