// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Authors: Abel Dieterich, Elia Jabbour (AI parts and RAG system)

using KnowledgeBank.Services;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Search.Models;
using KnowledgeBank.Services.Vector;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Data
{
    /// <summary>
    /// This class is responsible for all database interactions regarding resources and their metadata.
    ///
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="dbFactory">The database context variable</param>
    /// <param name="ragSystem">The RAG system variable</param>
    /// <param name="hybridSearchService">The hybrid search service variable</param>
    public partial class ResourceManager(IDbContextFactory<DatabaseContext> dbFactory, RAGSystem ragSystem, HybridSearchService hybridSearchService, IVectorStore vectorStore)
    {
        private readonly Serilog.ILogger _logger = Log.ForContext<ResourceManager>();
        private readonly IDbContextFactory<DatabaseContext> _dbFactory = dbFactory;
        private readonly RAGSystem _ragSystem = ragSystem;
        private readonly HybridSearchService _hybridSearchService = hybridSearchService;
        private readonly IVectorStore _vectorStore = vectorStore;

        // Shared context - lazily created and reused for all operations
        private DatabaseContext? _database;
        protected DatabaseContext database => _database ??= _dbFactory.CreateDbContext();

        /// <summary>
        /// Creates a new database context for operations that need their own context lifecycle
        /// </summary>
        protected async Task<DatabaseContext> CreateContextAsync() => await _dbFactory.CreateDbContextAsync();

        #region Transaction functions

        /// <summary>
        /// Starts a database transaction on the shared context
        /// </summary>
        /// <returns>If the transaction was started or not (if false, there was already a transaction running)</returns>
        public async Task<bool> BeginTransaction()
        {
            return await BeginTransaction(database);
        }

        /// <summary>
        /// Starts a database transaction on a specific context
        /// </summary>
        public async Task<bool> BeginTransaction(DatabaseContext db)
        {
            if (db.Database.CurrentTransaction == null)
            {
                await db.Database.BeginTransactionAsync();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Commits the current database transaction on the shared context
        /// </summary>
        public async Task Commit()
        {
            await Commit(database);
        }

        /// <summary>
        /// Commits the current database transaction on a specific context
        /// </summary>
        public async Task Commit(DatabaseContext db)
        {
            if (db.Database.CurrentTransaction != null)
            {
                await db.SaveChangesAsync();
                await db.Database.CurrentTransaction.CommitAsync();
            }
        }

        /// <summary>
        /// Rolls back the current database transaction on the shared context
        /// </summary>
        public async Task Rollback()
        {
            await Rollback(database);
        }

        /// <summary>
        /// Rolls back the current database transaction on a specific context
        /// </summary>
        public async Task Rollback(DatabaseContext db)
        {
            if (db.Database.CurrentTransaction != null)
                await db.Database.CurrentTransaction.RollbackAsync();
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
