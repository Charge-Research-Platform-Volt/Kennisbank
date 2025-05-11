namespace KnowledgeBank.Data
{
    /// <summary>
    /// The filetype class specifies which filetypes are supported by the application in the extToType and is able to convert extensions to their full names.
    /// </summary>
    public static class Filetype
    {
        public struct UploadType 
        {
            public const string Document = "document";
            public const string Audio = "audio";
            public const string Video = "video";
        }
    
        private static readonly Dictionary<string, string> extToType = new()
        {
            // Document
            { "ppt", UploadType.Document },
            { "pptx", UploadType.Document },
            { "doc", UploadType.Document },
            { "docx", UploadType.Document },
            { "pdf", UploadType.Document },
            { "txt", UploadType.Document },
            
            // Audio
            { "mp3", UploadType.Audio },
            { "wav", UploadType.Audio },
            { "ogg", UploadType.Audio },
            
            // Video
            { "mp4", UploadType.Video },
            { "avi", UploadType.Video },
            { "mkv", UploadType.Video },
            { "mov", UploadType.Video },
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
        
        /// <summary>
        /// Retrieve the supported extensions
        /// </summary>
        public static Dictionary<string, string[]> SupportedExtensions 
        {
            get 
            {
                return extToType.GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.Select(pair => pair.Key).ToArray());
            }
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


