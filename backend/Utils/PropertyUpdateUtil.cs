// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using System.Linq.Expressions;
using System.Reflection;
using KnowledgeBank.Data;

namespace KnowledgeBank.Utils 
{
    public static class PropertyUpdateUtil 
    {
        /// <summary>
        /// Converts an object to its target type
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <param name="targetType">The target type</param>
        public static object ConvertValue(object value, Type targetType)
        {
            // Check for null
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            
            // Handle Nullable<T>
            Type? underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                // If value is empty string or "null" and we're converting to nullable, return null
                if (value is string strValue && (string.IsNullOrEmpty(strValue) || strValue.Equals("null", StringComparison.OrdinalIgnoreCase)))
                    return null;
                
                // Otherwise convert to the underlying type
                targetType = underlyingType;
            }
            
            // Handle enum conversion
            if (targetType.IsEnum)
            {
                if (value is string stringValue)
                {
                    return Enum.Parse(targetType, stringValue, true);
                }
                else if (value is long || value is int || value is short || value is byte)
                {
                    return Enum.ToObject(targetType, value);
                }
            }
            
            // Handle Guid conversion
            if (targetType == typeof(Guid) && value is string guidString)
            {
                return Guid.Parse(guidString);
            }
            
            // Handle DateTime conversion
            if (targetType == typeof(DateTime) && value is string dateString)
            {
                return DateTime.Parse(dateString);
            }
            
            // Handle boolean conversion
            if (targetType == typeof(bool) && value is string boolString)
            {
                if (boolString.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    boolString.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                    boolString.Equals("1"))
                    return true;
                    
                if (boolString.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    boolString.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    boolString.Equals("0"))
                    return false;
            }
            
            // If the value is already of the correct type, return it as is
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }
            
            // For complex objects from System.Text.Json
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                try
                {
                    switch (jsonElement.ValueKind)
                    {
                        case System.Text.Json.JsonValueKind.String:
                            // For string-based types, try specialized conversion
                            string stringValue = jsonElement.GetString();
                            
                            if (targetType == typeof(string))
                                return stringValue;
                            else if (targetType == typeof(Guid))
                                return Guid.Parse(stringValue);
                            else if (targetType == typeof(DateTime))
                                return DateTime.Parse(stringValue);
                            else if (targetType.IsEnum)
                                return Enum.Parse(targetType, stringValue, true);
                            else
                                return Convert.ChangeType(stringValue, targetType);
                        
                        case System.Text.Json.JsonValueKind.Number:
                            // For numeric types
                            if (targetType == typeof(int) || targetType == typeof(Int32))
                                return jsonElement.GetInt32();
                            else if (targetType == typeof(long) || targetType == typeof(Int64))
                                return jsonElement.GetInt64();
                            else if (targetType == typeof(double))
                                return jsonElement.GetDouble();
                            else if (targetType == typeof(decimal))
                                return jsonElement.GetDecimal();
                            else if (targetType == typeof(float))
                                return jsonElement.GetSingle();
                            else if (targetType == typeof(short) || targetType == typeof(Int16))
                                return (short)jsonElement.GetInt32();
                            else if (targetType == typeof(byte))
                                return (byte)jsonElement.GetInt32();
                            else if (targetType.IsEnum)
                                return Enum.ToObject(targetType, jsonElement.GetInt32());
                            else
                                return Convert.ChangeType(jsonElement.GetDouble(), targetType);
                        
                        case System.Text.Json.JsonValueKind.True:
                            return true;
                        
                        case System.Text.Json.JsonValueKind.False:
                            return false;
                        
                        case System.Text.Json.JsonValueKind.Object:
                        case System.Text.Json.JsonValueKind.Array:
                            // For complex objects, use full JSON deserialization
                            string json = jsonElement.GetRawText();
                            return System.Text.Json.JsonSerializer.Deserialize(json, targetType);
                        
                        default:
                            throw new InvalidOperationException($"Unsupported JSON value kind: {jsonElement.ValueKind}");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to convert JSON element to {targetType.Name}: {ex.Message}", ex);
                }
            }
            
            // Finally, try standard conversion
            return Convert.ChangeType(value, targetType);
        }
        
        /// <summary>
        /// Invokes a generic method with the specified types using reflection
        /// </summary>
        /// <param name="instance">The instance to invoke the method on</param>
        /// <param name="methodName">Name of the method to invoke</param>
        /// <param name="id">First parameter to pass to the method</param>
        /// <param name="propertyName">Second parameter to pass to the method</param>
        /// <param name="value">Third parameter to pass to the method</param>
        /// <param name="setType">First generic type parameter</param>
        /// <param name="propertyType">Second generic type parameter</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task InvokeGenericMethodAsync(
            object instance,
            string methodName,
            string id,
            string propertyName,
            object value,
            Type setType,
            Type propertyType)
        {
            // Find the specified method
            MethodInfo method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                ?? throw new Exception($"{methodName} method is missing");
            
            // Make it generic with the specific types
            MethodInfo genericMethod = method.MakeGenericMethod(setType, propertyType);
            
            // Invoke it and handle possible null return
            object? result = genericMethod.Invoke(instance, [id, propertyName, value])
                ?? throw new Exception($"Error invoking {methodName}");

            await (Task)result;
        }
        
        /// <summary>
        /// Creates a property selector expression to be used in EF
        /// </summary>
        /// <typeparam name="TSet">The type of the set</typeparam>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>An expression that can be used with EF</returns>
        public static Expression<Func<TSet, TProperty>> CreatePropertySelector<TSet, TProperty>(string propertyName)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TSet), "item");
            MemberExpression property = Expression.Property(parameter, propertyName);
            return Expression.Lambda<Func<TSet, TProperty>>(property, parameter);
        }

        /// <summary>
        /// Updates the properties of a database entry of the given type with the given ID with the given property names to the given new values
        /// </summary>
        /// <param name="instance">The instance where the update method exists</param>
        /// /// <param name="methodName">The name of the update method in the given instance</param>
        /// <param name="type">The type of the database entry</param>
        /// <param name="id">The ID of the databse entry</param>
        /// <param name="updates">A dictionary of parameter names and their new values</param>
        /// <returns>A list of successfully updated parameters</returns>
        public static async Task<List<string>> UpdateProperties(object instance, string methodName, Type type, string id, Dictionary<string, object> updates)
        {
            // Get the properties of the type
            PropertyInfo[] props = type.GetProperties();

            List<string> updatedProperties = [];

            foreach (KeyValuePair<string, object> update in updates)
            {
                // Try to find the property
                PropertyInfo? prop = props.FirstOrDefault(p => string.Equals(p.Name, update.Key, StringComparison.OrdinalIgnoreCase));

                // Prop was not found
                if (prop == null) continue;

                // Convert the incoming value to the correct type
                var typedValue = ConvertValue(update.Value, prop.PropertyType);

                // Use reflection to determine type at runtime and update the property
                await InvokeGenericMethodAsync(instance, methodName, id, prop.Name, typedValue, type, prop.PropertyType);

                // Add property to updated list
                updatedProperties.Add(prop.Name);
            }

            return updatedProperties;
        }
    }
}