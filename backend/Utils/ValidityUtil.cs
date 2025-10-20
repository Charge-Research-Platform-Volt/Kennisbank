// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich


namespace KnowledgeBank.Utils
{
    /// <summary>
    /// This class contains functions to check validity
    /// 
    /// Author: Abel Dieterich
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
            // a.io is just about the shortest url there is
            // every URL needs at least 1 dot to be valid
            return url.Length > 3 && url.Contains('.');
        }
    }
}
