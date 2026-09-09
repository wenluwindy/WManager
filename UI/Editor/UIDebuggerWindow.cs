using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace WManager.UIEditor
{
    /// <summary>
    /// 运行时 UI 状态查看器：当前有哪些面板实例、各自什么状态、页面栈长什么样，
    /// 并提供关闭面板、清缓存、试一条 Toast 之类的常用操作。
    /// </summary>
    public class UIDebuggerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _toastText = "这是一条测试提示";
        private bool _showSettings = true;

        [MenuItem("Tools/WManager/UI/UI 调试器")]
        private static void Open()
        {
            var window = GetWindow<UIDebuggerWindow>("UI 调试器");
            window.minSize = new Vector2(560f, 320f);
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式后才能查看运行时的 UI 状态。", MessageType.Info);
                DrawSettingsShortcut();
                return;
            }

            if (!UIManager.HasInstance)
            {
                EditorGUILayout.HelpBox("UIManager 还没有创建。第一次调用 UI.OpenAsync 时会自动创建。", MessageType.Info);
                DrawSettingsShortcut();
                return;
            }

            var manager = UIManager.Instance;

            DrawToolbar(manager);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawPanels(manager);
            EditorGUILayout.Space(6f);
            DrawPageStack(manager);
            EditorGUILayout.Space(6f);
            DrawPlayground(manager);

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar(UIManager manager)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(
                $"面板实例 {manager.LivePanelCount}    页面栈 {manager.PageStackCount}",
                EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("关闭全部", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                manager.CloseAllAsync().Forget();

            if (GUILayout.Button("清理缓存", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                int released = manager.ReleaseAllCached();
                Debug.Log($"[UI 调试器] 释放了 {released} 个缓存面板。");
            }

            if (GUILayout.Button("打印状态", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                Debug.Log(manager.DumpState());

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPanels(UIManager manager)
        {
            EditorGUILayout.LabelField("面板实例", EditorStyles.boldLabel);

            var snapshot = manager.GetSnapshot();

            if (snapshot.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有任何面板实例。", MessageType.None);
                return;
            }

            foreach (var item in snapshot)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(StateIcon(item.StateText), GUILayout.Width(20f));
                GUILayout.Label(item.TypeName, EditorStyles.boldLabel, GUILayout.Width(180f));
                GUILayout.Label(item.StateText, GUILayout.Width(50f));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(item.Panel == null))
                {
                    if (GUILayout.Button("选中", GUILayout.Width(44f)))
                        Selection.activeGameObject = item.Panel.gameObject;

                    if (GUILayout.Button("关闭", GUILayout.Width(44f)))
                        manager.CloseAsync(item.Panel).Forget();
                }

                EditorGUILayout.EndHorizontal();

                string flags = $"层级 {item.Layer}    模式 {item.ShowMode}    缓存 {(item.Cache ? "是" : "否")}";

                if (item.Persistent)
                    flags += "    常驻";

                if (item.MultiInstance)
                    flags += "    多实例";

                if (item.StackIndex >= 0)
                    flags += $"    栈位置 {item.StackIndex}";

                EditorGUILayout.LabelField($"key  {item.Key}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(flags, EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawPageStack(UIManager manager)
        {
            EditorGUILayout.LabelField("页面栈（栈底在上）", EditorStyles.boldLabel);

            var stack = manager.PageStack;

            if (stack.Count == 0)
            {
                EditorGUILayout.HelpBox("页面栈是空的。用 UI.PushAsync 打开的页面才会入栈。", MessageType.None);
                return;
            }

            for (int i = 0; i < stack.Count; i++)
            {
                var panel = stack[i];
                bool isTop = i == stack.Count - 1;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(isTop ? "▶" : " ", GUILayout.Width(16f));
                GUILayout.Label($"[{i}] {(panel != null ? panel.GetType().Name : "null")}");
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("返回上一页"))
                manager.PopAsync().Forget();

            if (GUILayout.Button("返回栈底"))
                manager.PopToRootAsync().Forget();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlayground(UIManager manager)
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "快速测试", true);

            if (!_showSettings)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _toastText = EditorGUILayout.TextField("Toast 内容", _toastText);

            if (GUILayout.Button("飘一条", GUILayout.Width(70f)))
                manager.ShowToast(_toastText);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("显示 Loading"))
                manager.ShowLoading();

            if (GUILayout.Button("隐藏 Loading"))
                manager.HideLoading();

            if (GUILayout.Button("强制关 Loading"))
                manager.HideLoadingAll();

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("弹一个确认框"))
                manager.ShowMessageBoxAsync("提示", "这是调试器发出的测试消息框。", true).Forget();

            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsShortcut()
        {
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("定位 / 创建 UISettings 配置"))
                UISettingsLocator.Locate();
        }

        private static string StateIcon(string state)
        {
            switch (state)
            {
                case "打开": return "●";
                case "暂停": return "◐";
                case "过渡中": return "◌";
                case "缓存": return "○";
                default: return "×";
            }
        }
    }

    /// <summary>定位或创建 UISettings 资产</summary>
    internal static class UISettingsLocator
    {
        private const string DefaultFolder = "Assets/Resources";

        [MenuItem("Tools/WManager/UI/定位 UISettings 配置")]
        public static void Locate()
        {
            var guids = AssetDatabase.FindAssets("t:UISettings");

            if (guids.Length > 0)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UISettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "UISettings",
                    $"项目里还没有 UISettings 配置，框架当前使用内置默认值。\n\n是否在 {DefaultFolder} 下创建一个？",
                    "创建", "取消"))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(DefaultFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var settings = ScriptableObject.CreateInstance<UISettings>();
            string path = $"{DefaultFolder}/{UISettings.ResourcesPath}.asset";

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
