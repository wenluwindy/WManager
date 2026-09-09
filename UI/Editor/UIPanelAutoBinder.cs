using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WManager.UIEditor
{
    /// <summary>
    /// 按命名约定把面板的 [SerializeField] 引用自动绑到子节点上，省掉一个个手拖。
    ///
    /// 匹配规则：字段名和节点名都拆成词元，把类型相关的词元（button/btn、text/txt/tmp、
    /// image/img 等）去掉后比较剩下的部分。因此下面这些都能对上：
    /// <code>
    /// closeButton  ←  CloseButton / btn_Close / CloseBtn / Close
    /// titleText    ←  TitleText   / txt_Title / TitleLabel
    /// iconImage    ←  IconImage   / img_Icon
    /// </code>
    /// 找到多个候选时会跳过并给出警告，不会乱猜。
    /// </summary>
    public static class UIPanelAutoBinder
    {
        /// <summary>词元别名：缩写统一成全称</summary>
        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "btn", "button" },
            { "txt", "text" },
            { "lbl", "text" },
            { "label", "text" },
            { "tmp", "text" },
            { "img", "image" },
            { "spr", "image" },
            { "sld", "slider" },
            { "tgl", "toggle" },
            { "ipt", "inputfield" },
            { "input", "inputfield" },
            { "sr", "scrollrect" },
            { "scroll", "scrollrect" },
            { "cg", "canvasgroup" },
            { "go", "gameobject" },
            { "rt", "recttransform" },
            { "tf", "transform" },
            { "obj", "gameobject" },
            { "root", "gameobject" },
            { "node", "gameobject" }
        };

        [MenuItem("CONTEXT/UIPanel/自动绑定 UI 引用")]
        private static void BindFromContext(MenuCommand command)
        {
            if (command.context is UIPanel panel)
                Bind(panel, true);
        }

        [MenuItem("Tools/WManager/UI/自动绑定选中面板的 UI 引用")]
        private static void BindFromSelection()
        {
            var panels = new List<UIPanel>();

            foreach (var go in Selection.gameObjects)
            {
                var found = go.GetComponentsInChildren<UIPanel>(true);
                foreach (var panel in found)
                {
                    if (!panels.Contains(panel))
                        panels.Add(panel);
                }
            }

            if (panels.Count == 0)
            {
                EditorUtility.DisplayDialog("自动绑定", "请先在 Hierarchy 或 Project 里选中带 UIPanel 的对象。", "好");
                return;
            }

            foreach (var panel in panels)
                Bind(panel, true);
        }

        [MenuItem("Tools/WManager/UI/自动绑定选中面板的 UI 引用", true)]
        private static bool BindFromSelectionValidate()
        {
            return Selection.gameObjects.Length > 0;
        }

        /// <summary>
        /// 给面板做一次自动绑定。
        /// </summary>
        /// <param name="panel">目标面板</param>
        /// <param name="onlyEmpty">只填当前为空的引用，已经拖好的不动</param>
        /// <returns>本次绑定成功的字段数</returns>
        public static int Bind(UIPanel panel, bool onlyEmpty = true)
        {
            if (panel == null)
                return 0;

            var serialized = new SerializedObject(panel);
            var property = serialized.GetIterator();
            var report = new StringBuilder();

            int bound = 0;
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (property.propertyPath == "m_Script")
                    continue;

                if (onlyEmpty && property.objectReferenceValue != null)
                    continue;

                var fieldType = GetFieldType(panel.GetType(), property.propertyPath);
                if (fieldType == null)
                    continue;

                var match = FindMatch(panel.transform, property.name, fieldType, out string problem);

                if (match == null)
                {
                    if (!string.IsNullOrEmpty(problem))
                        report.AppendLine($"  · {property.name}：{problem}");

                    continue;
                }

                property.objectReferenceValue = match;
                report.AppendLine($"  · {property.name}  ←  {GetPath(panel.transform, match)}");
                bound++;
            }

            if (bound > 0)
            {
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(panel);
            }

            Debug.Log(
                $"[自动绑定] {panel.GetType().Name} 绑定 {bound} 个引用" +
                (report.Length > 0 ? $"\n{report}" : string.Empty),
                panel);

            return bound;
        }

        private static UnityEngine.Object FindMatch(Transform root, string fieldName, Type fieldType, out string problem)
        {
            problem = null;

            var fieldTokens = StripTypeTokens(Tokenize(fieldName), fieldType);
            var candidates = new List<UnityEngine.Object>();

            CollectCandidates(root, root, fieldType, fieldTokens, candidates);

            if (candidates.Count == 1)
                return candidates[0];

            if (candidates.Count > 1)
            {
                problem = $"找到 {candidates.Count} 个同名候选，跳过（请手动指定）";
                return null;
            }

            problem = $"没找到名字对得上的 {fieldType.Name} 子节点";
            return null;
        }

        private static void CollectCandidates(
            Transform root,
            Transform current,
            Type fieldType,
            HashSet<string> fieldTokens,
            List<UnityEngine.Object> results)
        {
            // 根节点自己不参与匹配，只看子节点
            if (current != root)
            {
                var nodeTokens = StripTypeTokens(Tokenize(current.name), fieldType);

                if (nodeTokens.SetEquals(fieldTokens))
                {
                    var value = Resolve(current, fieldType);

                    if (value != null)
                        results.Add(value);
                }
            }

            for (int i = 0; i < current.childCount; i++)
                CollectCandidates(root, current.GetChild(i), fieldType, fieldTokens, results);
        }

        private static UnityEngine.Object Resolve(Transform node, Type fieldType)
        {
            if (fieldType == typeof(GameObject))
                return node.gameObject;

            if (typeof(Component).IsAssignableFrom(fieldType))
                return node.GetComponent(fieldType);

            return null;
        }

        /// <summary>拆词：下划线、连字符、空格和驼峰边界都算分隔</summary>
        private static List<string> Tokenize(string name)
        {
            var tokens = new List<string>();

            if (string.IsNullOrEmpty(name))
                return tokens;

            var buffer = new StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (c == '_' || c == '-' || c == ' ' || c == '.' || c == '(' || c == ')')
                {
                    Flush(buffer, tokens);
                    continue;
                }

                bool boundary = i > 0
                                && char.IsUpper(c)
                                && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));

                if (boundary)
                    Flush(buffer, tokens);

                buffer.Append(char.ToLowerInvariant(c));
            }

            Flush(buffer, tokens);
            return tokens;
        }

        private static void Flush(StringBuilder buffer, List<string> tokens)
        {
            if (buffer.Length == 0)
                return;

            string token = buffer.ToString();
            buffer.Clear();

            if (Aliases.TryGetValue(token, out string alias))
                token = alias;

            tokens.Add(token);
        }

        /// <summary>去掉由字段类型隐含的词元，剩下的才是真正用来区分的名字</summary>
        private static HashSet<string> StripTypeTokens(List<string> tokens, Type fieldType)
        {
            var typeTokens = GetTypeTokens(fieldType);
            var result = new HashSet<string>();

            foreach (var token in tokens)
            {
                if (!typeTokens.Contains(token))
                    result.Add(token);
            }

            return result;
        }

        private static HashSet<string> GetTypeTokens(Type type)
        {
            var set = new HashSet<string>();

            foreach (var token in Tokenize(type.Name))
                set.Add(token);

            if (typeof(TMP_Text).IsAssignableFrom(type) || typeof(Text).IsAssignableFrom(type))
            {
                set.Add("text");
                set.Add("mesh");
                set.Add("pro");
                set.Add("ugui");
            }

            if (typeof(Button).IsAssignableFrom(type))
                set.Add("button");

            if (typeof(Image).IsAssignableFrom(type) || typeof(RawImage).IsAssignableFrom(type))
                set.Add("image");

            if (type == typeof(GameObject))
                set.Add("gameobject");

            return set;
        }

        private static Type GetFieldType(Type ownerType, string propertyPath)
        {
            var type = ownerType;

            while (type != null)
            {
                var field = type.GetField(
                    propertyPath,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);

                if (field != null)
                    return field.FieldType;

                type = type.BaseType;
            }

            return null;
        }

        private static string GetPath(Transform root, UnityEngine.Object target)
        {
            var transform = target as Transform;

            if (transform == null)
            {
                if (target is GameObject go)
                    transform = go.transform;
                else if (target is Component component)
                    transform = component.transform;
            }

            if (transform == null)
                return target.name;

            var parts = new List<string>();

            while (transform != null && transform != root)
            {
                parts.Insert(0, transform.name);
                transform = transform.parent;
            }

            return string.Join("/", parts);
        }
    }
}
