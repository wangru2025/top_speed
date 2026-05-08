using System.Collections.Generic;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal static class HelpDocumentResolver
    {
        public static IReadOnlyList<string> BuildCandidateFileNames(string fileName, string? languageCode)
        {
            var candidates = new List<string>();
            AddLocalizedCandidates(candidates, fileName, languageCode);
            AddUnique(candidates, fileName);
            return candidates;
        }

        private static void AddLocalizedCandidates(List<string> candidates, string fileName, string? languageCode)
        {
            var normalized = LanguageCode.Normalize(languageCode);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            var dotIndex = fileName.LastIndexOf('.');
            if (dotIndex <= 0 || dotIndex >= fileName.Length - 1)
            {
                AddUnique(candidates, fileName + "." + normalized);
            }
            else
            {
                var stem = fileName.Substring(0, dotIndex);
                var extension = fileName.Substring(dotIndex);
                AddUnique(candidates, stem + "." + normalized + extension);

                var parent = LanguageCode.ParentOf(normalized);
                if (!string.IsNullOrWhiteSpace(parent) && !string.Equals(parent, normalized, System.StringComparison.OrdinalIgnoreCase))
                    AddUnique(candidates, stem + "." + parent + extension);
            }
        }

        private static void AddUnique(List<string> candidates, string fileName)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], fileName, System.StringComparison.OrdinalIgnoreCase))
                    return;
            }

            candidates.Add(fileName);
        }
    }
}
