using System;
using System.Collections.Generic;

namespace WManager
{
    /// <summary>
    /// 服务定位器接口。用于模块间解耦（业务模块可通过 IServiceLocator 取得依赖，而不需要直接引用具体类）。
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// 注册服务实例
        /// </summary>
        void Register<T>(T service) where T : class;

        /// <summary>
        /// 注册服务工厂（延迟创建）
        /// </summary>
        void Register<T>(Func<T> factory) where T : class;

        /// <summary>
        /// 获取服务。未注册时返回 null
        /// </summary>
        T Resolve<T>() where T : class;

        /// <summary>
        /// 获取服务，未注册时抛异常
        /// </summary>
        T Require<T>() where T : class;

        /// <summary>
        /// 注销服务
        /// </summary>
        void Unregister<T>() where T : class;

        /// <summary>
        /// 清空所有注册
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 默认 IServiceLocator 实现。基于 Dictionary&lt;Type, object&gt;，线程安全。
    /// </summary>
    public class DefaultServiceLocator : IServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly object _lock = new object();

        public void Register<T>(T service) where T : class
        {
            lock (_lock)
            {
                _services[typeof(T)] = service;
            }
        }

        public void Register<T>(Func<T> factory) where T : class
        {
            lock (_lock)
            {
                _services[typeof(T)] = factory;
            }
        }

        public T Resolve<T>() where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var service))
                {
                    if (service is Func<T> factory) return factory();
                    return service as T;
                }
            }
            return null;
        }

        public T Require<T>() where T : class
        {
            var s = Resolve<T>();
            if (s == null) throw new InvalidOperationException($"服务未注册：{typeof(T).Name}");
            return s;
        }

        public void Unregister<T>() where T : class
        {
            lock (_lock)
            {
                _services.Remove(typeof(T));
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}