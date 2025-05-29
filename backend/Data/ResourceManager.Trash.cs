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
        #region Resource Trash
            public async Task<bool> TrashResourceAsync(Guid resourceId)
        { 
            await BeginTransaction();

            bool updatedTrashedRows = await UpdateResourceAsync(resourceId, resource => resource.Trashed, true);
            bool updatedTrashDateRows =  await UpdateResourceAsync(resourceId, resource => resource.TrashDate, DateTime.UtcNow);

            await Commit();

            return updatedTrashDateRows && updatedTrashedRows;
        }

        public async Task<bool> TrashResourceAsync(string resourceId)
        { return await TrashResourceAsync(Guid.Parse(resourceId)); }


        public async Task<bool> UntrashResourceAsync(Guid resourceId)
        { 
            await BeginTransaction();

            bool updatedTrashedRows = await UpdateResourceAsync(resourceId, resource => resource.Trashed, false);
            bool updatedTrashDateRows =  await UpdateResourceAsync(resourceId, resource => resource.TrashDate, null);

            await Commit();
            
            return updatedTrashDateRows && updatedTrashedRows; 
        }

        public async Task<bool> UntrashResourceAsync(string resourceId)
        { return await UntrashResourceAsync(Guid.Parse(resourceId)); }
        
        #endregion
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


