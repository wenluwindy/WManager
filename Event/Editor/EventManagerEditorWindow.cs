using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace WManager
{
    public class EventManagerEditorWindow : EditorWindow
    {
        private Dictionary<string, EventInfo> eventInfos = new Dictionary<string, EventInfo>();
        private Dictionary<string, bool> expandedEvents = new Dictionary<string, bool>();
        private Vector2 scrollPos;
        private string searchText = "";
        private bool isScanning = false;
        private string scanProgress = "";

        [MenuItem("Tools/事件查看器")]
        public static void ShowWindow()
        {
            GetWindow<EventManagerEditorWindow>("事件查看器");
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("扫描项目", EditorStyles.toolbarButton))
            {
                ScanProjectForEvents();
            }

            searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            // Color legend at the top
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("事件状态提示:");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUI.color = Color.green;
            GUILayout.Label("●", EditorStyles.boldLabel);
            GUI.color = Color.white;
            GUILayout.Label("有发射者和监听者");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUI.color = Color.red;
            GUILayout.Label("●", EditorStyles.boldLabel);
            GUI.color = Color.white;
            GUILayout.Label("有监听者没有发射者");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUI.color = Color.yellow;
            GUILayout.Label("●", EditorStyles.boldLabel);
            GUI.color = Color.white;
            GUILayout.Label("有发射者没有监听者");
            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();

            if (isScanning)
            {
                GUILayout.Label(scanProgress);
                return;
            }

            if (eventInfos.Count == 0)
            {
                GUILayout.Label("未找到事件。单击“扫描项目”进行分析。");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            var filteredEvents = eventInfos.Values.Where(e =>
                string.IsNullOrEmpty(searchText) || e.EventName.Contains(searchText))
                .OrderBy(e => e.EventName)
                .ToList();

            foreach (var eventInfo in filteredEvents)
            {
                // Initialize expanded state if not exists
                if (!expandedEvents.ContainsKey(eventInfo.EventName))
                {
                    expandedEvents[eventInfo.EventName] = false;
                }

                // Determine event color
                Color eventColor = GetEventColor(eventInfo);

                GUILayout.BeginVertical(EditorStyles.helpBox);

                // Event header with expand/collapse button
                GUILayout.BeginHorizontal();

                // Expand/collapse button
                expandedEvents[eventInfo.EventName] = EditorGUILayout.Foldout(expandedEvents[eventInfo.EventName], "");

                // Event name with color
                GUI.color = eventColor;
                GUILayout.Label(eventInfo.EventName, EditorStyles.boldLabel);
                GUI.color = Color.white;

                // Event count info
                GUILayout.Label($"(监听者: {eventInfo.Listeners.Count}, 发射者: {eventInfo.Emitters.Count})", GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();

                // Show details if expanded
                if (expandedEvents[eventInfo.EventName])
                {
                    if (eventInfo.Listeners.Count > 0)
                    {
                        GUILayout.Label("监听者:", EditorStyles.boldLabel);
                        foreach (var listener in eventInfo.Listeners)
                        {
                            GUILayout.BeginHorizontal();

                            // Show commented warning if needed
                            if (listener.IsCommented)
                            {
                                GUI.color = Color.red;
                                GUILayout.Label("[已注释]", GUILayout.ExpandWidth(false));
                                GUI.color = Color.white;
                            }

                            // Show script path and line number
                            GUILayout.Label($"  {listener.ScriptPath}  (行: {listener.LineNumber})", GUILayout.ExpandWidth(true));

                            // Locate button
                            if (GUILayout.Button("定位", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                            {
                                LocateScript(listener.ScriptPath);
                            }

                            // Open button
                            if (GUILayout.Button("打开", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                            {
                                OpenScript(listener.ScriptPath, listener.LineNumber);
                            }

                            GUILayout.EndHorizontal();
                        }
                    }

                    if (eventInfo.Emitters.Count > 0)
                    {
                        GUILayout.Label("发射者:", EditorStyles.boldLabel);
                        foreach (var emitter in eventInfo.Emitters)
                        {
                            GUILayout.BeginHorizontal();

                            // Show commented warning if needed
                            if (emitter.IsCommented)
                            {
                                GUI.color = Color.red;
                                GUILayout.Label("[已注释]", GUILayout.ExpandWidth(false));
                                GUI.color = Color.white;
                            }

                            // Show script path and line number
                            GUILayout.Label($"  {emitter.ScriptPath}  (行: {emitter.LineNumber})", GUILayout.ExpandWidth(true));

                            // Locate button
                            if (GUILayout.Button("定位", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                            {
                                LocateScript(emitter.ScriptPath);
                            }

                            // Open button
                            if (GUILayout.Button("打开", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                            {
                                OpenScript(emitter.ScriptPath, emitter.LineNumber);
                            }

                            GUILayout.EndHorizontal();
                        }
                    }
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        private Color GetEventColor(EventInfo eventInfo)
        {
            // Count only uncommented listeners and emitters
            int uncommentedListeners = eventInfo.Listeners.Count(l => !l.IsCommented);
            int uncommentedEmitters = eventInfo.Emitters.Count(e => !e.IsCommented);
            
            if (uncommentedListeners > 0 && uncommentedEmitters > 0)
            {
                return Color.green; // 有发射者和监听者
            }
            else if (uncommentedListeners > 0 && uncommentedEmitters == 0)
            {
                return Color.red; // 有监听者没有发射者（包括被注释掉的发射者）
            }
            else if (uncommentedListeners == 0 && uncommentedEmitters > 0)
            {
                return Color.yellow; // 有发射者没有监听者（包括被注释掉的监听者）
            }
            return Color.white; // 默认颜色
        }

        private void ScanProjectForEvents()
        {
            eventInfos.Clear();
            expandedEvents.Clear();
            isScanning = true;
            scanProgress = "扫描项目文件...";
            Repaint();

            // Search all C# files in the project
            string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            int processed = 0;

            foreach (string filePath in csFiles)
            {
                processed++;
                scanProgress = $"扫描中 {processed}/{csFiles.Length}: {Path.GetFileName(filePath)}";
                Repaint();

                string fileContent = File.ReadAllText(filePath);
                string[] lines = fileContent.Split('\n');

                // Find StartListening calls and their line numbers
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    Match match = Regex.Match(line, @"StartListening\s*\(\s*[""']([^""']+)['""]");
                    if (match.Success && match.Groups.Count >= 2)
                    {
                        string eventName = match.Groups[1].Value;
                        if (!eventInfos.ContainsKey(eventName))
                        {
                            eventInfos[eventName] = new EventInfo { EventName = eventName };
                        }
                        string scriptPath = filePath.Replace(Application.dataPath, "Assets");
                        bool isCommented = IsLineCommented(line);
                        eventInfos[eventName].Listeners.Add(new ScriptLocation { ScriptPath = scriptPath, LineNumber = i + 1, IsCommented = isCommented });
                    }
                }

                // Find EmitEvent calls and their line numbers
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    Match match = Regex.Match(line, @"EmitEvent\s*\(\s*[""']([^""']+)['""]");
                    if (match.Success && match.Groups.Count >= 2)
                    {
                        string eventName = match.Groups[1].Value;
                        if (!eventInfos.ContainsKey(eventName))
                        {
                            eventInfos[eventName] = new EventInfo { EventName = eventName };
                        }
                        string scriptPath = filePath.Replace(Application.dataPath, "Assets");
                        bool isCommented = IsLineCommented(line);
                        eventInfos[eventName].Emitters.Add(new ScriptLocation { ScriptPath = scriptPath, LineNumber = i + 1, IsCommented = isCommented });
                    }
                }

                // Find EmitEventData calls and their line numbers
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    Match match = Regex.Match(line, @"EmitEventData\s*\(\s*[""']([^""']+)['""]");
                    if (match.Success && match.Groups.Count >= 2)
                    {
                        string eventName = match.Groups[1].Value;
                        if (!eventInfos.ContainsKey(eventName))
                        {
                            eventInfos[eventName] = new EventInfo { EventName = eventName };
                        }
                        string scriptPath = filePath.Replace(Application.dataPath, "Assets");
                        bool isCommented = IsLineCommented(line);
                        eventInfos[eventName].Emitters.Add(new ScriptLocation { ScriptPath = scriptPath, LineNumber = i + 1, IsCommented = isCommented });
                    }
                }
            }

            scanProgress = "扫描完成!";
            isScanning = false;
            Repaint();
        }

        private bool IsLineCommented(string line)
        {
            // Check if the line starts with // or is within /* */ comments
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.Contains("/*") && trimmedLine.Contains("*/");
        }

        private void LocateScript(string scriptPath)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null)
            {
                EditorUtility.DisplayDialog("出错了！", $"未能找到脚本。\n路径：{scriptPath}。", "确认");
                return;
            }

            Selection.activeObject = script;
            EditorGUIUtility.PingObject(script);
        }

        private void OpenScript(string scriptPath, int lineNumber)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null)
            {
                EditorUtility.DisplayDialog("出错了！", $"未能找到脚本。\n路径：{scriptPath}。", "确认");
            }
            else
            {
                AssetDatabase.OpenAsset(script, lineNumber);
            }
        }

        // Class to store script location and line number
        private class ScriptLocation
        {
            public string ScriptPath { get; set; }
            public int LineNumber { get; set; }
            public bool IsCommented { get; set; }
        }

        private class EventInfo
        {
            public string EventName { get; set; }
            public List<ScriptLocation> Listeners { get; set; }
            public List<ScriptLocation> Emitters { get; set; }

            public EventInfo()
            {
                Listeners = new List<ScriptLocation>();
                Emitters = new List<ScriptLocation>();
            }
        }
    }
}