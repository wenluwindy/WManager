using System;

namespace WManager
{
    public static class ArrayExtension
    {
        /// <summary>
        /// 遍历
        /// </summary>
        /// <param name="action">遍历事件</param>
        public static T[] ForEach<T>(this T[] self, Action<int, T> action)
        {
            for (int i = 0; i < self.Length; i++)
            {
                action(i, self[i]);
            }
            return self;
        }
        /// <summary>
        /// 倒序遍历
        /// </summary>
        /// <param name="action">遍历事件</param>
        public static T[] ForEachReverse<T>(this T[] self, Action<T> action)
        {
            for (int i = self.Length - 1; i >= 0; i--)
            {
                action(self[i]);
            }
            return self;
        }
        /// <summary>
        /// 倒序遍历
        /// </summary>
        /// <param name="action">遍历事件</param>
        public static T[] ForEachReverse<T>(this T[] self, Action<int, T> action)
        {
            for (int i = self.Length - 1; i >= 0; i--)
            {
                action(i, self[i]);
            }
            return self;
        }
        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="target">合并的目标</param>
        /// <returns>返回一个新的Array 包含被合并的两个Array中的所有元素</returns>
        public static T[] Merge<T>(this T[] self, T[] target)
        {
            T[] mg = new T[self.Length + target.Length];
            Array.Copy(self, 0, mg, 0, self.Length);
            Array.Copy(target, 0, mg, self.Length, target.Length);
            return mg;
        }
        /// <summary>
        /// 插入排序
        /// </summary>
        /// <returns>返回排序后的数组</returns>
        public static int[] SortInsertion(this int[] self)
        {
            int[] array = self;
            for (int i = 1; i < array.Length; i++)
            {
                int t = array[i];
                int j = i;
                while ((j > 0) && (array[j - 1] > t))
                {
                    array[j] = array[j - 1];
                    --j;
                }
                array[j] = t;
            }
            return array;
        }
        /// <summary>
        /// 希尔排序
        /// </summary>
        /// <returns>返回排序后的数组</returns>
        public static int[] SortShell(this int[] self)
        {
            int[] array = self;
            int inc;
            for (inc = 1; inc <= array.Length / 9; inc = 3 * inc + 1) ;
            for (; inc > 0; inc /= 3)
            {
                for (int i = inc + 1; i <= array.Length; i += inc)
                {
                    int t = array[i - 1];
                    int j = i;
                    while ((j > inc) && (array[j - inc - 1] > t))
                    {
                        array[j - 1] = array[j - inc - 1];
                        j -= inc;
                    }
                    array[j - 1] = t;
                }
            }
            return array;
        }
        /// <summary>
        /// 选择排序
        /// </summary>
        /// <returns>返回排序后的数组</returns>
        public static int[] SortSelection(this int[] self)
        {
            int[] array = self;
            int min;
            for (int i = 0; i < array.Length - 1; i++)
            {
                min = i;
                for (int j = i + 1; j < array.Length; j++)
                {
                    if (array[j] < array[min])
                        min = j;
                }
                int t = array[min];
                array[min] = array[i];
                array[i] = t;
            }
            return array;
        }
        /// <summary>
        /// 冒泡排序
        /// </summary>
        /// <returns>返回排序后的数组</returns>
        public static int[] SortBubble(this int[] self)
        {
            int[] array = self;
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = i; j < array.Length; j++)
                {
                    if (array[i] < array[j])
                    {
                        int temp = array[i];
                        array[i] = array[j];
                        array[j] = temp;
                    }
                }
            }
            return array;
        }
    }
}