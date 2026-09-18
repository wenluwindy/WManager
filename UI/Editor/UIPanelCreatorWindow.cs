using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;
using WManager;

namespace WManager.UIEditor
{
    /// <summary>
    /// 一键新建面板：生成脚本 → 等编译完成 → 生成预制体 → 登记 Addressables。
    /// 把新建一个界面从"复制粘贴老面板再改名"变成填个表。
    /// </summary>
    public class UIPanelCreatorWindow : EditorWindow
    {
        private const string PendingKey = "WManager.UI.PendingPanelCreation";

        private string _panelName = "ShopPanel";
        private string _namespace = "";
        private string _scriptFolder = "Assets/Scripts/UI/Panels";
        private string _prefabFolder = "Assets/Scripts/UI/Prefab";

        private UILayer _layer = UILayer.Normal;
        private UIShowMode _showMode = UIShowMode.Normal;
        private bool _cache = true;
        private BaseKind _baseKind = BaseKind.Plain;

        private bool _createPrefab = true;
        private bool _registerAddressable = true;
        private string _addressableGroup = "";

        private enum BaseKind
        {
            [InspectorName("UIPanel（普通面板）")] Plain,
            [InspectorName("UIPanel<TArgs>（强类型参数）")] Typed,
            [InspectorName("UIAdaptivePanel（横竖屏双布局）")] Adaptive
        }

        [MenuItem("Tools/WManager/UI/新建 UI 面板")]
        private static void Open()
        {
            var window = GetWindow<UIPanelCreatorWindow>("新建 UI 面板");
            window.minSize = new Vector2(520f, 460f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            _panelName = EditorGUILayout.TextField("面板类名", _panelName);
            _namespace = EditorGUILayout.TextField("命名空间（可留空）", _namespace);
            _baseKind = (BaseKind)EditorGUILayout.EnumPopup("基类", _baseKind);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("面板属性（写进 [UIPanelInfo]）", EditorStyles.boldLabel);
            _layer = (UILayer)EditorGUILayout.EnumPopup("层级", _layer);
            _showMode = (UIShowMode)EditorGUILayout.EnumPopup("显示模式", _showMode);
            _cache = EditorGUILayout.Toggle("关闭后保留实例", _cache);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("输出位置", EditorStyles.boldLabel);
            _scriptFolder = DrawFolderField("脚本目录", _scriptFolder);

            _createPrefab = EditorGUILayout.Toggle("同时生成预制体", _createPrefab);

            using (new EditorGUI.DisabledScope(!_createPrefab))
            {
                _prefabFolder = DrawFolderField("预制体目录", _prefabFolder);

                _registerAddressable = EditorGUILayout.Toggle("登记到 Addressables", _registerAddressable);

                using (new EditorGUI.DisabledScope(!_registerAddressable))
                {
                    _addressableGroup = EditorGUILayout.TextField("Addressables 组（留空用默认组）", _addressableGroup);
                }
            }

            EditorGUILayout.Space(8f);

            string key = ResolveKey();
            EditorGUILayout.HelpBox(
                $"资源 Key：{key}\n" +
                $"脚本：{_scriptFolder}/{_panelName}.cs" +
                (_createPrefab ? $"\n预制体：{_prefabFolder}/{_panelName}.prefab" : string.Empty),
                MessageType.None);

            string error = Validate();

            if (!string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox(error, MessageType.Warning);

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(error)))
            {
                if (GUILayout.Button("创建", GUILayout.Height(32f)))
                    Create();
            }
        }

        private static string DrawFolderField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            value = EditorGUILayout.TextField(label, value);

            if (GUILayout.Button("...", GUILayout.Width(28f)))
            {
                string picked = EditorUtility.OpenFolderPanel(label, "Assets", string.Empty);

                if (!string.IsNullOrEmpty(picked) && picked.StartsWith(Application.dataPath))
                    value = "Assets" + picked.Substring(Application.dataPath.Length).Replace('\\', '/');
            }

            EditorGUILayout.EndHorizontal();
            return value;
        }

        private string ResolveKey()
        {
            return UISettings.Instance.defaultKeyPrefix + _panelName;
        }

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(_panelName))
                return "面板类名不能为空。";

            if (!IsValidIdentifier(_panelName))
                return "面板类名只能用字母、数字和下划线，且不能以数字开头。";

            if (!_scriptFolder.StartsWith("Assets"))
                return "脚本目录必须在 Assets 下。";

            if (_createPrefab && !_prefabFolder.StartsWith("Assets"))
                return "预制体目录必须在 Assets 下。";

            if (File.Exists($"{_scriptFolder}/{_panelName}.cs"))
                return $"{_scriptFolder}/{_panelName}.cs 已存在。";

            return null;
        }

        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsDigit(name[0]))
                return false;

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }

        private void Create()
        {
            EnsureFolder(_scriptFolder);

            string scriptPath = $"{_scriptFolder}/{_panelName}.cs";
            File.WriteAllText(scriptPath, BuildScript(), new UTF8Encoding(false));

            if (_createPrefab)
            {
                EnsureFolder(_prefabFolder);

                var pending = new PendingCreation
                {
                    typeName = _panelName,
                    ns = _namespace,
                    prefabPath = $"{_prefabFolder}/{_panelName}.prefab",
                    addressableKey = ResolveKey(),
                    addressableGroup = _addressableGroup,
                    registerAddressable = _registerAddressable
                };

                // 脚本还没编译，拿不到 Type，先存下来等域重载之后再继续
                SessionState.SetString(PendingKey, JsonUtility.ToJson(pending));
            }

            AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Debug.Log($"[新建面板] 已生成 {scriptPath}，等待编译完成后继续生成预制体。");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private string BuildScript()
        {
            var builder = new StringBuilder();
            bool hasNamespace = !string.IsNullOrWhiteSpace(_namespace);
            string indent = hasNamespace ? "    " : string.Empty;

            builder.AppendLine("using UnityEngine;");
            builder.AppendLine("using UnityEngine.UI;");
            builder.AppendLine("using WManager;");
            builder.AppendLine();

            if (hasNamespace)
            {
                builder.AppendLine($"namespace {_namespace}");
                builder.AppendLine("{");
            }

            if (_baseKind == BaseKind.Typed)
            {
                builder.AppendLine($"{indent}/// <summary>{_panelName} 的打开参数</summary>");
                builder.AppendLine($"{indent}public class {_panelName}Args");
                builder.AppendLine($"{indent}{{");
                builder.AppendLine($"{indent}    public int Tab;");
                builder.AppendLine($"{indent}}}");
                builder.AppendLine();
            }

            builder.AppendLine($"{indent}[UIPanelInfo(");
            builder.AppendLine($"{indent}    Key = \"{ResolveKey()}\",");
            builder.AppendLine($"{indent}    Layer = UILayer.{_layer},");
            builder.AppendLine($"{indent}    ShowMode = UIShowMode.{_showMode},");
            builder.AppendLine($"{indent}    Cache = {(_cache ? "true" : "false")})]");

            string baseType = _baseKind switch
            {
                BaseKind.Typed => $"UIPanel<{_panelName}Args>",
                BaseKind.Adaptive => "UIAdaptivePanel",
                _ => "UIPanel"
            };

            builder.AppendLine($"{indent}public class {_panelName} : {baseType}");
            builder.AppendLine($"{indent}{{");
            builder.AppendLine($"{indent}    [SerializeField] private Button closeButton;");
            builder.AppendLine();
            builder.AppendLine($"{indent}    protected override void OnCreate()");
            builder.AppendLine($"{indent}    {{");
            builder.AppendLine($"{indent}        BindClick(closeButton, Close);");
            builder.AppendLine($"{indent}    }}");
            builder.AppendLine();

            if (_baseKind == BaseKind.Typed)
            {
                builder.AppendLine($"{indent}    protected override void OnWillOpen({_panelName}Args args)");
                builder.AppendLine($"{indent}    {{");
                builder.AppendLine($"{indent}        // 打开前刷新数据，args 已经就绪");
                builder.AppendLine($"{indent}    }}");
            }
            else
            {
                builder.AppendLine($"{indent}    protected override void OnWillOpen(UIContext context)");
                builder.AppendLine($"{indent}    {{");
                builder.AppendLine($"{indent}        // 打开前刷新数据");
                builder.AppendLine($"{indent}    }}");
            }

            if (_baseKind == BaseKind.Adaptive)
            {
                builder.AppendLine();
                builder.AppendLine($"{indent}    protected override void OnOrientationChanged(UIOrientation orientation)");
                builder.AppendLine($"{indent}    {{");
                builder.AppendLine($"{indent}        // 横竖屏切换后重新排版。按钮不用重绑，两套布局的按钮在 OnCreate 里一起绑好即可");
                builder.AppendLine($"{indent}    }}");
            }

            builder.AppendLine($"{indent}}}");

            if (hasNamespace)
                builder.AppendLine("}");

            return builder.ToString();
        }

        // ============================================================ 编译完成后的第二阶段

        [Serializable]
        private class PendingCreation
        {
            public string typeName;
            public string ns;
            public string prefabPath;
            public string addressableKey;
            public string addressableGroup;
            public bool registerAddressable;
        }

        [DidReloadScripts]
        private static void ContinueAfterCompile()
        {
            string json = SessionState.GetString(PendingKey, string.Empty);

            if (string.IsNullOrEmpty(json))
                return;

            SessionState.EraseString(PendingKey);

            PendingCreation pending;

            try
            {
                pending = JsonUtility.FromJson<PendingCreation>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[新建面板] 读取待处理任务失败：{e.Message}");
                return;
            }

            var panelType = FindPanelType(pending);

            if (panelType == null)
            {
                Debug.LogError(
                    $"[新建面板] 找不到刚生成的类型 {pending.typeName}，预制体没有生成。" +
                    "请确认脚本编译没有报错，然后手动新建预制体。");
                return;
            }

            CreatePrefab(pending, panelType);
        }

        private static Type FindPanelType(PendingCreation pending)
        {
            string fullName = string.IsNullOrWhiteSpace(pending.ns)
                ? pending.typeName
                : $"{pending.ns}.{pending.typeName}";

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);

                if (type != null && typeof(UIPanel).IsAssignableFrom(type))
                    return type;
            }

            return null;
        }

        private static void CreatePrefab(PendingCreation pending, Type panelType)
        {
            var go = new GameObject(pending.typeName, typeof(RectTransform), typeof(CanvasGroup));

            try
            {
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                go.AddComponent(panelType);

                var transition = go.AddComponent<UITweenTransition>();
                transition.enableFade = true;
                transition.enableScale = true;
                transition.startScale = new Vector3(0.9f, 0.9f, 1f);

                // 一个默认的关闭按钮，正好演示自动绑定的命名约定
                var button = CreateCloseButton(rect);

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, pending.prefabPath);

                if (prefab != null)
                {
                    var panel = prefab.GetComponent(panelType) as UIPanel;

                    if (panel != null)
                        UIPanelAutoBinder.Bind(panel);

                    AssetDatabase.SaveAssets();
                }

                if (pending.registerAddressable)
                    RegisterAddressable(pending);

                var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(pending.prefabPath);
                Selection.activeObject = loaded;
                EditorGUIUtility.PingObject(loaded);

                Debug.Log(
                    $"[新建面板] 已生成预制体 {pending.prefabPath}。" +
                    $"打开方式：await UI.OpenAsync<{pending.typeName}>();",
                    loaded);

                // button 只是为了生成层级，实际引用已经写进预制体
                _ = button;
            }
            finally
            {
                DestroyImmediate(go);
            }
        }

        private static Button CreateCloseButton(RectTransform parent)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;

            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-32f, -32f);
            rect.sizeDelta = new Vector2(88f, 88f);

            return go.GetComponent<Button>();
        }

        private static void RegisterAddressable(PendingCreation pending)
        {
            try
            {
                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

                if (settings == null)
                {
                    Debug.LogWarning("[新建面板] 项目还没有初始化 Addressables 设置，跳过登记。");
                    return;
                }

                var group = string.IsNullOrWhiteSpace(pending.addressableGroup)
                    ? settings.DefaultGroup
                    : settings.FindGroup(pending.addressableGroup) ?? settings.DefaultGroup;

                string guid = AssetDatabase.AssetPathToGUID(pending.prefabPath);

                if (string.IsNullOrEmpty(guid))
                    return;

                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.SetAddress(pending.addressableKey);

                settings.SetDirty(
                    UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved,
                    entry,
                    true);

                AssetDatabase.SaveAssets();

                Debug.Log($"[新建面板] 已登记 Addressables：{pending.addressableKey} → {group.Name}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[新建面板] 登记 Addressables 失败，请手动添加：{e.Message}");
            }
        }
    }
}
