using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Utils
{
    public static class CollectionExtensionMethods
    {
        public static string ToNiceString<T>(this IEnumerable<T> items)
        {
            return "[" + string.Join<T>(", ", items) + "]";
        }
        public static Dictionary<T, int> IndexOfDictionary<T>(this IEnumerable<T> items)
        {
            var result = new Dictionary<T, int>();
            foreach (var item in items) result.Add(item, result.Count);
            return result;
        }
    }
}
