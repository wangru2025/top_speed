using System;
using System.IO;

namespace TopSpeed.Vehicles
{
    internal sealed partial class VehicleRadioController
    {
        public void Dispose()
        {
            DisposeSource();
        }

        private void DisposeSource()
        {
            if (_source != null)
            {
                _source.Stop();
                _source.Dispose();
                _source = null;
            }

            if (!string.IsNullOrWhiteSpace(_ownedTempFile))
            {
                SafeDelete(_ownedTempFile!);
                _ownedTempFile = null;
            }
        }

        private void ReplaceOwnedTempFile(string? path)
        {
            if (!string.IsNullOrWhiteSpace(_ownedTempFile) && !string.Equals(_ownedTempFile, path, StringComparison.OrdinalIgnoreCase))
                SafeDelete(_ownedTempFile!);
            _ownedTempFile = path;
        }

        private static void SafeDelete(string? path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort cleanup only.
            }
        }
    }
}

