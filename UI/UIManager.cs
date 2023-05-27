using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using WManager;
using UnityEngine.Events;

namespace WManager.UI
{
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;

        private static Dictionary<string, UIView> viewDic = new Dictionary<string, UIView>();

        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (UIManager)FindObjectOfType(typeof(UIManager));
                }
                return instance;
            }
        }

        public static Canvas Canvas
        {
            get
            {
                return Instance.GetComponentInChildren<Canvas>();
            }
        }
        public static Camera Camera
        {
            get
            {
                return Instance.GetComponentInChildren<Camera>();
            }
        }
        public static Vector2 Resolution
        {
            get
            {
                return Instance.GetComponentInChildren<CanvasScaler>().referenceResolution;
            }
        }
        private void Awake()
        {
            string[] levelNames = Enum.GetNames(typeof(ViewLevel));
            for (int i = levelNames.Length - 1; i >= 0; i--)
            {
                string levelName = levelNames[i];
                var levelInstance = new GameObject(levelName);
                levelInstance.layer = LayerMask.NameToLayer("UI");
                levelInstance.transform.SetParent(Instance.transform, false);
                RectTransform rectTransform = levelInstance.AddComponent<RectTransform>();
                rectTransform.sizeDelta = Resolution;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
                rectTransform.SetAsFirstSibling();
            }
            DontDestroyOnLoad(Instance);
        }

        #region >> Resources模式加载视图
        /// <summary>
        /// 加载视图
        /// </summary>
        /// <param name="viewName">视图命名</param>
        /// <param name="viewResourcePath">视图资源路径</param>
        /// <param name="level">视图层级</param>
        /// <param name="data">视图数据</param>
        /// <param name="instant">是否立即显示</param>
        /// <returns>加载成功返回视图，否则返回null</returns>
        public static T LoadViewResource<T>(string viewName, string viewResourcePath, ViewLevel level = ViewLevel.Default, IViewData data = null, bool instant = false) where T : UIView
        {
            if (!viewDic.TryGetValue(viewName, out UIView view))
            {
                GameObject viewPrefab = Resources.Load<GameObject>(viewResourcePath);
                if (viewPrefab == null)
                {
                    Debug.LogError("LoadViewResource失败,未找到资源:" + viewResourcePath);
                    return null;
                }
                else
                {
                    GameObject inst = Instantiate(viewPrefab);
                    inst.transform.SetParent(Instance.transform.GetChild((int)level), false);
                    inst.name = viewName;

                    view = inst.GetComponent<T>();
                    if (view == null)
                    {
                        Debug.LogError("预制体" + viewPrefab.name + "缺少UIView组件");
                        return null;
                    }

                    view.Name = viewName;
                    view.Init(data, instant);

                    viewDic.Add(viewName, view);
                    return view as T;
                }
            }
            else
            {
                return view as T;
            }
        }
        /// <summary>
        /// 加载视图，不显示
        /// </summary>
        /// <param name="viewName">视图名</param>
        /// <param name="level">视图层级</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>UIView</returns>
        public static T LoadViewResourceDotShow<T>(string viewName, ViewLevel level) where T : UIView
        {
            if (!viewDic.TryGetValue(viewName, out UIView view))
            {
                GameObject viewPrefab = Resources.Load<GameObject>(viewName);
                if (viewPrefab == null)
                {
                    Debug.LogError("LoadViewResource失败,未找到资源:" + viewName);
                    return null;
                }
                else
                {
                    GameObject inst = Instantiate(viewPrefab);
                    inst.transform.SetParent(Instance.transform.GetChild((int)level), false);
                    inst.name = viewName;

                    view = inst.GetComponent<T>();
                    if (view == null)
                    {
                        Debug.LogError("预制体" + viewPrefab.name + "缺少UIView组件");
                        return null;
                    }

                    view.Name = viewName;
                    //可交互性置为false
                    view.CanvasGroup.alpha = 0;
                    view.CanvasGroup.interactable = false;
                    view.CanvasGroup.blocksRaycasts = false;

                    viewDic.Add(viewName, view);
                    return view as T;
                }
            }
            else
            {
                return view as T;
            }
        }
        /// <summary>
        /// 加载视图
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <param name="level">视图层级</param>
        /// <param name="data">视图数据</param>
        /// <param name="instant">是否立即显示</param>
        /// <returns>加载成功返回视图，否则返回null</returns>
        public static T LoadViewResource<T>(ViewLevel level = ViewLevel.Default, IViewData data = null, bool instant = false) where T : UIView
        {
            string viewName = typeof(T).Name;
            T view = LoadViewResource<T>(viewName, viewName, level, data, instant);
            if (view != null)
            {
                return view;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region >> AssetBundle模式加载视图
        /// <summary>
        /// 异步加载AssetBundle中的视图
        /// </summary>
        /// <param name="ABName">AB包名称，在StreamingAssets下</param>
        /// <param name="viewName">视图名</param>
        /// <param name="level">视图层级</param>
        /// <param name="data">视图数据</param>
        /// <param name="instant">是否立即显示</param>
        /// <param name="onCompleted">加载完成事件</param>
        /// <typeparam name="T">视图类型</typeparam>
        public static void LoadViewAB<T>(string ABName, string viewName, Action<float> onLoading, ViewLevel level = ViewLevel.Default, IViewData data = null, bool instant = false, Action<bool, T> onCompleted = null) where T : UIView
        {
            if (!viewDic.ContainsKey(viewName))
            {
                //回传的加载进度值
                AssetBundleLoader.onLoading += (progress) =>
                {
                    onLoading?.Invoke(progress);
                };

                AssetBundleLoader.LoadObjFromWeb(ABName, viewName, (obj, f) =>
                {
                    GameObject inst = Instantiate(obj);
                    inst.transform.SetParent(Instance.transform.GetChild((int)level), false);
                    inst.name = viewName;

                    T view = inst.GetComponent<T>();
                    if (view == null)
                    {
                        Debug.LogError("预制体" + view.name + "缺少UIView组件");
                        onCompleted?.Invoke(false, null);
                        return;
                    }
                    view.Name = viewName;
                    view.Init(data, instant);
                    viewDic.Add(viewName, view);

                    onCompleted?.Invoke(true, view);
                });
            }
            else
            {
                onCompleted?.Invoke(false, viewDic[viewName] as T);
            }
        }
        /// <summary>
        /// 异步加载AssetBundle中的视图
        /// </summary>
        /// <param name="ABName">AB包名称，在StreamingAssets下</param>
        /// <param name="viewName">视图名</param>
        /// <param name="level">视图层级</param>
        /// <param name="onCompleted">加载完成事件</param>
        /// <typeparam name="T">视图类型</typeparam>
        public static void LoadViewAB<T>(string ABName, string viewName, ViewLevel level, Action<float> onLoading, Action<bool, T> onCompleted) where T : UIView
        {
            LoadViewAB(ABName, viewName, onLoading, level, null, false, onCompleted);
        }
        /// <summary>
        /// 异步加载AssetBundle中的视图
        /// </summary>
        /// <param name="ABName">AB包名称，在StreamingAssets下</param>
        /// <param name="viewName">视图名</param>
        /// <param name="onCompleted">加载完成事件</param>
        /// <typeparam name="T">视图类型</typeparam>
        public static void LoadViewAB<T>(string ABName, string viewName, Action<float> onLoading, Action<bool, T> onCompleted) where T : UIView
        {
            LoadViewAB(ABName, viewName, onLoading, ViewLevel.Default, null, false, onCompleted);
        }

        #endregion

        /// <summary>
        /// 显示视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="data">视图数据</param>
        /// <param name="instant">是否立即显示</param>
        /// <returns>视图</returns>
        public static T ShowView<T>(string viewName, ViewLevel level = ViewLevel.Default, IViewData data = null, bool instant = false) where T : UIView
        {
            if (viewDic.TryGetValue(viewName, out UIView view))
            {
                view.Show(data, instant);
                return view as T;
            }
            else
            {
                T newView = LoadViewResource<T>(viewName, viewName, level);
                if (newView != null)
                {
                    newView.Show(data, instant);
                    return view as T;
                }
                else
                {
                    UnloadView<T>();
                    return null;
                }
            }
        }
        /// <summary>
        /// 显示视图
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <param name="data">视图数据</param>
        /// <param name="instant">是否立即显示</param>
        /// <returns>视图</returns>
        public static T ShowView<T>(IViewData data = null, bool instant = false) where T : UIView
        {
            var view = ShowView<T>(typeof(T).Name, ViewLevel.Default, data, instant);
            return view != null ? view as T : null;
        }
        /// <summary>
        /// 隐藏视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="instant">是否立即隐藏</param>
        /// <returns>视图</returns>
        public static IUIView HideView(string viewName, bool instant = false)
        {
            if (viewDic.TryGetValue(viewName, out UIView view))
            {
                view.Hide(instant);
                return view;
            }
            return null;
        }
        /// <summary>
        /// 隐藏视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="instant">是否立即隐藏</param>
        /// <returns>视图</returns>
        public static T HideView<T>(string viewName, bool instant = false) where T : UIView
        {
            if (viewDic.TryGetValue(viewName, out UIView view))
            {
                view.Hide(instant);
                return view as T;
            }
            return default(T);
        }
        /// <summary>
        /// 隐藏视图
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <param name="instant">是否立即隐藏</param>
        /// <returns>视图</returns>
        public static T HideView<T>(bool instant = false) where T : UIView
        {
            var thisView = GetView<T>().gameObject;
            if (thisView.activeSelf)
            {
                IUIView view = HideView(typeof(T).Name, instant);
                return view != null ? view as T : null;
            }
            Debug.Log("[UIManager]该视图已影藏:" + thisView.name);
            return null;
        }

        /// <summary>
        /// 卸载视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="instant">是否立即卸载</param>
        /// <returns>成功卸载返回true 否则返回false</returns>
        public static bool UnloadView(string viewName, bool instant = false)
        {
            if (viewDic.TryGetValue(viewName, out UIView view))
            {
                view.Unload(instant);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 卸载视图
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <param name="instant">是否立即卸载</param>
        /// <returns>成功卸载返回true 否则返回false</returns>
        public static bool UnloadView<T>(bool instant = false) where T : UIView
        {
            return UnloadView(typeof(T).Name, instant);
        }
        /// <summary>
        /// 卸载所有视图
        /// </summary>
        public static void UnloadAll()
        {
            List<IUIView> views = new List<IUIView>();
            foreach (var kv in viewDic)
            {
                views.Add(kv.Value);
            }
            for (int i = 0; i < views.Count; i++)
            {
                views[i].Unload(true);
                views.RemoveAt(i);
                i--;
            }
            viewDic.Clear();
        }

        /// <summary>
        /// 获取视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <returns>视图</returns>
        public static UIView GetView(string viewName)
        {
            if (viewDic.TryGetValue(viewName, out UIView view))
            {
                return view;
            }
            return null;
        }
        /// <summary>
        /// 获取视图 
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <returns>视图</returns>
        public static T GetView<T>() where T : UIView
        {
            IUIView view = GetView(typeof(T).Name);
            return view != null ? view as T : null;
        }
        /// <summary>
        /// 获取或加载视图
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <returns>视图</returns>
        public static T GetOrLoadView<T>() where T : UIView
        {
            T view = GetView<T>() ?? LoadViewResource<T>();
            return view;
        }

        /// <summary>
        /// 从字典中移除
        /// </summary>
        /// <param name="viewName">视图名称</param>
        public static void Remove(string viewName)
        {
            if (viewDic.ContainsKey(viewName))
            {
                viewDic.Remove(viewName);
            }
        }
    }
}