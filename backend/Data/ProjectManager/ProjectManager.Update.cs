using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {

        #region Generic functions

        protected async Task<int> UpdateProjectAsync<T, TProperty>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Expression<Func<T, TProperty>> propertySelector, TProperty newValue) where T : class
        {
            bool startedTransaction = await BeginTransaction();
            int count = await dbSet.Where(predicate).ExecuteUpdateAsync(s => s.SetProperty(e => EF.Property<TProperty>(e, GetPropertyName(propertySelector)), _ => newValue));
            if (startedTransaction) await Commit();
            return count;
        }

        private string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a property access expression.", nameof(propertyExpression));
        }

        #endregion

        #region Projects / folders
        // Used to alter properties of projects / folders themselves
        public async Task<bool> UpdateProjectAsync<T>(Guid id, Expression<Func<Project, T>> propertySelector, T newValue)
        { return await UpdateProjectAsync(database.Projects, resource => resource.Id == id, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateProjectAsync<T>(string id, Expression<Func<Project, T>> propertySelector, T newValue)
        { return await UpdateProjectAsync(Guid.Parse(id), propertySelector, newValue); }

        public async Task<bool> UpdateProjectAsync<T>(Expression<Func<Project, bool>> predicate, Expression<Func<Project, T>> propertySelector, T newValue)
        { return await UpdateProjectAsync(database.Projects, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Tags

        public async Task<bool> UpdateProjectTagsAsync(Guid id, Guid[] newValue)
        {
            await RemoveAllTagsFromProject(id);
            await AddTagToProjectRangeAsync(id, newValue);
            return true;
        }

        public async Task<bool> UpdateProjectTagsAsync(string id, string[] newValue)
        {
            return await UpdateProjectTagsAsync(Guid.Parse(id), newValue.Select(Guid.Parse).ToArray());
        }

        public async Task<bool> UpdateProjectTagsAsync(Guid id, string[] newValue)
        {
            return await UpdateProjectTagsAsync(id, newValue.Select(Guid.Parse).ToArray());
        }

        public async Task<bool> UpdateProjectTagsAsync(string id, Guid[] newValue)
        {
            return await UpdateProjectTagsAsync(Guid.Parse(id), newValue);
        }

        #endregion

        #region Creators

        public async Task<bool> UpdateProjectCreatorsAsync(Guid id, Guid[] newValue)
        {
            await RemoveAllCreatorsFromProject(id);
            await AddCreatorToProjectRangeAsync(id, newValue);
            return true;
        }

        public async Task<bool> UpdateProjectCreatorsAsync(string id, string[] newValue)
        {
            return await UpdateProjectCreatorsAsync(Guid.Parse(id), newValue.Select(Guid.Parse).ToArray());
        }

        public async Task<bool> UpdateProjectCreatorsAsync(Guid id, string[] newValue)
        {
            return await UpdateProjectCreatorsAsync(id, newValue.Select(Guid.Parse).ToArray());
        }

        public async Task<bool> UpdateProjectCreatorsAsync(string id, Guid[] newValue)
        {
            return await UpdateProjectCreatorsAsync(Guid.Parse(id), newValue);
        }

        #endregion
    }
}