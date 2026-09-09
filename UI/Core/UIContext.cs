using System.Collections.Generic;

namespace WManager
{
    /// <summary>
    /// 面板传参容器。用于给 OnWillOpen / OnDidOpen 携带任意数据，
    /// 避免 OpenAsync 的参数越加越多。
    ///
    /// 需要类型安全的强类型参数时，优先继承 <see cref="UIPanel{TArgs}"/>，
    /// 那条路径内部也是用这个容器承载的。
    /// </summary>
    public class UIContext
    {
        /// <summary><see cref="UIPanel{TArgs}"/> 承载强类型参数用的固定键名</summary>
        public const string ArgsKey = "__args";

        private readonly Dictionary<string, object> _data = new();

        /// <summary>面板数量</summary>
        public int Count => _data.Count;

        /// <summary>
        /// 构造一个只携带强类型参数的上下文
        /// </summary>
        public static UIContext Of<T>(T args)
        {
            return new UIContext().Set(ArgsKey, args);
        }

        public UIContext Set<T>(string key, T value)
        {
            _data[key] = value;
            return this;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var value) && value is T typed)
                return typed;

            return defaultValue;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public bool Has(string key)
        {
            return _data.ContainsKey(key);
        }

        public object GetRaw(string key)
        {
            return _data.TryGetValue(key, out var value) ? value : null;
        }

        public bool Remove(string key)
        {
            return _data.Remove(key);
        }

        public void Clear()
        {
            _data.Clear();
        }
    }
}
