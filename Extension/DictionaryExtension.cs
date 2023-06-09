﻿using System;
using System.Collections.Generic;

namespace WManager
{
    public static class DictionaryExtension
    {
        /// <summary>
        /// 遍历字典
        /// </summary>
        /// <param name="action">遍历事件</param>
        public static Dictionary<K, V> ForEach<K, V>(this Dictionary<K, V> self, Action<K, V> action)
        {
            using (var dicE = self.GetEnumerator())
            {
                while (dicE.MoveNext())
                {
                    action(dicE.Current.Key, dicE.Current.Value);
                }
            }
            return self;
        }
        /// <summary>
        /// 合并字典
        /// </summary>
        /// <param name="target">被合并的字典</param>
        /// <param name="isOverride">若存在相同键，是否覆盖对应值</param>
        /// <returns>合并后的字典</returns>
        public static Dictionary<K, V> AddRange<K, V>(this Dictionary<K, V> self, Dictionary<K, V> target, bool isOverride = false)
        {
            using (var dicE = target.GetEnumerator())
            {
                while (dicE.MoveNext())
                {
                    var current = dicE.Current;
                    if (self.ContainsKey(current.Key))
                    {
                        if (isOverride)
                        {
                            self[current.Key] = current.Value;
                            continue;
                        }
                    }
                    self.Add(current.Key, current.Value);
                }
            }
            return self;
        }
    }
}