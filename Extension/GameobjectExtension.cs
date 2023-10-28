using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using HighlightingSystem;
using UnityEngine.UI;

namespace WManager
{
    public static class GameobjectExtension
    {
        /// <summary>
        /// 打开高亮开关
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="_tweenGradient">高亮颜色</param>
        public static void OpenHighlighterSwitch(this GameObject gameObject, Gradient _tweenGradient)
        {
            var highlighter = gameObject.GetComponent<Highlighter>() ?? gameObject.AddComponent<Highlighter>();
            highlighter.tweenGradient = _tweenGradient;
            highlighter.tween = true;
        }
        /// <summary>
        /// 关闭高亮开关
        /// </summary>
        /// <param name="gameObject"></param>
        public static void CloseHighlighterSwitch(this GameObject gameObject)
        {
            var highlighter = gameObject.GetComponent<Highlighter>();
            if (highlighter != null)
            {
                UnityEngine.Object.Destroy(highlighter);
            }
        }
        /// <summary>
        /// 开启UI高亮开关
        /// </summary>
        /// <param name="button"></param>
        public static void OpenUIHighlighterSwitch(this Button button)
        {
            var highlighter = button.gameObject.AddComponent<UIHighlighter>() ?? button.gameObject.AddComponent<UIHighlighter>();
        }
        /// <summary>
        /// 关闭UI高亮开关
        /// </summary>
        /// <param name="gameObject"></param>
        public static void CloseUIHighlighterSwitch(this Button button)
        {
            var highlighter = button.gameObject.GetComponent<UIHighlighter>();
            if (highlighter != null)
            {
                UnityEngine.Object.Destroy(highlighter);
            }
        }
    }
}
