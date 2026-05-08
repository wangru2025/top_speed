using TopSpeed.Runtime;

namespace TopSpeed.iOS;

internal sealed class IosUpdatePackageInstaller : IUpdatePackageInstaller
{
    public bool TryInstallPackage(string packagePath, out string errorMessage)
    {
        errorMessage = "iOS does not support in-place package installs from inside the app.";
        return false;
    }
}
