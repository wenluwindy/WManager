using System.IO;
using System.Linq;
using UnityEditor;
using System.Text;
using UnityEngine;

namespace WManager.Editor
{
    public class UIScriptCreator : EditorWindow
    {
        private static string TEMPLATE_PATH = "Assets/WManager/UI/Editor/UIViewBase.txt";// 模板路径
        private string DEFAULT_SCRIPT_NAME = "UIViewBase";
        private void OnEnable()
        {
            // Set the minimum and maximum size of the window
            minSize = new Vector2(200f, 100f);
            maxSize = new Vector2(400f, 120f);

            // Set the position and size of the window
            position = new Rect(100f, 100f, 300f, 120f);
        }

        [MenuItem("Assets/Create/WManager/创建UI模板脚本")]
        public static void ShowWindow()
        {
            GetWindow<UIScriptCreator>("创建UI模板脚本");
        }
        private void OnGUI()
        {
            GUILayout.Label("输入脚本名:", EditorStyles.boldLabel);
            DEFAULT_SCRIPT_NAME = EditorGUILayout.TextField(DEFAULT_SCRIPT_NAME);

            if (GUILayout.Button("创建"))
            {
                CreateCustomScript();
            }
        }
        private void CreateCustomScript()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (selectedPath == "")
            {
                selectedPath = "Assets";
            }
            else if (Path.GetExtension(selectedPath) != "")
            {
                selectedPath = selectedPath.Replace(Path.GetFileName(selectedPath), "");
            }

            string fullPath = Path.Combine(selectedPath, DEFAULT_SCRIPT_NAME + ".cs");
            int count = 1;
            while (File.Exists(fullPath))
            {
                DEFAULT_SCRIPT_NAME = "NewUIView" + count;
                fullPath = Path.Combine(selectedPath, DEFAULT_SCRIPT_NAME + ".cs");
                count++;
            }

            string templateContent = File.ReadAllText(TEMPLATE_PATH);
            templateContent = templateContent.Replace("CustomScript", DEFAULT_SCRIPT_NAME);

            File.WriteAllText(fullPath, templateContent);

            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(fullPath);
            Object createdAsset = AssetDatabase.LoadAssetAtPath(fullPath, typeof(Object));
            Selection.activeObject = createdAsset;
            EditorGUIUtility.PingObject(createdAsset);
            Close();
        }

    }
}

