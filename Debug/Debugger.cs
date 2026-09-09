using System;
using System.Collections.Generic;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 调试器入口。
    /// - 快捷键切换可见性
    /// - 抓取并显示日志
    /// - 分发 OnGUI 给各 DebugPanel 绘制
    /// - 保留 Hierarchy 面板（场景对象检视）
    /// </summary>
    public class Debugger : SingletonBehaviour<Debugger>
    {
        /// <summary>
        /// 是否允许调试
        /// </summary>
        public bool AllowDebugging = true;
        public KeyCode AllowDebuggingHotKey = KeyCode.BackQuote;
        public GUISkin mySkin;
        public float windowScale = 1.0f;

        // 日志抓取
        private readonly List<LogData> _logInformations = new List<LogData>();
        private int _currentLogIndex = -1;
        private int _infoLogCount;
        private int _warningLogCount;
        private int _errorLogCount;
        private int _fatalLogCount;
        private bool _showInfoLog = true;
        private bool _showWarningLog = true;
        private bool _showErrorLog = true;
        private bool _showFatalLog = true;

        private Vector2 _scrollLogView = Vector2.zero;
        private Vector2 _scrollCurrentLogView = Vector2.zero;

        // 面板
        private DebugType _debugType = DebugType.Console;
        private FpsPanel _fpsPanel = new FpsPanel();
        private MemoryPanel _memoryPanel = new MemoryPanel();
        private SystemPanel _systemPanel = new SystemPanel();

        // Hierarchy
        private Vector2 _scrollHierarchyView = Vector2.zero;
        private HashSet<int> _expandedObjects = new HashSet<int>();
        private GameObject _selectedGameObject;
        private Vector2 _scrollComponentView = Vector2.zero;

        // Window
        private bool _expansion = false;
        private Rect _windowRect = new Rect(0, 0, 100, 60);
        private Color _fpsColor = Color.white;

        private bool _isResizing = false;
        private Vector2 _resizeStartMousePos;
        private Rect _resizeStartWindowRect;
        private const float _resizeHandleSize = 15f;

        private void OnEnable()
        {
            if (AllowDebugging)
                Application.logMessageReceived += LogHandler;
        }

        private void OnDisable()
        {
            if (AllowDebugging)
                Application.logMessageReceived -= LogHandler;
        }

        private void Update()
        {
            if (Input.GetKeyDown(AllowDebuggingHotKey))
                AllowDebugging = !AllowDebugging;

            if (AllowDebugging)
                _fpsPanel.Tick(Time.unscaledDeltaTime);
        }

        private void LogHandler(string condition, string stackTrace, LogType type)
        {
            LogData log = new LogData
            {
                time = DateTime.Now.ToString("HH:mm:ss"),
                message = condition,
                stackTrace = stackTrace
            };

            switch (type)
            {
                case LogType.Assert:
                    log.type = "Fatal"; _fatalLogCount++; break;
                case LogType.Exception:
                case LogType.Error:
                    log.type = "Error"; _errorLogCount++; break;
                case LogType.Warning:
                    log.type = "Warning"; _warningLogCount++; break;
                case LogType.Log:
                    log.type = "Info"; _infoLogCount++; break;
            }

            _logInformations.Add(log);

            if (_warningLogCount > 0) _fpsColor = Color.yellow;
            if (_errorLogCount > 0) _fpsColor = Color.red;
        }

        private void OnGUI()
        {
            GUI.skin = mySkin;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(windowScale, windowScale, 1.0f));

            if (!AllowDebugging) return;

            if (_expansion)
                _windowRect = GUI.Window(0, _windowRect, ExpansionGUIWindow, "调试器");
            else
                _windowRect = GUI.Window(0, _windowRect, ShrinkGUIWindow, "调试器");
        }

        private void ExpansionGUIWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            DrawTitleBar();

            switch (_debugType)
            {
                case DebugType.Console: DrawConsolePanel(); break;
                case DebugType.Hierarchy: DrawHierarchyPanel(); break;
                case DebugType.Memory: DrawPanel(_memoryPanel); break;
                case DebugType.System: DrawPanel(_systemPanel); break;
                case DebugType.Screen: DrawScreenPanel(); break;
                case DebugType.Quality: DrawQualityPanel(); break;
                case DebugType.Environment: DrawEnvironmentPanel(); break;
            }

            HandleResize(windowId);
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal();
            GUI.contentColor = _fpsColor;
            if (GUILayout.Button($"FPS: {_fpsPanel.CurrentFps}", GUILayout.Height(30)))
            {
                _expansion = false;
                _windowRect.width = 100;
                _windowRect.height = 60;
            }
            GUI.contentColor = Color.white;

            DrawTabButton("控制台", DebugType.Console);
            DrawTabButton("层级", DebugType.Hierarchy);
            DrawTabButton("内存", DebugType.Memory);
            DrawTabButton("系统", DebugType.System);
            DrawTabButton("屏幕", DebugType.Screen);
            DrawTabButton("质量", DebugType.Quality);
            DrawTabButton("环境", DebugType.Environment);

            GUILayout.EndHorizontal();
        }

        private void DrawTabButton(string label, DebugType type)
        {
            GUI.contentColor = _debugType == type ? Color.white : Color.gray;
            if (GUILayout.Button(label, GUILayout.Height(30)))
                _debugType = type;
            GUI.contentColor = Color.white;
        }

        private void DrawPanel(DebugPanel panel)
        {
            GUILayout.BeginVertical("Box");
            panel.DrawGUI();
            GUILayout.EndVertical();
        }

        private void DrawConsolePanel()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("清除"))
            {
                _logInformations.Clear();
                _fatalLogCount = _warningLogCount = _errorLogCount = _infoLogCount = 0;
                _currentLogIndex = -1;
                _fpsColor = Color.white;
            }
            GUI.contentColor = _showInfoLog ? Color.white : Color.gray;
            _showInfoLog = GUILayout.Toggle(_showInfoLog, $"Info [{_infoLogCount}]");
            GUI.contentColor = _showWarningLog ? Color.white : Color.gray;
            _showWarningLog = GUILayout.Toggle(_showWarningLog, $"Warning [{_warningLogCount}]");
            GUI.contentColor = _showErrorLog ? Color.white : Color.gray;
            _showErrorLog = GUILayout.Toggle(_showErrorLog, $"Error [{_errorLogCount}]");
            GUI.contentColor = _showFatalLog ? Color.white : Color.gray;
            _showFatalLog = GUILayout.Toggle(_showFatalLog, $"Fatal [{_fatalLogCount}]");
            GUI.contentColor = Color.white;
            GUILayout.EndHorizontal();

            _scrollLogView = GUILayout.BeginScrollView(_scrollLogView, "Box", GUILayout.Height(165));
            for (int i = 0; i < _logInformations.Count; i++)
            {
                if (!ShouldShowLog(_logInformations[i].type)) continue;

                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_currentLogIndex == i, ""))
                    _currentLogIndex = i;
                GUI.contentColor = GetLogColor(_logInformations[i].type);
                GUILayout.Label($"[{_logInformations[i].type}] ");
                GUILayout.Label($"[{_logInformations[i].time}] ");
                GUILayout.Label(_logInformations[i].message);
                GUILayout.FlexibleSpace();
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            _scrollCurrentLogView = GUILayout.BeginScrollView(_scrollCurrentLogView, "Box", GUILayout.Height(100));
            if (_currentLogIndex != -1)
            {
                GUILayout.Label(_logInformations[_currentLogIndex].message + "\r\n\r\n" + _logInformations[_currentLogIndex].stackTrace);
            }
            GUILayout.EndScrollView();
        }

        private bool ShouldShowLog(string type) => type switch
        {
            "Fatal" => _showFatalLog,
            "Error" => _showErrorLog,
            "Info" => _showInfoLog,
            "Warning" => _showWarningLog,
            _ => true
        };

        private Color GetLogColor(string type) => type switch
        {
            "Fatal" => Color.red,
            "Error" => Color.red,
            "Warning" => Color.yellow,
            _ => Color.white
        };

        private void DrawHierarchyPanel()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("场景层级 (包含常驻内存物体)");
            GUILayout.EndHorizontal();

            _scrollHierarchyView = GUILayout.BeginScrollView(_scrollHierarchyView, "Box");
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GUILayout.Label($"<b>Active Scene: {activeScene.name}</b>");
            foreach (var obj in activeScene.GetRootGameObjects())
                DrawObjectTree(obj, 0);

            GUILayout.Space(10);
            GUILayout.Label("<b>DontDestroyOnLoad</b>");
            var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in allTransforms)
            {
                if (t.parent == null && t.gameObject.scene.name == "DontDestroyOnLoad")
                    DrawObjectTree(t.gameObject, 0);
            }
            GUILayout.EndScrollView();

            GUILayout.Label("<b>组件详情 (Inspector)</b>");
            _scrollComponentView = GUILayout.BeginScrollView(_scrollComponentView, "Box", GUILayout.Height(120));
            if (_selectedGameObject != null) DrawComponentInspector(_selectedGameObject);
            else GUILayout.Label("请在上方选择一个物体");
            GUILayout.EndScrollView();
        }

        private void DrawObjectTree(GameObject obj, int indent)
        {
            if (obj == null) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20);

            bool isActive = GUILayout.Toggle(obj.activeSelf, "", GUILayout.Width(20));
            if (isActive != obj.activeSelf) obj.SetActive(isActive);

            bool hasChildren = obj.transform.childCount > 0;
            if (hasChildren)
            {
                bool isExpanded = _expandedObjects.Contains(obj.GetInstanceID());
                if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(20)))
                {
                    if (isExpanded) _expandedObjects.Remove(obj.GetInstanceID());
                    else _expandedObjects.Add(obj.GetInstanceID());
                }
            }
            else { GUILayout.Space(25); }

            GUI.contentColor = _selectedGameObject == obj ? Color.cyan : (obj.activeInHierarchy ? Color.white : Color.gray);

            if (GUILayout.Button(obj.name, GUILayout.ExpandWidth(true)))
            {
                _selectedGameObject = obj;
#if UNITY_EDITOR
                UnityEditor.Selection.activeGameObject = obj;
#endif
            }

            GUILayout.Label($"[{obj.GetComponents<Component>().Length}]", GUILayout.Width(30));
            GUI.contentColor = Color.white;
            GUILayout.EndHorizontal();

            if (hasChildren && _expandedObjects.Contains(obj.GetInstanceID()))
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                    DrawObjectTree(obj.transform.GetChild(i).gameObject, indent + 1);
            }
        }

        private void DrawComponentInspector(GameObject target)
        {
            Component[] components = target.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;

                GUILayout.BeginHorizontal("Box");
                var behaviour = comp as Behaviour;
                if (behaviour != null)
                    behaviour.enabled = GUILayout.Toggle(behaviour.enabled, "", GUILayout.Width(20));
                else
                    GUILayout.Space(25);

                string compName = comp.GetType().Name;
                GUILayout.Label(compName, GUILayout.ExpandWidth(true));

                if (comp is Transform t)
                {
                    GUILayout.BeginVertical();
                    t.localPosition = DrawVector3Vertical("Position", t.localPosition);
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    Vector3 rotation = t.localEulerAngles;
                    rotation = DrawVector3Vertical("Rotation", rotation);
                    if (GUI.changed) t.localEulerAngles = rotation;
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    t.localScale = DrawVector3Vertical("Scale", t.localScale);
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }
        }

        private Vector3 DrawVector3Vertical(string title, Vector3 value)
        {
            GUILayout.Label($"<color=yellow>{title}</color>");

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("X", GUILayout.Width(20));
            value.x = DrawFloat(value.x);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Y", GUILayout.Width(20));
            value.y = DrawFloat(value.y);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Z", GUILayout.Width(20));
            value.z = DrawFloat(value.z);
            GUILayout.EndHorizontal();

            return value;
        }

        private float DrawFloat(float value)
        {
            string text = GUILayout.TextField(value.ToString("F2"), GUILayout.ExpandWidth(true));
            if (float.TryParse(text, out float result)) return result;
            return value;
        }

        private void DrawScreenPanel()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("屏幕信息");
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("Box");
            GUILayout.Label($"DPI：{Screen.dpi}");
            GUILayout.Label($"分辨率：{Screen.currentResolution}");
            GUILayout.EndVertical();

            if (GUILayout.Button("全屏"))
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, !Screen.fullScreen);
        }

        private void DrawQualityPanel()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("图形质量信息");
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("Box");
            string value = "";
            if (QualitySettings.GetQualityLevel() == 0) value = " [最低]";
            else if (QualitySettings.GetQualityLevel() == QualitySettings.names.Length - 1) value = " [最高]";
            GUILayout.Label($"图形质量：{QualitySettings.names[QualitySettings.GetQualityLevel()]}{value}");
            GUILayout.EndVertical();

            if (GUILayout.Button("降低一级图形质量")) QualitySettings.DecreaseLevel();
            if (GUILayout.Button("提升一级图形质量")) QualitySettings.IncreaseLevel();
        }

        private void DrawEnvironmentPanel()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("环境信息");
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("Box");
            GUILayout.Label($"项目名称：{Application.productName}");
            GUILayout.Label($"项目ID：{Application.identifier}");
            GUILayout.Label($"项目版本：{Application.version}");
            GUILayout.Label($"Unity版本：{Application.unityVersion}");
            GUILayout.Label($"公司名称：{Application.companyName}");
            GUILayout.EndVertical();

            if (GUILayout.Button("退出程序")) Application.Quit();
        }

        private void ShrinkGUIWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            GUI.contentColor = _fpsColor;
            if (GUILayout.Button($"FPS: {_fpsPanel.CurrentFps}", GUILayout.Width(80), GUILayout.Height(30)))
            {
                _expansion = true;
                _windowRect.width = 600;
                _windowRect.height = 360;
            }
            GUI.contentColor = Color.white;
            HandleResize(windowId);
        }

        private void HandleResize(int windowId)
        {
            Rect resizeHandleRect = new Rect(_windowRect.width - _resizeHandleSize, _windowRect.height - _resizeHandleSize, _resizeHandleSize, _resizeHandleSize);
            GUI.Label(resizeHandleRect, "◢");

            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && resizeHandleRect.Contains(currentEvent.mousePosition))
            {
                _isResizing = true;
                _resizeStartMousePos = currentEvent.mousePosition;
                _resizeStartWindowRect = _windowRect;
                currentEvent.Use();
            }

            if (_isResizing)
            {
                if (currentEvent.type == EventType.MouseDrag)
                {
                    float diffX = currentEvent.mousePosition.x - _resizeStartMousePos.x;
                    float diffY = currentEvent.mousePosition.y - _resizeStartMousePos.y;
                    _windowRect.width = Mathf.Max(100, _resizeStartWindowRect.width + diffX);
                    _windowRect.height = Mathf.Max(60, _resizeStartWindowRect.height + diffY);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseUp)
                {
                    _isResizing = false;
                }
            }
        }
    }

    public struct LogData
    {
        public string time;
        public string type;
        public string message;
        public string stackTrace;
    }

    public enum DebugType
    {
        Console,
        Hierarchy,
        Memory,
        System,
        Screen,
        Quality,
        Environment
    }
}