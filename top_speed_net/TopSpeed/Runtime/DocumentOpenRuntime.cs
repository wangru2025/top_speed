using System;

namespace TopSpeed.Runtime
{
    public static class DocumentOpenRuntime
    {
        private static readonly object Sync = new object();
        private static IDocumentOpener? _opener;

        public static void SetOpener(IDocumentOpener? opener)
        {
            IDocumentOpener? previous;
            lock (Sync)
            {
                previous = _opener;
                _opener = opener;
            }

            if (previous is IDisposable disposable)
                disposable.Dispose();
        }

        public static bool TryOpenDocument(string path, string contentType, out string errorMessage)
        {
            IDocumentOpener? opener;
            lock (Sync)
                opener = _opener;

            if (opener == null)
            {
                errorMessage = "No platform document opener is available.";
                return false;
            }

            return opener.TryOpenDocument(path, contentType, out errorMessage);
        }
    }
}
