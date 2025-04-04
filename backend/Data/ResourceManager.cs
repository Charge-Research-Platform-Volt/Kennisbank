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
    }
}
