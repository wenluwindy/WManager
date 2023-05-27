using System.IO;
using System.Linq;
using UnityEditor;
using System.Text;

namespace WManager.Editor
{
    public static class UIScriptCreator
    {
        /// <summary>
        /// 脚本名
        /// </summary>
        public static string ScriptName = "UIViewBase";
        /// <summary>
        /// 模板路径
        /// </summary>
        private static string _templatePath = "Assets/WManager/UI/Editor/UIViewBase.txt";
        /// <summary>
        /// 生成路径
        /// </summary>
        private static string _spawnPath = "Assets/Scripts/";
        /// <summary>
        /// 模板内容
        /// </summary>
        private static string _templateContent;
        [MenuItem("Assets/Create/WManager/创建UI模板脚本", false, 0)]
        public static void CreateUIBase()
        {
            _templateContent = File.ReadAllText(_templatePath);
            //生成路径
            var path = _spawnPath + ScriptName + ".cs";
            //将组织好的内容写入文件
            File.WriteAllText(path, _templateContent, Encoding.UTF8);
            //刷新一下资源，不然创建好文件后第一时间不会显示
            AssetDatabase.Refresh();
        }
    }
}

