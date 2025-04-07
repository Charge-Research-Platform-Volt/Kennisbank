using System.Reflection;

/// <summary>
/// Static class that can compare anonymous response types.
/// </summary>
public static class ObjectComparer
{
    /// <summary>
    /// Compares two anonymous response type objects.
    /// </summary>
    /// <param name="obj1">The first object to compare.</param>
    /// <param name="obj2">The second object to compare.</param>
    /// <returns>
    // Returns true if both objects have the same properties,
    // and these properties have the same values.
    /// </returns>
    public static bool AreObjectsEqual(object obj1, object obj2)
    {
        if (obj1 == null && obj2 == null)
            return true;

        if (obj1 == null || obj2 == null)
            return false;

        // Get properties of both objects
        PropertyInfo[] properties1 = obj1.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] properties2 = obj2.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (properties1.Length != properties2.Length)
        {
            return false;
        }

        for (int i = 0; i < properties1.Length - 1; i++)
        {
            if (properties1[i].GetValue(obj1) != properties2[i].GetValue(obj2))
            {
                return false;
            }
        }

        // All properties are equal
        return true;
    }
}