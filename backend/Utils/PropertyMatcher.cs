using System.Linq.Expressions;
using System.Reflection;

namespace KnowledgeBank.Utils;


/// <summary>
/// A utility class that provides dynamic property matching and sorting functionality for any object type.
/// It allows searching for properties by name, including case-insensitive and partial matches, 
/// and enables sorting collections based on property values using reflection.
/// </summary>
public static class PropertyMatcher
{
    /// <summary>
    /// The default property to fall back on if no match is found.
    /// </summary>
    private static string DefaultPropertyName { get; set; } = "Name";

    /// <summary>
    /// Finds the property on an object by name, ignoring case and handles partial matches.
    /// If no match is found, it returns the default property specified by <see cref="DefaultPropertyName"/>.
    /// </summary>
    /// <param name="obj">The object containing the property.</param>
    /// <param name="propertyName">The name of the property to search for.</param>
    /// <returns>
    /// The <see cref="PropertyInfo"/> of the matched property.
    /// Throws an <see cref="InvalidOperationException"/> if no match is found and no default property is available.
    /// </returns>
    private static PropertyInfo GetMatchingProperty(object obj, string propertyName)
    {
        // Clean up the input property name
        string cleanPropertyName = propertyName
            .Replace("-", "")
            .Replace("_", "")
            .Trim();

        // Get all properties of the object's type
        PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Look for an exact match first, if not found look for a partial match
        PropertyInfo? matchedProperty = 
            properties
                .FirstOrDefault(prop => prop.Name.Equals(cleanPropertyName, StringComparison.OrdinalIgnoreCase)) 
            ?? properties
                .FirstOrDefault(prop => prop.Name.Contains(cleanPropertyName, StringComparison.OrdinalIgnoreCase));

        // If still no match, take the default property
        if (matchedProperty == null)
        {
            matchedProperty = properties
                .FirstOrDefault(prop => prop.Name.Equals(DefaultPropertyName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Could not find the default property '{DefaultPropertyName}' on type '{obj.GetType().Name}'.");
        }

        return matchedProperty;
    }

    /// <summary>
    /// Sorts a collection of objects by a property dynamically using reflection.
    /// </summary>
    /// <param name="collection">The collection to sort</param>
    /// <param name="propertyName">The name of the property to sort by</param>
    /// <param name="descending">True to sort in descending order, false for ascending</param>
    /// <returns>The sorted collection</returns>
    public static IEnumerable<T> SortByProperty<T>(IEnumerable<T> collection, string propertyName, bool descending)
    {   
        // Take object from collection, throw error if collection is empty
        object? obj = collection.First() 
            ?? throw new InvalidOperationException("The collection is empty");
        
        // Get the property matching with propertyName
        PropertyInfo property = GetMatchingProperty(obj, propertyName);

        // Step 1: Create a parameter expression that represents an individual item in the collection (e.g., 'Tag')
        // 'T' is the type of elements in the collection, and "x" is the name of the parameter representing each element.
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        
        // Step 2: Create an expression to access the specific property on the parameter ('x') of type 'T'.
        // 'property' is the PropertyInfo of the property to be accessed (e.g., 'CreatedOn').
        MemberExpression propertyAccess = Expression.Property(parameter, property);
        
        // Step 3: Create a lambda expression 'x => x.Property' from the property access expression.
        // This expression represents a function that takes an element 'x' and returns the value of the 'property' (converted to an object).
        // Uses Expression.Convert to ensure the result is of type 'object', which is required for LINQ operations.
        Expression<Func<T, object>> lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(propertyAccess, typeof(object)), parameter);

        return descending 
            ? collection.AsQueryable().OrderByDescending(lambda)
            : collection.AsQueryable().OrderBy(lambda);
    }
    
    /// <summary>
    /// Sorts a collection of objects by a property dynamically using reflection.
    /// </summary>
    /// <param name="collection">The collection to sort</param>
    /// <param name="propertyName">The name of the property to sort by</param>
    /// <param name="descending">True to sort in descending order, false for ascending</param>
    /// <param name="customDefaultPropertyName">The custom default property name, matching falls back to this property name if no match found</param>
    /// <returns>The sorted collection</returns>
    public static IEnumerable<T> SortByProperty<T>(IEnumerable<T> collection, string propertyName, bool descending, string customDefaultPropertyName)
    {
        DefaultPropertyName = customDefaultPropertyName;
        
        return SortByProperty(collection, propertyName, descending);
    }   
    
    /// <summary>
    /// Sorts a collection of objects based on a weighted combination of properties specified in a string expression.
    /// </summary>
    /// <typeparam name="T">Type of objects in the collection</typeparam>
    /// <param name="collection">The collection to sort</param>
    /// <param name="weightedSortExpression">A string containing property-weight pairs, e.g. "PropertyA:2,PropertyB:1"</param>
    /// <param name="descending">True to sort in descending order (higher scores first), false for ascending</param>
    /// <returns>The sorted collection</returns>
    public static IEnumerable<T> SortByWeightedProperties<T>(
        IEnumerable<T> collection,
        string weightedSortExpression,
        bool descending = true)
    {
        if (collection == null || !collection.Any())
            throw new InvalidOperationException("The collection is empty");

        Dictionary<string, double>? weights = ParseWeightedSortExpression(weightedSortExpression);
        if (!weights.Any())
            return collection;

        object? sampleItem = collection.First()
            ?? throw new InvalidOperationException("The collection is empty");

        // Prepare matched properties once per sort operation
        object? obj = collection.First()
            ?? throw new InvalidOperationException("The object is null");
        
        Dictionary<string, PropertyInfo>? matchedProperties = weights.Keys
            .ToDictionary(
                key => key,
                key => GetMatchingProperty(obj, key),
                StringComparer.OrdinalIgnoreCase);

        return descending
            ? collection.OrderByDescending(item => CalculateWeightedScore(item, weights, matchedProperties))
            : collection.OrderBy(item => CalculateWeightedScore(item, weights, matchedProperties));;
    }
    
    /// <summary>
    /// Parses a weighted sort expression string into a dictionary of property names and weights.
    /// </summary>
    /// <param name="expression">Format: "PropertyName:Weight,PropertyName2:Weight2,..."</param>
    /// <returns>Dictionary mapping property names to their weights</returns>
    private static Dictionary<string, double> ParseWeightedSortExpression(string expression)
    {
        Dictionary<string, double>? result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrWhiteSpace(expression))
            return result;
            
        foreach (string? pair in expression.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[]? parts = pair.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && double.TryParse(parts[1], out double weight))
            {
                result[parts[0].Trim()] = weight;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculates a weighted score for an object based on its property values and weights.
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    /// <param name="item">The object to calculate a score for</param>
    /// <param name="weights">Dictionary mapping property names to their weights</param>
    /// <param name="matchedProperties">Dictionary mapping property names to the actual property</param>
    /// <returns>The calculated score</returns>
    private static double CalculateWeightedScore<T>(
        T item, 
        Dictionary<string, double> weights,
        Dictionary<string, PropertyInfo> matchedProperties)
    {
        double score = 0;

        foreach (KeyValuePair<string, double> pair in weights)
        {
            if (!matchedProperties.TryGetValue(pair.Key, out PropertyInfo? property))
                continue;

            object? value = property.GetValue(item);
            if (value == null)
                continue;

            if (value is bool boolValue)
                score += boolValue ? pair.Value : 0;
            else if (value is int intValue)
                score += intValue * pair.Value;
            else if (value is double doubleValue)
                score += doubleValue * pair.Value;
            else if (value is float floatValue)
                score += floatValue * pair.Value;
            else if (value is decimal decimalValue)
                score += (double)decimalValue * pair.Value;
            else if (value is long longValue)
                score += longValue * pair.Value;
            // We can add more type handling here if necessary
        }

        return score;
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

