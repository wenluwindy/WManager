using System.IO;

using UnityEngine;
using UnityEditor;

namespace AssetBundleBrowser
{
    public class AssetBundleEditorWindow : EditorWindow
    {
        [MenuItem("插件工具/AssetBundle打包窗口")]
        public static void Open()
        {
            var window = GetWindow<AssetBundleEditorWindow>("AssetBundle打包器");
            window.titleContent = new GUIContent("AssetBundle打包器");
            window.minSize = new Vector2(300f, 100f);
            window.maxSize = new Vector2(1080f, 100f);
            window.Show();
        }
        //打包路径
        private string path;
        //打包选项
        private BuildAssetBundleOptions options;
        //目标平台
        private BuildTarget target;

        private const float labelWidth = 100f;

        private void OnEnable()
        {
            path = EditorPrefs.HasKey(EditorPrefsKeys.path)
                ? EditorPrefs.GetString(EditorPrefsKeys.path)
                : Application.streamingAssetsPath;

            options = EditorPrefs.HasKey(EditorPrefsKeys.options)
                ? (BuildAssetBundleOptions)EditorPrefs.GetInt(EditorPrefsKeys.options)
                : BuildAssetBundleOptions.None;

            target = EditorPrefs.HasKey(EditorPrefsKeys.target)
                ? (BuildTarget)EditorPrefs.GetInt(EditorPrefsKeys.target)
                : BuildTarget.StandaloneWindows;
        }

        private void OnGUI()
        {
            //路径
            GUILayout.BeginHorizontal();
            GUILayout.Label("输出路径", GUILayout.Width(labelWidth));
            string newPath = EditorGUILayout.TextField(path);
            if (newPath != path)
            {
                path = newPath;
                EditorPrefs.SetString(EditorPrefsKeys.path, path);
            }
            //浏览 选择路径
            if (GUILayout.Button("浏览", GUILayout.Width(60f)))
            {
                newPath = EditorUtility.OpenFolderPanel("AssetsBundle构建路径", Application.dataPath, string.Empty);
                if (!string.IsNullOrEmpty(newPath) && newPath != path)
                {
                    path = newPath;
                    EditorPrefs.SetString(EditorPrefsKeys.path, path);
                }
            }
            GUILayout.EndHorizontal();

            //选项
            GUILayout.BeginHorizontal();
            GUILayout.Label("打包选项", GUILayout.Width(labelWidth));
            var newOptions = (BuildAssetBundleOptions)EditorGUILayout.EnumPopup(options);
            if (newOptions != options)
            {
                options = newOptions;
                EditorPrefs.SetInt(EditorPrefsKeys.options, (int)options);
            }
            GUILayout.EndHorizontal();

            //平台
            GUILayout.BeginHorizontal();
            GUILayout.Label("打包平台", GUILayout.Width(labelWidth));
            var newTarget = (BuildTarget)EditorGUILayout.EnumPopup(target);
            if (newTarget != target)
            {
                target = newTarget;
                EditorPrefs.SetInt(EditorPrefsKeys.target, (int)target);
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //构建按钮
            if (GUILayout.Button("开始打包"))
            {
                //检查路径是否有效
                if (!Directory.Exists(path))
                {
                    Debug.LogError(string.Format("无效路径 {0}", path));
                    return;
                }
                //提醒
                if (EditorUtility.DisplayDialog("提醒", "构建AssetsBundle将花费一定时间，是否确定开始？", "确定", "取消"))
                {
                    //开始构建
                    BuildPipeline.BuildAssetBundles(path, options, target);
                }
            }
        }

        private class EditorPrefsKeys
        {
            public static string path = Application.productName + "SIMPLEASSETSBUNDLE_PATH";
            public static string options = Application.productName + "SIMPLEASSETSBUNDLE_OPTIONS";
            public static string target = Application.productName + "SIMPLEASSETSBUNDLE_TARGET";
        }
    }
}