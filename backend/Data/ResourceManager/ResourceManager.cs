// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Authors: Abel Dieterich, Elia Jabbour (AI parts and RAG system)

using KnowledgeBank.Services;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Search.Models;
using Serilog;

namespace KnowledgeBank.Data
{
    /// <summary>
    /// This class is responsible for all database interactions regarding resources and their metadata.
    ///
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="dbContext">The database context variable</param>
    /// <param name="ragSystem">The RAG system variable</param>
    /// <param name="hybridSearchService">The hybrid search service variable</param>
    public partial class ResourceManager(DatabaseContext dbContext, RAGSystem ragSystem, HybridSearchService hybridSearchService)
    {
        private readonly DatabaseContext database = dbContext;
        private readonly Serilog.ILogger _logger = Log.ForContext<ResourceManager>();
        private readonly RAGSystem _ragSystem = ragSystem;
        private readonly HybridSearchService _hybridSearchService = hybridSearchService;


        #region Transaction functions

        /// <summary>
        /// Starts a database transaction
        /// </summary>
        /// <returns>If the transaction was started or not (if false, there was already a transaction running)</returns>
        public async Task<bool> BeginTransaction()
        {
            // Begin a transaction that can be committed or rolled back later
            if (database.Database.CurrentTransaction == null)
            {
                await database.Database.BeginTransactionAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Commits the current database transaction
        /// </summary>
        public async Task Commit()
        {
            // Commit changes from transaction to database
            if (database.Database.CurrentTransaction != null)
            {
                await database.SaveChangesAsync();
                await database.Database.CurrentTransaction.CommitAsync();
            }
        }

        /// <summary>
        /// Rolls back the current database transaction
        /// </summary>
        public async Task Rollback()
        {
            // Roll back the transaction if one exists
            if (database.Database.CurrentTransaction != null)
                await database.Database.CurrentTransaction.RollbackAsync();
        }

        #endregion

        #region Helper functions
        // Converts a string array to guid array
        private static Guid[] StringToGuidArray(string[]? strings)
        {
            if (strings == null || strings.Length == 0) return [];

            Guid[] guids = new Guid[strings.Length];

            for (int i = 0; i < strings.Length; i++)
            {
                guids[i] = Guid.Parse(strings[i]);
            }

            return guids;
        }
        #endregion
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


