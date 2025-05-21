// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    // This part is for updating properties
    public partial class ResourceManager
    {
        #region Resource Archive
            public async Task<bool> ArchiveResourceAsync(Guid resourceId)
        { 
            await BeginTransaction();

            bool updatedArchivedRows = await UpdateResourceAsync(resourceId, resource => resource.Archived, true);
            bool updatedArchiveDateRows =  await UpdateResourceAsync(resourceId, resource => resource.ArchiveDate, DateTime.UtcNow);

            await Commit();

            return updatedArchiveDateRows && updatedArchivedRows;
        }

        public async Task<bool> ArchiveResourceAsync(string resourceId)
        { return await ArchiveResourceAsync(Guid.Parse(resourceId)); }


        public async Task<bool> UnarchiveResourceAsync(Guid resourceId)
        { 
            await BeginTransaction();

            bool updatedArchivedRows = await UpdateResourceAsync(resourceId, resource => resource.Archived, false);
            bool updatedArchiveDateRows =  await UpdateResourceAsync(resourceId, resource => resource.ArchiveDate, null);

            await Commit();
            
            return updatedArchiveDateRows && updatedArchivedRows; 
        }

        public async Task<bool> UnarchiveResourceAsync(string resourceId)
        { return await UnarchiveResourceAsync(Guid.Parse(resourceId)); }
        
        #endregion
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


