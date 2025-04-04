using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models;

namespace backend.Data
{
    public partial class ResourceManager
    {
        #region Resource

        // Hash exists

        public async Task<bool> HashExistsAsync(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;

            return await GetFirstWhereAsync(database.Resources, resource => resource.Hash == hash) == null;
        }

        #endregion
    }
}
