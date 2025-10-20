namespace KnowledgeBank.Data
{
    /// <summary>
    /// This class is responsible for all database interactions regarding projects.
    /// 
    /// Author: Justin Liem
    /// </summary>
    /// <param name="dbContext">The database context variable</param>
    public partial class ProjectManager(DatabaseContext dbContext)
    {
        private readonly DatabaseContext database = dbContext;

        #region Transaction functions
        
        /// <summary>
        /// Starts a database transaction
        /// 
        /// Author: Justin Liem
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
        /// 
        /// Author: Justin Liem
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
        /// 
        /// Author: Justin Liem
        /// </summary>
        public async Task Rollback()
        {
            // Roll back the transaction if one exists
            if (database.Database.CurrentTransaction != null)
                await database.Database.CurrentTransaction.RollbackAsync();
        }

        #endregion

        #region Helper functions

        /// <summary>
        /// Converts a string array to guid array
        /// </summary>
        /// <param name="strings">Strings to convert.</param>
        /// <returns>Guids of converted string.</returns>
        private static Guid[] StringToGuidArray(string[] strings)
        {
            Guid[] guids = new Guid[strings.Length];

            for (int i = 0; i < strings.Length; i++)
            {
                guids[i] = Guid.Parse(strings[i]);
            }

            return guids;
        }

        /// <summary>
        /// Gets all the first values of a tuple array and outputs it as an array
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="TFirst">Type of first item in tuple list.</typeparam>
        /// <typeparam name="TSecond">Type of second item in tuple list.</typeparam>
        /// <param name="tuples">Tuples to convert.</param>
        /// <returns>Array of the first item of the tuples.</returns>
        private static TFirst[] FirstsOfTupleArray<TFirst, TSecond>((TFirst, TSecond)[] tuples)
        {
            return tuples.Select(tuple => tuple.Item1).ToArray();
        }

        /// <summary>
        /// Gets all the second values of a tuple array and outputs it as an array
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="TFirst">Type of first item in tuple list.</typeparam>
        /// <typeparam name="TSecond">Type of second item in tuple list.</typeparam>
        /// <param name="tuples">Tuples to convert.</param>
        /// <returns>Array of the second items of the tuples.</returns>
        private static TSecond[] SecondsOfTupleArray<TFirst, TSecond>((TFirst, TSecond)[] tuples)
        {
            return tuples.Select(tuple => tuple.Item2).ToArray();
        }
        #endregion
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


