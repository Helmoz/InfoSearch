using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace InfoSearch.Extensions
{
    public static class CollectionExtensions
    {
        public static bool NotContains<T>(this IEnumerable<T> source, T element)
        {
            return !source.Contains(element);
        }

        public static void SafeAdd<T>(this ConcurrentBag<T> bag, T elem)
        {
            if (bag.NotContains(elem))
            {
                bag.Add(elem);
            }
        }
    }
}