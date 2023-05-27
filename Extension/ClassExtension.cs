using System;

namespace WManager
{
    public static class ClassExtension 
    {
        /// <summary>
        /// 如果对象不为null则执行事件
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static T Execute<T>(this T self, Action<T> action) where T : class
        {
            if (null != self)
            {
                action(self);
            }
            return self;
        }
    }
}