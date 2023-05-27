using System;
using System.Collections.Generic;

namespace WManager
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var item in self)
            {
                action(item);
            }
            return self;
        }
    }
}