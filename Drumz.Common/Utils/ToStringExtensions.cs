using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Utils
{
    public static class ToStringExtensions
    {
        public static string ToNiceString<T>(this IEnumerable<T> items)
        {
            return "[" + string.Join<T>(", ", items) + "]";
        }
    }
}
