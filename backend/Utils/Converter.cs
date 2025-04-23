// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

namespace KnowledgeBank.Utils 
{
    public static class Converter 
    {
        public static object ConvertValue(object value, Type targetType)
        {
            // Check for null
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
            
            // Handle Nullable<T>
            Type underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                // If value is empty string or "null" and we're converting to nullable, return null
                if (value is string strValue && (string.IsNullOrEmpty(strValue) || strValue.Equals("null", StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }
                
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
    }
}