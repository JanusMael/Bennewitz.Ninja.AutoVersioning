using System;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

public static class ArrayExtensions
{
    public static T[]? ToArray<T>(this Array? array)
    {
        var typeOfT = typeof(T);
        T[]? result;
        if (array == null)
        {
            result = null;
        }
        else if (array.Length == 0)
        {
            result = Array.Empty<T>();
        }
        else
        {
            result = new T[array.Length];
            int i = 0;
            foreach (var item in array)
            {
                result[i] = (T) Convert.ChangeType(item, typeOfT);
                i++;
            }
        }

        return result;
    }
}