using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace AcxiomCRM.Common
{
    public static class SanitizationHelper
    {
        private static readonly Regex HtmlTagsRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private static readonly Regex DangerousScriptsRegex = new Regex(@"(?i)<script\b[^>]*>[\s\S]*?<\/script>|javascript:|onload=|onerror=|onclick=", RegexOptions.Compiled);

        public static string? SanitizeText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // Remove potential malicious script patterns
            var cleaned = DangerousScriptsRegex.Replace(input, string.Empty);

            // Strip raw HTML tags to prevent stored XSS attacks
            cleaned = HtmlTagsRegex.Replace(cleaned, string.Empty);

            return cleaned.Trim();
        }

        public static string EncodeForHtml(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return HtmlEncoder.Default.Encode(input);
        }
    }
}
