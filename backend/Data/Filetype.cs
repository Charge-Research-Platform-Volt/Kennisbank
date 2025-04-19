namespace KnowledgeBank.Data
{
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

        private static string trimExtension(string extension)
        {
            return extension.Replace(".", "").Trim();
        }

        public static string ConvertExtensionToFiletype(string extension)
        {
            return extToType[trimExtension(extension)];
        }

        public static bool Supported(string extension)
        {
            return extToType.ContainsKey(trimExtension(extension));
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


