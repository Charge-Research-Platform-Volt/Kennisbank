namespace KnowledgeBank.Data
{
    /// <summary>
    /// Single source of truth for supported file types and their properties.
    /// </summary>
    public static class Filetype
    {
        public struct UploadType
        {
            public const string Document = "document";
            public const string Audio = "audio";
            public const string Video = "video";
        }

        private static readonly Dictionary<string, string> supportedDocuments = new()
        {
            { "pdf",  UploadType.Document },
            { "docx", UploadType.Document },
            { "pptx", UploadType.Document },
            { "xlsx", UploadType.Document },
            { "html", UploadType.Document },
            { "txt",  UploadType.Document },
        };

        private static readonly Dictionary<string, string> supportedImages = new()
        {
            { "jpg",  UploadType.Document },
            { "jpeg", UploadType.Document },
            { "png",  UploadType.Document },
            { "bmp",  UploadType.Document },
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

        private static readonly Dictionary<string, string> supportedText =
            supportedDocuments
                .Concat(supportedImages)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        private static readonly Dictionary<string, string> extToType =
            supportedText
                .Concat(supportedAudio)
                .Concat(supportedVideo)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        private static readonly Dictionary<string, string> mimeTypes = new()
        {
            { "pdf",  "application/pdf" },
            { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { "html", "text/html" },
            { "txt",  "text/plain" },
            { "jpg",  "image/jpeg" },
            { "jpeg", "image/jpeg" },
            { "png",  "image/png" },
            { "bmp",  "image/bmp" },
            { "tiff", "image/tiff" },
            { "heif", "image/heif" },
            { "mp3",  "audio/mpeg" },
            { "wav",  "audio/wav" },
            { "ogg",  "audio/ogg" },
            { "mp4",  "video/mp4" },
            { "avi",  "video/x-msvideo" },
            { "mkv",  "video/x-matroska" },
            { "mov",  "video/quicktime" },
        };

        public static string TrimExtension(string extension) =>
            extension.Replace(".", "").Trim().ToLowerInvariant();

        public static string ConvertExtensionToFiletype(string extension) =>
            extToType[TrimExtension(extension)];

        public static bool Supported(string extension) =>
            extToType.ContainsKey(TrimExtension(extension));

        public static bool SupportedText(string extension) =>
            supportedText.ContainsKey(TrimExtension(extension));

        public static bool SupportedDocument(string extension) =>
            supportedDocuments.ContainsKey(TrimExtension(extension));

        public static bool SupportedImage(string extension) =>
            supportedImages.ContainsKey(TrimExtension(extension));

        public static bool SupportedAudio(string extension) =>
            supportedAudio.ContainsKey(TrimExtension(extension));

        public static bool SupportedVideo(string extension) =>
            supportedVideo.ContainsKey(TrimExtension(extension));

        public static string GetMimeType(string extension) =>
            mimeTypes.TryGetValue(TrimExtension(extension), out var mime) ? mime : "application/octet-stream";

        public static bool IsDocumentUrl(string url)
        {
            string ext = TrimExtension(Path.GetExtension(new Uri(url).LocalPath));
            return ext != "html" && SupportedText(ext);
        }

        public static string GetDocumentUrlExtension(string url) =>
            Path.GetExtension(new Uri(url).LocalPath);

        public static Dictionary<string, string[]> SupportedExtensions =>
            extToType.GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.Select(pair => pair.Key).ToArray());
    }
}
