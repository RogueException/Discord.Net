namespace Discord
{
    /// <summary> A helper class for formatting characters. </summary>
    public static class Format
    {
        // Characters which need escaping
        private static readonly string[] SensitiveCharacters = {
            "\\", "*", "_", "~", "`", ".", ":", "/" };

        /// <summary> Returns a markdown-formatted string with bold formatting. </summary>
        public static string Bold(string text) => $"**{text}**";
        /// <summary> Returns a markdown-formatted string with italics formatting. </summary>
        public static string Italics(string text) => $"*{text}*";
        /// <summary> Returns a markdown-formatted string with underline formatting. </summary>
        public static string Underline(string text) => $"__{text}__";
        /// <summary> Returns a markdown-formatted string with strikethrough formatting. </summary>
        public static string Strikethrough(string text) => $"~~{text}~~";
        /// <summary> Returns a markdown-formatted URL. Only works in <see cref="EmbedBuilder"/> descriptions and fields. </summary>
        public static string Url(string text, string url) => $"[{text}]({url})";
        /// <summary> Escapes a URL so that a preview is not generated. </summary>
        public static string EscapeUrl(string url) => $"<{url}>";

        /// <summary> Returns a markdown-formatted string with codeblock formatting. </summary>
        public static string Code(string text, string language = null)
        {
            if (language != null || text.Contains("\n"))
                return $"```{language ?? ""}\n{text}\n```";
            else
                return $"`{text}`";
        }

        /// <summary> Sanitizes the string, safely escaping any Markdown sequences. </summary>
        public static string Sanitize(string text)
        {
            foreach (string unsafeChar in SensitiveCharacters)
                text = text.Replace(unsafeChar, $"\\{unsafeChar}");
            return text;
        }
    }
}
