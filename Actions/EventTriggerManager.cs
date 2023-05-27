using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace WManager
{
    public static class EventTriggerManager
    {
        //用一个字典组来存储每个GameObject上不同事件类型对应的EventTriggerType和UnityAction
        private static Dictionary<GameObject, Dictionary<EventTriggerType, List<UnityAction>>> eventDic = new Dictionary<GameObject, Dictionary<EventTriggerType, List<UnityAction>>>();
        /// <summary>
        /// 添加事件 
        /// </summary>
        /// <param name="go">游戏物体</param> 
        /// <param name="type">事件类型</param> 
        /// <param name="action">响应事件</param>
        public static void AddEvent(GameObject go, EventTriggerType type, UnityAction action)
        {
            go.isStatic = false;//将物体改为非静态

            // 如果字典中不存在这个GameObject,添加一个新的键值对
            if (!eventDic.ContainsKey(go))
            {
                eventDic.Add(go, new Dictionary<EventTriggerType, List<UnityAction>>());
            }
            if (!eventDic[go].ContainsKey(type))
            {
                eventDic[go].Add(type, new List<UnityAction>());
            }

            // 给GameObject添加EventTrigger组件和BoxCollider组件
            EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
            // BoxCollider collider = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
            // collider.isTrigger = true;//设定为AddEvent添加的BoxCollider

            // 在EventTrigger组件上添加type类型的事件和响应action方法
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback.AddListener((eventData) => { action(); });
            trigger.triggers.Add(entry);

            // 将信息存入字典
            eventDic[go][type].Add(action);
        }

        /// <summary>
        /// 移除事件
        /// </summary> 
        /// <param name="go">游戏物体</param> 
        /// <param name="type">事件类型</param>
        public static void RemoveEvent(GameObject go, EventTriggerType type, UnityAction action)
        {
            // 如果字典中不存在这个GameObject,直接返回
            if (eventDic.ContainsKey(go) && eventDic[go].ContainsKey(type))
            {
                eventDic[go][type].Remove(action);

                // 找到GameObject上的type类型的事件响应
                EventTrigger trigger = go.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    EventTrigger.Entry entry = trigger.triggers.Find(e => e.eventID == type && e.callback.GetPersistentEventCount() > 0);

                    // 如果找到,移除这个事件响应
                    if (entry != null)
                    {
                        trigger.triggers.Remove(entry);

                        // 移除这个事件响应后,检查EventTrigger中是否还有其他事件
                        if (trigger.triggers.Count == 0)
                        {
                            GameObject.Destroy(trigger); // 删除EventTrigger组件
                                                         // 删除BoxCollider组件
                            // Collider collider = go.GetComponent<Collider>();
                            // if (collider is BoxCollider && (collider as BoxCollider).isTrigger)
                            //     GameObject.Destroy(collider);
                        }
                    }
                }

                // 从字典中移除这个信息
                if (eventDic[go][type].Count == 0)
                {
                    eventDic[go].Remove(type);
                }

                // 如果这个GameObject的事件响应字典为空,从大的字典中移除这个键
                if (eventDic[go].Count == 0)
                {
                    eventDic.Remove(go);
                }
            }
        }
        /// <summary>
        /// 为这个物体移除所有事件
        /// </summary>
        public static void RemoveAllEvent(this GameObject go)
        {
            // 如果字典中不存在这个GameObject,直接返回
            if (!eventDic.ContainsKey(go)) return;

            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                // 移除所有的事件响应
                trigger.triggers.Clear();

                // 删除EventTrigger组件和BoxCollider组件
                GameObject.Destroy(trigger);
                //检查Collider组件
                // Collider collider = go.GetComponent<Collider>();
                // if (collider is BoxCollider && (collider as BoxCollider).isTrigger)
                //     GameObject.Destroy(collider);
            }
            // 从字典中移除这个GameObject
            eventDic.Remove(go);
        }
    }

    public static class CustomEventTrigger
    {
        /// <summary>
        /// 为这个物体添加事件
        /// </summary>
        /// <param name="type">事件类型</param>
        /// <param name="action">事件</param>
        public static void AddEvent(this GameObject go, EventTriggerType type, UnityAction action)
        {
            EventTriggerManager.AddEvent(go, type, action);
        }
        /// <summary>
        /// 为这个物体移除一个类型的所有事件
        /// </summary>
        /// <param name="type">事件类型</param>
        public static void RemoveEvent(this GameObject go, EventTriggerType type, UnityAction action)
        {
            EventTriggerManager.RemoveEvent(go, type, action);
        }

        /// <summary>
        /// 为这个物体添加鼠标点击事件
        /// </summary>
        public static void OnClickAddListener(this GameObject go, UnityAction action, EventTriggerType type = EventTriggerType.PointerClick)
        {
            EventTriggerManager.AddEvent(go, type, action);
        }
        /// <summary>
        /// 为这个物体移除所有鼠标点击事件
        /// </summary>
        public static void OnClickRemoveListener(this GameObject go, UnityAction action, EventTriggerType type = EventTriggerType.PointerClick)
        {
            EventTriggerManager.RemoveEvent(go, type, action);
        }
        /// <summary>
        /// 为这个物体添加鼠标进入事件
        /// </summary>
        public static void OnEnterAddListener(this GameObject go, UnityAction action, EventTriggerType type = EventTriggerType.PointerEnter)
        {
            EventTriggerManager.AddEvent(go, type, action);
        }
        /// <summary>
        /// 为这个物体移除所有鼠标进入事件
        /// </summary>
        public static void OnEnterRemoveListener(this GameObject go, UnityAction action, EventTriggerType type = EventTriggerType.PointerEnter)
        {
            EventTriggerManager.RemoveEvent(go, type, action);
        }
        /// <summary>
        /// 为这个物体添加鼠标退出事件
        /// </summary>
        public static void OnExitAddListener(this GameObject go, UnityAction action, EventTriggerType type = EventTriggerType.PointerExit)
        {
            EventTriggerManager.AddEvent(go, type, action);
        }
        /// <summary>
        /// 为这个物体移除所有鼠标退出事件
        /// </summary>
        public static void OnExitRemoveListener(this GameObject go, UnityAction action, EventTriggerType type = EventTriggerType.PointerExit)
        {
            EventTriggerManager.RemoveEvent(go, type, action);
        }
    }
}