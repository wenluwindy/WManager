using System;
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
            if (self == null) return null;
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
        /// <param name="isOverride">若存在相同键，是否覆盖对应值。默认 false（保留 self 中的旧值）</param>
        /// <returns>合并后的字典（即 self 本身）</returns>
        public static Dictionary<K, V> AddRange<K, V>(this Dictionary<K, V> self, Dictionary<K, V> target, bool isOverride = false)
        {
            if (self == null) return null;
            if (target == null) return self;

            using (var dicE = target.GetEnumerator())
            {
                while (dicE.MoveNext())
                {
                    var current = dicE.Current;
                    if (self.ContainsKey(current.Key))
                    {
                        // 已存在：仅当 isOverride=true 时才覆盖，否则跳过
                        if (isOverride)
                        {
                            self[current.Key] = current.Value;
                        }
                        // 修复前这里漏了 continue / else，会穿透到下面的 self.Add 导致重复键异常
                    }
                    else
                    {
                        self.Add(current.Key, current.Value);
                    }
                }
            }
            return self;
        }

        /// <summary>
        /// 尝试往字典里加一对 key/value；key 已存在则保留旧值并返回 false。
        /// </summary>
        public static bool TryAdd<K, V>(this Dictionary<K, V> self, K key, V value)
        {
            if (self == null) return false;
            if (self.ContainsKey(key)) return false;
            self.Add(key, value);
            return true;
        }

        /// <summary>
        /// 字典是否包含所有指定 key（全部存在才返回 true）。
        /// </summary>
        public static bool ContainsAllKeys<K, V>(this Dictionary<K, V> self, params K[] keys)
        {
            if (self == null || keys == null) return false;
            for (int i = 0; i < keys.Length; i++)
            {
                if (!self.ContainsKey(keys[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// 字典是否包含任一指定 key（任一存在即返回 true）。
        /// </summary>
        public static bool ContainsAnyKey<K, V>(this Dictionary<K, V> self, params K[] keys)
        {
            if (self == null || keys == null) return false;
            for (int i = 0; i < keys.Length; i++)
            {
                if (self.ContainsKey(keys[i])) return true;
            }
            return false;
        }
    }
}