using Azure.Core;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Linq.Expressions;

namespace backend.Data
{
    public partial class ResourceManager
    {
        private readonly DatabaseContext database;

        public ResourceManager(DatabaseContext dbContext)
        {
            database = dbContext;
        }

        #region Changes
        public async Task<bool> RenameResourceAsync(ResourceRenameDto dto)
        {
            Resource? resource = await GetResourceAsync(dto.Id);

            if (resource == null) return false;

            resource.Title = dto.Title;

            await database.SaveResourceChangesAsync();

            return true;
        }
        #endregion

        #region Information
        public async Task<bool> HashExistsAsync(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;

            Resource? resource = await database.Resources.Where(f => f.Hash == hash).FirstOrDefaultAsync();

            return resource == null;
        }
        #endregion

        #region Transaction functions

        public async Task BeginTransaction()
        {
            // Begin a transaction that can be committed or rolled back later
            await database.Database.BeginTransactionAsync();
        }

        public async Task Commit()
        {
            // Commit changes from transaction to database
            if (database.Database.CurrentTransaction != null)
                await database.Database.CurrentTransaction.CommitAsync();
        }

        public async Task Rollback()
        {
            // Roll back the transaction if one exists
            if (database.Database.CurrentTransaction != null)
                await database.Database.CurrentTransaction.RollbackAsync();
        }

        #endregion

        #region Helper functions
        private Guid[] StringToGuidArray(string[] strings)
        {
            Guid[] guids = new Guid[strings.Length];

            for (int i = 0; i < strings.Length; i++)
            {
                guids[i] = Guid.Parse(strings[i]);
            }

            return guids;
        }

        private TFirst[] FirstsOfTupleArray<TFirst, TSecond>((TFirst, TSecond)[] tuples)
        {
            return tuples.Select(tuple => tuple.Item1).ToArray();
        }

        private TSecond[] SecondsOfTupleArray<TFirst, TSecond>((TFirst, TSecond)[] tuples)
        {
            return tuples.Select(tuple => tuple.Item2).ToArray();
        }
        #endregion

        #region Generic retrieval functions

        public async Task<T?> GetAsync<T>(Guid id, DbSet<T> dbSet) where T : class
        { return await dbSet.FindAsync(id); }

        public async Task<T?> GetAsync<T>(string id, DbSet<T> dbSet) where T : class
        { return await GetAsync(Guid.Parse(id), dbSet); }

        public async Task<T[]> GetAllAsync<T, TKey>(DbSet<T> dbSet, Expression<Func<T, TKey>> orderBy) where T : class
        { return await dbSet.OrderBy(orderBy).ToArrayAsync(); }

        public async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet) where T : class
        { return await dbSet.ToArrayAsync(); }

        public async Task<T[]> GetPageAsync<T>(DbSet<T> dbSet, int pageIndex = 1, int pageSize = 100) where T : class
        {
            // Return empty for invalid input
            if (pageIndex < 1 || pageSize < 1) return Array.Empty<T>();

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            return await dbSet.Skip(skip).Take(pageSize).ToArrayAsync();
        }

        public async Task<T[]> GetAllWhereAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        {
            return await dbSet.Where(predicate).ToArrayAsync();
        }

        #endregion

        #region Generic remove relation functions

        public async Task<bool> RemoveWithCompositeKeyAsync<TSet, TKey1, TKey2>(DbSet<TSet> dbSet, TKey1 key1, TKey2 key2) where TSet : class
        {
            TSet? entry = await dbSet.FindAsync(key1, key2);

            if (entry == null) return false;

            dbSet.Remove(entry);

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveAllWhereAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        {
            T[] entries = await dbSet.Where(predicate).ToArrayAsync();

            if (entries.Length == 0) return false;

            dbSet.RemoveRange(entries);

            await database.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
