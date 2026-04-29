using System;
using System.IO;

namespace TopSpeed.Localization
{
    public static class LocalizationBootstrap
    {
        public const string ClientCatalogGroup = "client";
        public const string ServerCatalogGroup = "server";
        private static readonly object Sync = new object();
        private static string? _languagesRoot;

        public static void SetLanguagesRoot(string? languagesRoot)
        {
            var trimmed = languagesRoot?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            lock (Sync)
                _languagesRoot = trimmed;
        }

        public static void Configure(string? languageCode, string? catalogGroup = null)
        {
            var languagesRoot = ResolveLanguagesRoot(catalogGroup);
            var localizer = CatalogLocalizer.Create(languageCode, languagesRoot);
            LocalizationService.SetLocalizer(localizer);
        }

        public static string ResolveLanguagesRoot(string? catalogGroup = null)
        {
            string? root;
            lock (Sync)
                root = _languagesRoot;

            if (string.IsNullOrWhiteSpace(root))
                root = Path.Combine(AppContext.BaseDirectory, "languages");

            var group = catalogGroup?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(group)
                ? root!
                : Path.Combine(root!, group);
        }
    }
}
