namespace KnowledgeBank.Utils
{
    /// <summary>
    /// This class contains functions to check validity
    /// 
    /// </summary>
    public static class ValidityUtil
    {
        /// <summary>
        /// Checks if an ID is valid
        /// </summary>
        /// <param name="id">The ID</param>
        /// <returns>A boolean representing if the ID was valid.</returns>
        public static bool IsValidId(string id)
        {
            return !string.IsNullOrEmpty(id) && Guid.TryParse(id, out Guid _);
        }

        /// <summary>
        /// Checks if an URL is valid
        /// </summary>
        /// <param name="url">The URL</param>
        /// <returns>A boolean representing if the URL was valid.</returns>
        public static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? result)
                && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}
