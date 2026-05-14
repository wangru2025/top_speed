using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyNetwork(DriveSettings settings, SettingsNetworkDocument network, List<SettingsIssue> issues)
        {
            if (network.LastServerAddress != null)
                settings.LastServerAddress = network.LastServerAddress;

            settings.DefaultServerPort = ReadDefaultServerPort(network.DefaultServerPort, settings.DefaultServerPort, "network.defaultServerPort", issues);
            if (network.DefaultCallSign != null)
                settings.DefaultCallSign = (network.DefaultCallSign ?? string.Empty).Trim();
            if (network.UseUpdateProxy.HasValue)
                settings.UseUpdateProxy = network.UseUpdateProxy.Value;
            if (network.UpdateProxyUrlPrefix != null)
                settings.UpdateProxyUrlPrefix = (network.UpdateProxyUrlPrefix ?? string.Empty).Trim();
            settings.SavedServers = ParseSavedServers(network.SavedServers?.Servers, issues);
        }

        private static List<SavedServerEntry> ParseSavedServers(List<SettingsSavedServerDocument>? savedServers, List<SettingsIssue> issues)
        {
            var result = new List<SavedServerEntry>();
            if (savedServers == null)
                return result;

            for (var i = 0; i < savedServers.Count; i++)
            {
                var entry = savedServers[i];
                if (entry == null)
                    continue;

                var host = (entry.Host ?? string.Empty).Trim();
                if (host.Length == 0)
                {
                    issues.Add(new SettingsIssue(
                        SettingsIssueSeverity.Warning,
                        $"network.savedServers.servers[{i}]",
                        LocalizationService.Mark("A saved server entry was ignored because the host is empty.")));
                    continue;
                }

                result.Add(new SavedServerEntry
                {
                    Name = (entry.Name ?? string.Empty).Trim(),
                    Host = host,
                    Port = ClampInt(entry.Port, 0, 0, 65535, $"network.savedServers.servers[{i}].port", issues),
                    DefaultCallSign = (entry.DefaultCallSign ?? string.Empty).Trim()
                });
            }

            return result;
        }
    }
}


