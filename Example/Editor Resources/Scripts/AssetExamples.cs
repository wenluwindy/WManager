// ----------------------------------------------------------------------------
// AssetExamples.cs
//
// 演示 AddressablesManager：
// - LoadAsync / Release（资源加载 + 引用计数）
// - InstantiateAsync / ReleaseInstance（实例化 + 释放）
// - 热更新流程（InitializeAsync → CheckUpdateSizeAsync → DownloadUpdateAsync）
// - 自定义 IAssetLoader 注入 UIManager
//
// =============================================================================
// 如何在场景中使用 AssetExample
// =============================================================================
//
// 1) 准备场景结构（Hierarchy）
//    在你的演示场景里按下面结构创建物体，然后把 AssetExample 挂到根节点：
//
//      DemoRoot  （空 GameObject，挂 AssetExample 组件）
//        └── FX_SpawnPoint  （空子物体，作为特效 Prefab 的父节点，可选）
//
//    说明：
//      - 如果场景里没有 SpriteRenderer，LoadSpriteDemoAsync 会跳过显示这一步，
//        但资源加载/释放逻辑仍然完整运行。
//      - InstantiatePrefabDemoAsync 默认 parent 是挂载该脚本的 transform
//        （即 DemoRoot 本身），所以即使没有 FX_SpawnPoint 也能跑。
//
// 2) Addressables 资源准备
//    在 Addressables Groups 窗口里准备好下面两个 key：
//      - Sprite Key   : "Icon/Bag"        （一张 Sprite 资源，例如背包图标）
//      - Prefab Key   : "FX/Hit"          （一个带 ParticleSystem 的 Prefab）
//    并确保它们所在 Group 的 Addressable Name 与上面完全一致。
//
// 3) Inspector 配置（选中 DemoRoot 看到的字段）
//      Sprite Key      : Icon/Bag          ← 可改为你自己的 Sprite key
//      Prefab Key      : FX/Hit            ← 可改为你自己的 Prefab key
//      Label           : default           ← 用于热更新演示的 Label 名
//
// 4) 运行场景
//    按下 Play，控制台会按顺序输出：
//      [Asset] Icon/Bag 已加载？True，引用计数 = 1
//      [Asset] FX/Hit 当前实例数 = 1
//      ... 3 秒后自动释放实例 ...
//      [Asset] 下载进度 xx.x%   （有热更新时才会出现）
//
// 5) 触发兜底 Loader（可选）
//    想看 Resources 兜底效果时，调用 DemoRoot 上的 UseResourcesFallbackLoader()
//    方法（例如绑定到一个 UI Button 的 OnClick）。
//    注意：必须在 UIManager.OpenAsync 之前调用。
//
// 6) 想在 Inspector 上手动一键释放所有缓存时
//    暴露 ReleaseEverything() 方法，可在 Debug 菜单里挂一个按钮调用。
//
// =============================================================================
// 典型工作流回顾（结合上面的注释一起看）
// =============================================================================
//   Start()
//     ├─ InitializeAsync()                       // 初始化 Addressables（幂等）
//     ├─ LoadSpriteDemoAsync()                   // LoadAsync + Release
//     ├─ InstantiatePrefabDemoAsync()            // InstantiateAsync + 3s 后 ReleaseInstance
//     └─ HotUpdateDemoAsync()                    // CheckUpdateSizeAsync → DownloadUpdateAsync
//   OnDestroy()
//     └─ ReleaseInstance(_spawnedFx)             // 场景销毁兜底释放
// ----------------------------------------------------------------------------
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WManager;

namespace WManager.Example
{
    public class AssetExample : MonoBehaviour
    {
        [Header("Addressables Key 配置")]
        [Tooltip("Sprite 资源 key，例如：Icon/Bag")]
        [SerializeField] private string spriteKey = "Icon/Bag";
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Prefab 资源 key，例如：FX/Hit")]
        [SerializeField] private string prefabKey = "FX/Hit";

        [Tooltip("用于热更新演示的 Label 名")]
        [SerializeField] private string label = "default";

        [Header("可选：演示用子节点")]
        [Tooltip("把特效 Prefab 实例化到这个 Transform 下面，留空则挂到本节点")]
        [SerializeField] private Transform fxSpawnPoint;

        // 演示用：缓存实例和资源，便于管理生命周期
        private GameObject _spawnedFx;
        private Sprite _bagSprite;

        private async void Start()
        {
            // 1) Addressables 初始化（幂等）
            if (!await AddressablesManager.Instance.InitializeAsync())
            {
                Debug.LogError("Addressables 初始化失败");
                return;
            }

            await LoadSpriteDemoAsync();
            await InstantiatePrefabDemoAsync();
            await HotUpdateDemoAsync();
        }

        // ============================================================================
        // 加载 Sprite 等普通资源：LoadAsync 返回的是缓存句柄，Release 才真释放
        // ============================================================================
        private async UniTask LoadSpriteDemoAsync()
        {
            // 第一次加载：内部 +1 引用计数
            _bagSprite = await AddressablesManager.Instance.LoadAsync<Sprite>(spriteKey);
            if (_bagSprite == null)
            {
                Debug.LogError($"Sprite 加载失败：{spriteKey}");
                return;
            }

            // 用 SpriteRenderer / Image 显示加载的 Sprite
            if (spriteRenderer != null) spriteRenderer.sprite = _bagSprite;

            // 检查缓存状态
            Debug.Log($"[Asset] {spriteKey} 已加载？{AddressablesManager.Instance.IsLoaded(spriteKey)}，" +
                      $"引用计数 = {AddressablesManager.Instance.GetAssetRefCount(spriteKey)}");

            // 用完释放（引用计数 -1，归零才真正释放）
            AddressablesManager.Instance.Release(spriteKey);
            _bagSprite = null;
        }

        // ============================================================================
        // 实例化 Prefab：必须用 ReleaseInstance，不能 Destroy
        // ============================================================================
        private async UniTask InstantiatePrefabDemoAsync()
        {
            // 选择父节点：优先用 Inspector 指定的 fxSpawnPoint，否则挂到本节点
            Transform parent = fxSpawnPoint != null ? fxSpawnPoint : transform;

            // 同一 key 重复 InstantiateAsync 会创建多个实例
            _spawnedFx = await AddressablesManager.Instance.InstantiateAsync(
                key: prefabKey,
                parent: parent,
                worldPositionStays: false);

            if (_spawnedFx == null)
            {
                Debug.LogError($"Prefab 实例化失败：{prefabKey}");
                return;
            }

            _spawnedFx.transform.localPosition = Vector3.zero;
            _spawnedFx.transform.localScale = Vector3.one;

            // 当前这个 key 已经有多少实例
            Debug.Log($"[Asset] {prefabKey} 当前实例数 = " +
                      $"{AddressablesManager.Instance.GetInstanceCount(prefabKey)}");

            // 用完必须 ReleaseInstance，不能 Destroy！
            // 5 秒后自动释放演示
            await UniTask.Delay(5_000);

            if (_spawnedFx != null)
            {
                AddressablesManager.Instance.ReleaseInstance(_spawnedFx);
                _spawnedFx = null;
            }
        }

        // ============================================================================
        // 热更新全流程
        // ============================================================================
        private async UniTask HotUpdateDemoAsync()
        {
            // 1) 检查更新大小（内部已经包含 Initialize + UpdateCatalogs）
            long size = await AddressablesManager.Instance.CheckUpdateSizeAsync(label);

            if (size <= 0)
            {
                Debug.Log("[Asset] 无需更新");
                return;
            }

            Debug.Log($"[Asset] 需要下载 {size / 1024f / 1024f:F2} MB");

            // 2) 下载并接收进度
            bool ok = await AddressablesManager.Instance.DownloadUpdateAsync(
                label,
                (downloaded, total) =>
                {
                    float p = total > 0 ? (float)downloaded / total : 0f;
                    Debug.Log($"[Asset] 下载进度 {p * 100f:F1}%");
                });

            if (ok) Debug.Log("[Asset] 下载完成，可以继续加载新资源");
        }

        // ============================================================================
        // 一次性清理（场景销毁）
        // ============================================================================
        private void OnDestroy()
        {
            // 释放当前物体还持有的资源引用
            if (_spawnedFx != null)
            {
                AddressablesManager.Instance.ReleaseInstance(_spawnedFx);
                _spawnedFx = null;
            }
        }

        // ============================================================================
        // 把当前所有缓存全部释放（慎用：会清空所有正在用的资源/实例）
        // ============================================================================
        public void ReleaseEverything()
        {
            AddressablesManager.Instance.ReleaseAllInstances();
            AddressablesManager.Instance.ReleaseAllAssets();
            AddressablesManager.Instance.PrintDebugInfo();
        }

        // ============================================================================
        // 自定义 IAssetLoader：把 UIManager 的资源加载器换成 Resource 版
        // （比如联机不到 Addressables Catalog 时的兜底）
        // ============================================================================
        public void UseResourcesFallbackLoader()
        {
            // UIManager 没有公开的 Create()。第一次访问 .Instance 会触发 Awake → UIRoot.Create()，
            // 然后立刻注入自定义加载器。SetAssetLoader 必须在第一次 OpenAsync/PreloadAsync 之前。
            _ = UIManager.Instance;
            UIManager.Instance.SetAssetLoader(new ResourcesAssetLoaderExample());
            // 之后所有 UIManager.Instance.OpenAsync<T>("UI/xxx") 都会从 Resources/UI/xxx 加载 Prefab
        }

        // ============================================================================
        // 场景使用小工具：演示外部如何触发加载/实例化
        // ============================================================================
        /// <summary>
        /// 手动加载一次 Sprite（演示用）：可绑定到 UI Button OnClick
        /// </summary>
        public async void ManualLoadSprite()
        {
            var sprite = await AddressablesManager.Instance.LoadAsync<Sprite>(spriteKey);
            if (sprite == null)
            {
                Debug.LogError($"[Asset] 手动加载 Sprite 失败：{spriteKey}");
                return;
            }

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;

            Debug.Log($"[Asset] 手动加载 {spriteKey} 成功，引用计数 = " +
                      $"{AddressablesManager.Instance.GetAssetRefCount(spriteKey)}");
        }

        /// <summary>
        /// 手动实例化一次 Prefab（演示用）：可绑定到 UI Button OnClick
        /// </summary>
        public async void ManualSpawnPrefab()
        {
            Transform parent = fxSpawnPoint != null ? fxSpawnPoint : transform;

            var go = await AddressablesManager.Instance.InstantiateAsync(
                key: prefabKey,
                parent: parent,
                worldPositionStays: false);

            if (go == null)
            {
                Debug.LogError($"[Asset] 手动实例化 Prefab 失败：{prefabKey}");
                return;
            }

            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            // 5 秒后自动释放
            await UniTask.Delay(5_000);
            if (go != null) AddressablesManager.Instance.ReleaseInstance(go);
        }
    }

    /// <summary>
    /// 一个最简的 IAssetLoader 实现：从 Resources/ 加载 Prefab。
    /// 实际接入自己项目的 AssetBundle / YooAssets 时仿照写一个就行。
    /// </summary>
    public class ResourcesAssetLoaderExample : IAssetLoader
    {
        public async UniTask<GameObject> InstantiateAsync(
            string key,
            Transform parent = null,
            bool worldPositionStays = false,
            CancellationToken cancellationToken = default)
        {
            // Resources.Load 是同步的；包一层 await 以满足 async 接口
            await UniTask.Yield(cancellationToken);

            var prefab = Resources.Load<GameObject>(key);
            if (prefab == null) return null;

            return Object.Instantiate(prefab, parent, worldPositionStays);
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance != null) Object.Destroy(instance);
        }

        public async UniTask<bool> PreloadAsync<T>(string key) where T : class
        {
            await UniTask.Yield();
            return Resources.Load<UnityEngine.Object>(key) != null;
        }
    }
}