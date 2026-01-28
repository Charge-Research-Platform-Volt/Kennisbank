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
            bool startedTransaction = await BeginTransaction();

            bool updatedTrashedRows = await UpdateResourceAsync(resourceId, resource => resource.Trashed, true);
            bool updatedTrashDateRows =  await UpdateResourceAsync(resourceId, resource => resource.TrashDate, DateTime.UtcNow);

            if (startedTransaction)
                await Commit();

            return updatedTrashDateRows && updatedTrashedRows;
        }

        public async Task<bool> UntrashResourceAsync(Guid resourceId)
        { 
            bool startedTransaction = await BeginTransaction();

            bool updatedTrashedRows = await UpdateResourceAsync(resourceId, resource => resource.Trashed, false);
            bool updatedTrashDateRows =  await UpdateResourceAsync(resourceId, resource => resource.TrashDate, null);

            if (startedTransaction)
                await Commit();
            
            return updatedTrashDateRows && updatedTrashedRows; 
        }

        #endregion

        #region Person Trash
        public async Task<bool> TrashPersonAsync(Guid Id)
        { 
            bool startedTransaction = await BeginTransaction();

            bool updatedTrashedRows = await UpdatePersonAsync(Id, x => x.Trashed, true);
            bool updatedTrashDateRows =  await UpdatePersonAsync(Id, x => x.TrashDate, DateTime.UtcNow);

            if (startedTransaction)
                await Commit();

            return updatedTrashDateRows && updatedTrashedRows;
        }

        public async Task<bool> UntrashPersonAsync(Guid Id)
        { 
            bool startedTransaction = await BeginTransaction();

            bool updatedTrashedRows = await UpdatePersonAsync(Id, x => x.Trashed, false);
            bool updatedTrashDateRows =  await UpdatePersonAsync(Id, x => x.TrashDate, null);

            if (startedTransaction)
                await Commit();
            
            return updatedTrashDateRows && updatedTrashedRows; 
        }

        #endregion

        #region Organisation Trash
        public async Task<bool> TrashOrganisationAsync(Guid Id)
        { 
            bool startedTransaction = await BeginTransaction();

            bool updatedTrashedRows = await UpdateOrganisationAsync(Id, x => x.Trashed, true);
            bool updatedTrashDateRows =  await UpdateOrganisationAsync(Id, x => x.TrashDate, DateTime.UtcNow);

            if (startedTransaction)
                await Commit();

            return updatedTrashDateRows && updatedTrashedRows;
        }

        public async Task<bool> UntrashOrganisationAsync(Guid Id)
        { 
            bool startedTransaction = await BeginTransaction();

            bool updatedTrashedRows = await UpdateOrganisationAsync(Id, x => x.Trashed, false);
            bool updatedTrashDateRows =  await UpdateOrganisationAsync(Id, x => x.TrashDate, null);

            if (startedTransaction)
                await Commit();
            
            return updatedTrashDateRows && updatedTrashedRows; 
        }

        #endregion
    }
}
