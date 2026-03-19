using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Helper
{
    public static class CodeGeneratorHelper
    {
        /// <summary>
        /// Generate organization code: {ORG_ALIAS_3}{GUID6}
        /// Example: VINF3CB22
        /// </summary>
        public static string GenerateOrganizationCode(string organizationName)
        {
            var orgAlias = GetOrganizationAlias(organizationName);
            var guid6 = GetGuid6();
            return $"{orgAlias}{guid6}";
        }

        /// <summary>
        /// Generate subscription code: {ORG_CODE}-{YEAR}-{GUID6}
        /// Example: VINF3CB22-2025-A91FBB
        /// </summary>
        public static string GenerateSubscriptionCode(string organizationCode, DateTime startDate)
        {
            var year = startDate.Year;
            var guid6 = GetGuid6();
            return $"{organizationCode}-{year}-{guid6}";
        }

        /// <summary>
        /// Get first 3 characters of organization name (uppercase, alphanumeric only)
        /// </summary>
        private static string GetOrganizationAlias(string organizationName)
        {
            if (string.IsNullOrWhiteSpace(organizationName))
                throw new ArgumentException("Organization name cannot be empty");

            // Remove diacritics (Vietnamese accents) and special characters
            var normalized = RemoveDiacritics(organizationName);

            // Keep only alphanumeric characters and spaces
            var cleaned = Regex.Replace(normalized, @"[^a-zA-Z0-9\s]", "");

            // Remove extra spaces and get first 3 characters
            var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var alias = new StringBuilder();

            foreach (var word in words)
            {
                if (alias.Length >= 3) break;

                foreach (var ch in word)
                {
                    if (alias.Length >= 3) break;
                    if (char.IsLetterOrDigit(ch))
                        alias.Append(char.ToUpper(ch));
                }
            }

            // If still less than 3 characters, pad with 'X'
            while (alias.Length < 3)
                alias.Append('X');

            return alias.ToString().Substring(0, 3);
        }

        /// <summary>
        /// Get first 6 characters of a new GUID (uppercase)
        /// </summary>
        private static string GetGuid6()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }

        /// <summary>
        /// Remove Vietnamese diacritics
        /// </summary>
        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
