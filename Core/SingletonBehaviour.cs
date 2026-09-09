using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 泛型懒汉单例 MonoBehaviour 基类。
    /// 特性：
    /// - 线程安全的双检锁创建
    /// - 自动创建隐藏 GameObject（命名 [TypeName]）并 DontDestroyOnLoad
    /// - 场景里预放的实例优先，会销毁后创建的新实例
    /// - 应用退出时停止自动创建（避免退出流程里的 OnDestroy 触发的创建）
    /// </summary>
    /// <typeparam name="T">子类类型，必须是 MonoBehaviour 子类</typeparam>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _quitting;

        /// <summary>
        /// 获取单例实例。第一次访问时自动创建。
        /// 应用退出过程中返回 null（防止 OnDestroy 触发的死循环创建）。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_quitting) return null;
                if (_instance != null) return _instance;
                lock (_lock)
                {
                    if (_instance != null) return _instance;
                    var go = new GameObject($"[{typeof(T).Name}]");
                    _instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                    return _instance;
                }
            }
        }

        /// <summary>
        /// 单例是否已存在
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // 场景里已有一个，销毁新创建的这个
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit()
        {
            _quitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}