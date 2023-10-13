using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WManager
{
    public class MethodManager : MonoBehaviour
    {
        #region 单例模式
        private const string ASSET_BUNDLE_MANAGER_NAME = "MethodManager";
        public static MethodManager _instance;
        public static MethodManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject(ASSET_BUNDLE_MANAGER_NAME).AddComponent<MethodManager>();
                    DontDestroyOnLoad(_instance);
                }
                return _instance;
            }
        }
        #endregion

        private static Dictionary<int, Action> keyMethods = new();
        /// <summary>
        /// 当前方法索引
        /// </summary>
        private static int currentMethodIndex = -1;
        /// <summary>
        /// 存储当前事件链的字段
        /// </summary>
        public static IActionChain currentChain;

        /// <summary>
        /// 添加方法
        /// </summary>
        /// <param name="iAction">具体方法</param>
        public static void AddMethod(Action iAction)
        {
            int newIndex = keyMethods.Count;
            keyMethods.Add(newIndex, iAction);
        }
        /// <summary>
        /// 移除方法
        /// </summary>
        /// <param name="index">索引</param>
        public static void RemoveMethod(int index)
        {
            if (index >= 0 && index < keyMethods.Count)
            {
                keyMethods.Remove(index);
            }
        }
        /// <summary>
        /// 移除方法
        /// </summary>
        /// <param name="iAction">具体方法</param>
        public static void RemoveMethod(Action iAction)
        {
            // 寻找匹配的键值对
            List<int> matchingKeys = new List<int>();

            foreach (var kvp in keyMethods)
            {
                if (kvp.Value == iAction)
                {
                    matchingKeys.Add(kvp.Key);
                }
            }

            // 如果找到匹配的键值对，则移除它们
            foreach (int key in matchingKeys)
            {
                keyMethods.Remove(key);
            }

            // 如果没有找到匹配的键值对，则报错
            if (matchingKeys.Count == 0)
            {
                Debug.LogError("未找到匹配的方法");
            }
        }
        /// <summary>
        /// 执行下一个方法
        /// </summary>
        public static void NextMethod()
        {
            if (keyMethods.Count == 0) return;

            Stop();

            // 判断是否达到最后一个方法
            if (currentMethodIndex >= keyMethods.Count - 1)
            {
                Debug.Log("已经是最后一个方法");
                return;
            }

            //增加索引
            currentMethodIndex++;

            //执行下一个事件链
            keyMethods[currentMethodIndex]();
            Debug.Log("开始了：" + currentMethodIndex);
        }
        /// <summary>
        /// 执行上一个方法
        /// </summary>
        public static void PreviousMethod()
        {
            if (keyMethods.Count == 0) return;

            Stop();

            // 判断是否达到第一个方法
            if (currentMethodIndex <= 0)
            {
                Debug.Log("已经是第一个方法");
                return;
            }

            //减少索引
            currentMethodIndex--;

            //执行上一个事件链
            keyMethods[currentMethodIndex]();
            Debug.Log("开始了：" + currentMethodIndex);
        }
        /// <summary>
        /// 跳转步骤
        /// </summary>
        /// <param name="i">步骤索引(从0开始)</param>
        public static void JumpMethod(int i)
        {
            if (keyMethods.Count == 0) return;

            Stop();

            // 判断是否达到第一个方法
            if (i < 0 && i > keyMethods.Count)
            {
                Debug.Log("超过事件索引量，请更改跳转索引");
                return;
            }

            //指定索引
            currentMethodIndex = i;

            //执行上一个事件链
            keyMethods[currentMethodIndex]();
            Debug.Log("开始了：" + currentMethodIndex);
        }
        /// <summary>
        /// 停止当前事件链
        /// </summary>
        private static void Stop()
        {
            if (currentChain != null)
            {
                currentChain.Stop();
                currentChain = null;
            }
        }
    }
}

