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

        private static readonly Dictionary<string, string> supportedText = new()
        {
            // Document
            { "pdf", UploadType.Document }, 
            { "docx", UploadType.Document },
            { "pptx", UploadType.Document },
            { "xlsx", UploadType.Document },
            { "html", UploadType.Document },
            { "txt", UploadType.Document }, 
            
            // Images (OCR supported)
            { "jpg", UploadType.Document }, 
            { "jpeg", UploadType.Document },
            { "png", UploadType.Document }, 
            { "bmp", UploadType.Document }, 
            { "tiff", UploadType.Document },
            { "heif", UploadType.Document },
        };

        private static readonly Dictionary<string, string> supportedAudio = new()
        {
            { "mp3", UploadType.Audio },
            { "wav", UploadType.Audio },
            { "ogg", UploadType.Audio },
        };

        private static readonly Dictionary<string, string> supportedVideo = new()
        {
            { "mp4", UploadType.Video },
            { "avi", UploadType.Video },
            { "mkv", UploadType.Video },
            { "mov", UploadType.Video },
        };

        // Combine all supported extensions into one dictionary
        private static readonly Dictionary<string, string> extToType =
            supportedText
                .Concat(supportedAudio)
                .Concat(supportedVideo)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>
        /// Trims the extension
        /// </summary>
        /// <param name="extension">The full extension (.pdf for example)</param>
        /// <returns></returns>
        public static string TrimExtension(string extension)
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
            return extToType[TrimExtension(extension)];
        }

        /// <summary>
        /// Use the Supported function to check if a filetype is supported.
        /// </summary>
        /// <param name="extension">The full extension (.pdf for example)</param>
        /// <returns></returns>
        public static bool Supported(string extension)
        {
            return extToType.ContainsKey(TrimExtension(extension));
        }

        /// <summary>
        /// Check if a text/document extension is supported
        /// </summary>
        /// <param name="extension">The full extension (.pdf for example)</param>
        /// <returns></returns>
        public static bool SupportedText(string extension)
        {
            return supportedText.ContainsKey(TrimExtension(extension));
        }

        /// <summary>
        /// Check if an audio extension is supported
        /// </summary>
        /// <param name="extension">The full extension (.mp3 for example)</param>
        /// <returns></returns>
        public static bool SupportedAudio(string extension)
        {
            return supportedAudio.ContainsKey(TrimExtension(extension));
        }

        /// <summary>
        /// Check if a video extension is supported
        /// </summary>
        /// <param name="extension">The full extension (.mp4 for example)</param>
        /// <returns></returns>
        public static bool SupportedVideo(string extension)
        {
            return supportedVideo.ContainsKey(TrimExtension(extension));
        }

        /// <summary>
        /// Retrieve all supported extensions grouped by type
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
