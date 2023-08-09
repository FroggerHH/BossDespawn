using System.Collections.Generic;
using UnityEngine;

internal static class ListExtension
{
    internal static string GetString<T>(this List<T> list)
    {
        string result = string.Empty;
        for (var i = 0; i < list.Count; i++)
        {
            var element = list[i];
            if (i > 0) result += ", ";
            result += element.ToString();
        }

        return result;
    }
}