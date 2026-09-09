using System.Threading;
using Cysharp.Threading.Tasks;

namespace WManager
{
    /// <summary>
    /// 存档服务抽象。业务模块通过此接口访问存档功能。
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// 保存到本地文件（加密）
        /// </summary>
        void Save<T>(T data, string path);

        /// <summary>
        /// 从本地文件加载（加密）
        /// </summary>
        T Load<T>(string path) where T : class;

        /// <summary>
        /// 保存到 PlayerPrefs（加密）
        /// </summary>
        void SaveToPrefs<T>(T data, string key);

        /// <summary>
        /// 从 PlayerPrefs 加载（加密）
        /// </summary>
        T LoadFromPrefs<T>(string key) where T : class;

        /// <summary>
        /// 检查存档是否存在
        /// </summary>
        bool Exists(string path);

        /// <summary>
        /// 检查 PlayerPrefs 键是否存在
        /// </summary>
        bool ExistsInPrefs(string key);

        /// <summary>
        /// 删除存档
        /// </summary>
        void Delete(string path);

        /// <summary>
        /// 删除 PlayerPrefs 键
        /// </summary>
        void DeletePref(string key);

        /// <summary>
        /// 异步保存（大数据不卡主线程）
        /// </summary>
        UniTask SaveAsync<T>(T data, string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步加载
        /// </summary>
        UniTask<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : class;
    }
}