using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WManager
{
    public sealed class SceneLoader : MonoBehaviour
    {
        #region Private Variables
        //正在激活场景
        private bool activatingScene;
        //场景已经加载准备完毕
        private bool sceneLoadedAndReady;
        //场景加载准备完成的时间
        private float sceneLoadedAndReadyTime;
        //允许场景激活
        private bool allowSceneActivation = true;
        //场景激活延迟
        private float sceneActivationDelay = 0.2f;
        //当前的异步工作
        private AsyncOperation currentAsyncOperation;
        private Action onBegin;
        private Action onComplete;
        private Action<float> onDelay;
        private Action<float> onLoading;
        #endregion

        #region Public Properties
        /// <summary>
        /// 加载进度
        /// </summary>
        public float Progress { get; private set; }
        #endregion

        #region Private Methods
        private void Start()
        {
            onBegin?.Invoke();
        }
        private void Update()
        {
            if (null == currentAsyncOperation) return;
            Progress = Mathf.Clamp01(currentAsyncOperation.progress / 0.9f);
            if (!sceneLoadedAndReady)
            {
                onLoading?.Invoke(Progress);
                if (DebugMode)
                {
                    LogInfo($"场景加载进度: {Mathf.Round(Progress * 100)}%");
                }
            }
            if (!sceneLoadedAndReady && currentAsyncOperation.progress == 0.9f)
            {
                sceneLoadedAndReady = true;
                sceneLoadedAndReadyTime = Time.realtimeSinceStartup;
                if (DebugMode)
                {
                    LogInfo("场景准备激活.");
                }
            }
            if (sceneLoadedAndReady && !activatingScene && allowSceneActivation)
            {
                float elapsedTime = Time.realtimeSinceStartup - sceneLoadedAndReadyTime;
                elapsedTime = Mathf.Clamp(elapsedTime, 0f, sceneActivationDelay);
                onDelay?.Invoke(elapsedTime);
                if (elapsedTime == sceneActivationDelay)
                {
                    if (currentAsyncOperation != null)
                    {
                        currentAsyncOperation.allowSceneActivation = true;
                    }
                    activatingScene = true;
                }
            }
            if (currentAsyncOperation.isDone)
            {
                if (DebugMode)
                {
                    LogInfo("场景加载完成.");
                }
                currentAsyncOperation = null;
                onComplete?.Invoke();
                Destroy(gameObject);
            }
        }
        #endregion

        #region Pubclic Mehtods
        /// <summary>
        /// 设置是否允许场景激活
        /// </summary>
        /// <param name="allowSceneActivation">是否允许场景激活</param>
        public SceneLoader SetAllowSceneActivation(bool allowSceneActivation)
        {
            this.allowSceneActivation = allowSceneActivation;
            return this;
        }
        /// <summary>
        /// 设置场景激活延迟
        /// </summary>
        /// <param name="sceneActivationDelay">场景激活延迟</param>
        public SceneLoader SetSceneActivationDelay(float sceneActivationDelay)
        {
            if (sceneActivationDelay < 0f) sceneActivationDelay = 0f;
            this.sceneActivationDelay = sceneActivationDelay;
            return this;
        }
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="buildIndex">场景的BuildIndex</param>
        /// <param name="mode">加载方式</param>
        private SceneLoader Load(int buildIndex, LoadSceneMode mode)
        {
            currentAsyncOperation = SceneManager.LoadSceneAsync(buildIndex, mode);
            currentAsyncOperation.allowSceneActivation = false;
            sceneLoadedAndReady = false;
            activatingScene = false;
            return this;
        }
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景的名称</param>
        /// <param name="mode">加载方式</param>
        private SceneLoader Load(string sceneName, LoadSceneMode mode)
        {
            currentAsyncOperation = SceneManager.LoadSceneAsync(sceneName, mode);
            currentAsyncOperation.allowSceneActivation = false;
            sceneLoadedAndReady = false;
            activatingScene = false;
            return this;
        }
        /// <summary>
        /// 设置加载开始事件
        /// </summary>
        /// <param name="onBegin">加载开始事件</param>
        public SceneLoader OnBegin(Action onBegin)
        {
            this.onBegin = onBegin;
            return this;
        }
        /// <summary>
        /// 设置加载完成事件
        /// </summary>
        /// <param name="onComplete">加载完成事件</param>
        public SceneLoader OnCompleted(Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }
        /// <summary>
        /// 设置激活延迟事件
        /// </summary>
        /// <param name="onDelay">激活延迟事件</param>
        public SceneLoader OnDelay(Action<float> onDelay)
        {
            this.onDelay = onDelay;
            return this;
        }
        /// <summary>
        /// 设置加载中事件
        /// </summary>
        /// <param name="onLoading">加载事件</param>
        public SceneLoader OnLoading(Action<float> onLoading)
        {
            this.onLoading = onLoading;
            return this;
        }
        #endregion
        /// <summary>
        /// 异步场景加载
        /// </summary>
        /// <param name="buildIndex">场景序号</param>
        /// <param name="loadSceneMode">场景加载模式</param>
        /// <returns></returns>
        public static SceneLoader LoadSceneAsync(int buildIndex, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (DebugMode)
            {
                LogInfo($"异步加载场景: 场景指针 - {buildIndex} 加载模式 - {loadSceneMode}");
            }
            var loader = new GameObject(typeof(SceneLoader).Name).AddComponent<SceneLoader>().Load(buildIndex, loadSceneMode);
            DontDestroyOnLoad(loader);
            return loader;
        }
        /// <summary>
        /// 异步场景加载
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="loadSceneMode">场景加载模式</param>
        /// <returns></returns>
        public static SceneLoader LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (DebugMode)
            {
                LogInfo($"异步加载场景: 场景名称 - {sceneName} 加载模式 - {loadSceneMode}");
            }
            var loader = new GameObject(typeof(SceneLoader).Name).AddComponent<SceneLoader>().Load(sceneName, loadSceneMode);
            DontDestroyOnLoad(loader);
            return loader;
        }
        public static AsyncOperation UnloadSceneAsync(Scene scene)
        {
            if (DebugMode)
            {
                LogInfo($"异步卸载场景: 场景 - {scene}");
            }
            return SceneManager.UnloadSceneAsync(scene);
        }
        public static AsyncOperation UnloadSceneAsync(int buildIndex)
        {
            if (DebugMode)
            {
                LogInfo($"异步卸载场景: 场景指针- {buildIndex}");
            }
            return SceneManager.UnloadSceneAsync(buildIndex);
        }
        public static AsyncOperation UnloadSceneAsync(string sceneName)
        {
            if (DebugMode)
            {
                LogInfo($"异步卸载场景: 场景名称- {sceneName}");
            }
            return SceneManager.UnloadSceneAsync(sceneName);
        }
        public static bool DebugMode = true;
        private static void LogInfo(string info)
        {
            Debug.Log($"<color=cyan><b>场景加载器信息 >></b></color> {info}");
        }
    }
}