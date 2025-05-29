using System.Reflection;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Data
{
    /// <summary>
    /// Provides methods to generate DTOs from models with support for nested relations.
    /// 
    /// Author: Abel Dietrich
    /// </summary>
    public static class DtoGenerator
    {
        /// <summary>
        /// Converts an entity to a DTO, including specified properties.
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entity">The entity instance</param>
        /// <param name="includeProperties">Relations to include in the DTO (supports dot notation like "Authors.Person")</param>
        /// <returns>An object representing the DTO</returns>
        public static object ToDto<T>(T entity, bool useCamelCase = true, params string[] includeProperties)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Parse the include properties to handle nested relations
            Dictionary<string, HashSet<string>> includePaths = ParseIncludePaths(includeProperties);

            // Convert entity to a DTO
            return ConvertEntityToDto(entity, includePaths, useCamelCase);
        }

        /// <summary>
        /// Convert an entity to a DTO, including specified properties.
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="entity">The entity instance</param>
        /// <param name="includePaths">Relations to include in the DTO (supports dot notation like "Authors.Person")</param>
        /// <param name="useCamelCase">Whether to convert property names to camelCase</param>
        /// <returns>An object representing the DTO</returns>
        private static Dictionary<string, object?> ConvertEntityToDto(object entity, Dictionary<string, HashSet<string>> includePaths, bool useCamelCase)
        {
            Type entityType = entity.GetType();
            Dictionary<string, object?> dto = [];

            // Process properties
            foreach (PropertyInfo prop in entityType.GetProperties())
            {
                // Skip if this property has JsonIgnore and is not in includePaths
                JsonIgnoreAttribute? jsonIgnore = prop.GetCustomAttribute<JsonIgnoreAttribute>();
                if (jsonIgnore != null && !includePaths.ContainsKey(prop.Name))
                    continue;

                string propKey = useCamelCase ? ToCamelCase(prop.Name) : prop.Name;

                // Add primitive properties directly
                if (IsPrimitiveOrString(prop.PropertyType))
                    dto[propKey] = prop.GetValue(entity);

                // Process included relations
                else if (includePaths.TryGetValue(prop.Name, out HashSet<string>? nestedIncludes))
                {
                    object? value = prop.GetValue(entity);

                    // Skip null relations
                    if (value == null)
                        continue;

                    // Handle collections
                    if (IsGenericCollection(prop.PropertyType))
                    {
                        if (value is System.Collections.IEnumerable collection)
                        {
                            List<object> dtoList = [];

                            foreach (object item in collection)
                            {
                                // Recursively convert each item in the collection
                                dtoList.Add(ConvertEntityToDto(item, ParseIncludePaths(nestedIncludes), useCamelCase));
                            }

                            dto[propKey] = dtoList;
                        }
                    }

                    // Handle single navigation property
                    else
                    {
                        object nestedDto = ConvertEntityToDto(value, ParseIncludePaths(nestedIncludes), useCamelCase);
                        dto[propKey] = nestedDto;
                    }
                }
            }

            return dto;
        }

        /// <summary>
        /// Converts a string from PascalCase to camelCase
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>The converted string in camelCase</returns>
        private static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Create nested include paths for the next level of relations
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="nestedIncludes">The nested include paths to parse</param>
        /// <returns>A dictionary where keys are root properties and values are sets of nested properties</returns>
        private static Dictionary<string, HashSet<string>> ParseIncludePaths(IEnumerable<string> nestedIncludes)
        {
            Dictionary<string, HashSet<string>> result = [];

            foreach (string include in nestedIncludes)
            {
                string[] parts = include.Split('.');
                string root = parts[0];

                if (!result.ContainsKey(root))
                    result[root] = [];

                // If there are further nested parts, add them
                if (parts.Length > 1)
                    result[root].Add(string.Join('.', parts.Skip(1)));
            }

            return result;
        }

        /// <summary>
        /// Check if a type is primitive, string, or other simple type
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is primitive, string, or a simple type, otherwise false</returns>
        private static bool IsPrimitiveOrString(Type type)
        {
            return type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(Guid) ||
                type == typeof(TimeSpan) ||
                type == typeof(DateTimeOffset) ||
                type.IsEnum ||
                Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Check if a type is a generic collection
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is a collection (array or generic IEnumerable), otherwise false</returns>
        private static bool IsGenericCollection(Type type)
        {
            return type != typeof(string) && (type.IsArray || (type.IsGenericType && (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))));
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


