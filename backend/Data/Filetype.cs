namespace KnowledgeBank.Data
{
    /// <summary>
    /// The filetype class specifies which filetypes are supported by the application in the extToType and is able to convert extensions to their full names.
    /// </summary>
    public static class Filetype
    {
        private static Dictionary<string, string> extToType = new Dictionary<string, string>
        {
            // Powerpoint
            { "ppt", "powerpoint" },
            { "pptx", "powerpoint" },

            // Word
            { "doc", "word" },
            { "docx", "word" },

            // PDF
            { "pdf", "pdf" },

            // Text
            { "txt", "text" },
        };

        /// <summary>
        /// Trims the extension
        /// </summary>
        /// <param name="extension">The full extension (.pdf for example)</param>
        /// <returns></returns>
        private static string trimExtension(string extension)
        {
            return extension.Replace(".", "").Trim();
        }

        /// <summary>
        /// Use the ConvertExtensionToFiletype function to display the full name of the extension.
        /// </summary>
        /// <param name="extension">The full extension (.pdf for example)</param>
        /// <returns></returns>
        public static string ConvertExtensionToFiletype(string extension)
        {
            return extToType[trimExtension(extension)];
        }

        /// <summary>
        /// Use the Supported function to check if a filetype is supported.
        /// </summary>
        /// <param name="extension">The full extension (.pdf for example)</param>
        /// <returns></returns>
        public static bool Supported(string extension)
        {
            return extToType.ContainsKey(trimExtension(extension));
        }
    }
}
