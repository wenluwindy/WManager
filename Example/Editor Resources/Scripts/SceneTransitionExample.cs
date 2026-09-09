// ----------------------------------------------------------------------------
// SceneTransitionExample.cs
//
// 演示场景切换的标准流程 + WManager 清理顺序。
// ----------------------------------------------------------------------------
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using WManager;

namespace WManager.Example
{
    public class SceneTransitionExample : MonoBehaviour
    {
        // 把这些场景名换成工程里的实际场景
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private string mainSceneName = "MainScene";

        /// <summary>
        /// 切换场景的统一入口（带 UI/计时器/资源/音频/事件的清理）。
        /// </summary>
        public async UniTask GoToSceneAsync(string sceneName)
        {
            // 1. 关掉所有 UI（页面内 OnDidClose 自动解绑事件），再把缓存实例也释放掉
            if (UI.IsAlive)
            {
                await UI.CloseAllAsync();
                UI.ReleaseAllCached();
            }

            // 2. 停止计时器
            if (TimerScheduler.HasInstance)
                TimerScheduler.Instance.StopAllAndRemove();

            // 3. 释放 Addressables 实例和缓存资源
            if (AddressablesManager.HasInstance)
                AddressablesManager.Instance.ReleaseAll();

            // 4. 淡出停止所有音效
            if (SoundManager.HasInstance)
                SoundManager.StopAll();

            // 5. 清空事件总线
            EventManager.StopAll();
            EventManager.DisposeAll();

            // 6. 切场景（带取消令牌）
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single)
                .ToUniTask();

            // 7. 触发一次 GC（可选）
            await Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // ============================================================================
        // 场景中的业务入口：进入场景后做初始化
        // ============================================================================
        public class InGameEntry : MonoBehaviour
        {
            private async void Start()
            {
                // key / 层级 / 缓存策略都在 InGameHUDExample 的 [UIPanelInfo] 上
                await UI.OpenAsync<InGameHUDExample>();
            }
        }

        [UIPanelInfo(Key = "UI/InGameHUD", Layer = UILayer.Bottom, Cache = true, Persistent = true)]
        public class InGameHUDExample : UIPanel
        {
            protected override void OnCreate()
            {
                // 绑定返回主菜单按钮
            }

            protected override void OnDidClose()
            {
                // 订阅的事件在这里解绑
            }
        }

        // ============================================================================
        // 切回主菜单按钮
        // ============================================================================
        public async void BackToMain()
        {
            await GoToSceneAsync(mainSceneName);
        }

        public async void EnterGame()
        {
            await GoToSceneAsync(gameSceneName);
        }
    }
}
