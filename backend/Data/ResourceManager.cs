using Azure.Core;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
/// <summary>
/// ResourceManager is a class that manages the resources in the database.
/// </summary>
    public partial class ResourceManager
    {
        private readonly DatabaseContext database;

        /// <summary>
        /// ResourceManager constructor that takes a DatabaseContext as a parameter.
        /// </summary>
        /// <param name="dbContext">A DatabaseContext variable</param>
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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


