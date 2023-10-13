using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WManager
{
    public static class EventTriggerManager
    {
        //用一个字典组来存储每个GameObject上不同事件类型对应的EventTriggerType和UnityAction
        private static Dictionary<int, (GameObject, Dictionary<EventTriggerType, List<UnityAction>>)> eventDic = new();

        /// <summary>
        /// 添加事件 
        /// </summary>
        /// <param name="gameObject">游戏物体</param> 
        /// <param name="type">事件类型</param> 
        /// <param name="action">响应事件</param>
        public static void AddEvent(GameObject gameObject, EventTriggerType type, UnityAction action)
        {
            // 检查参数合法性
            if (gameObject == null) return;
            //将物体改为非静态
            gameObject.isStatic = false;
            // 获取GameObject的实例ID
            int instanceId = gameObject.GetInstanceID();

            // 如果字典中存在这个GameObject,则查找物体下的EventTrigger组件
            if (eventDic.ContainsKey(instanceId))
            {
                EventTriggerType triggerType = type;
                //如果存在这个类型
                if (eventDic[instanceId].Item2.ContainsKey(triggerType))
                {
                    //如果存在这个事件
                    if (eventDic[instanceId].Item2[triggerType].Contains(action))
                    {
                        return;
                    }
                    else//不存在则添加一个事件
                    {
                        eventDic[instanceId].Item2[triggerType].Add(action);

                        // 在EventTrigger组件上添加这个type类型下的action方法
                        EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
                        EventTrigger.Entry foundEntry = trigger.triggers.Find(x => x.eventID == type);
                        foundEntry.callback.AddListener((eventData) => { TriggerEvent(action); });
                    }
                }
                else//不存在则创建这个类型
                {
                    eventDic[instanceId].Item2.Add(triggerType, new List<UnityAction>());
                    eventDic[instanceId].Item2[triggerType].Add(action);

                    // 在EventTrigger组件上添加type类型的事件和响应action方法
                    EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = type;
                    entry.callback.AddListener((eventData) => { TriggerEvent(action); });
                    trigger.triggers.Add(entry);
                }
            }
            else//不存在则创建这个物体键值对
            {
                eventDic.Add(instanceId, (gameObject, new Dictionary<EventTriggerType, List<UnityAction>>()));
                EventTriggerType triggerType = type;
                eventDic[instanceId].Item2.Add(triggerType, new List<UnityAction>());
                eventDic[instanceId].Item2[triggerType].Add(action);

                // 给GameObject添加EventTrigger组件和Collider组件
                EventTrigger trigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
                Collider collider = gameObject.GetComponent<Collider>() ?? gameObject.AddComponent<Collider>();
                // collider.isTrigger = true;//设定为AddEvent添加的BoxCollider

                // 在EventTrigger组件上添加type类型的事件和响应action方法
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = type;
                entry.callback.AddListener((eventData) => { TriggerEvent(action); });
                trigger.triggers.Add(entry);
            }
        }

        /// <summary>
        /// 移除事件
        /// </summary> 
        /// <param name="go">游戏物体</param> 
        /// <param name="type">事件类型</param>
        public static void RemoveEvent(GameObject go, EventTriggerType type, UnityAction action)
        {
            int goInstanceID = go.GetInstanceID();
            // 如果字典中存在这个GameObject
            if (eventDic.ContainsKey(goInstanceID))
            {
                //Debug.Log("存在物体");
                // 如果存在这个类型
                if (eventDic[goInstanceID].Item2.ContainsKey(type))
                {
                    //todo：移除指定方法无效
                    // Debug.Log("存在类型");
                    // List<UnityAction> actionList = eventDic[goInstanceID].Item2[type];
                    // // 查找并移除指定的UnityAction
                    // if (actionList.Contains(action))
                    // {
                    //     Debug.Log("存在方法");
                    //     // 在EventTrigger组件上移除对应类型的事件回调
                    //     EventTrigger trigger = go.GetComponent<EventTrigger>();
                    //     EventTrigger.Entry foundEntry = trigger.triggers.Find(x => x.eventID == type);
                    //     if (foundEntry != null)
                    //     {
                    //         foundEntry.callback.RemoveListener((eventData) => { TriggerEvent(action); });
                    //         //移除字典内容
                    //         actionList.Remove(action);
                    //         Debug.Log(eventDic[goInstanceID].Item2[type].Count);
                    //         Debug.Log("移除方法");
                    //     }

                    //     // 如果没有剩余的UnityAction，从字典中移除事件类型
                    //     if (actionList.Count == 0)
                    //     {
                    //         eventDic[goInstanceID].Item2.Remove(type);
                    //     }

                    //     // 如果没有剩余的事件类型，从字典中移除GameObject的条目
                    //     if (eventDic[goInstanceID].Item2.Count == 0)
                    //     {
                    //         eventDic.Remove(goInstanceID);
                    //     }
                    // }

                    //!直接移除类型下所有方法
                    EventTrigger trigger = go.GetComponent<EventTrigger>();
                    List<UnityAction> actionList = eventDic[goInstanceID].Item2[type];

                    // 找到要移除的事件回调
                    EventTrigger.Entry entryToRemove = trigger.triggers.Find(entry => entry.eventID == type);

                    if (entryToRemove != null)
                    {
                        // 从事件列表中移除找到的事件回调
                        trigger.triggers.Remove(entryToRemove);
                        // 从字典中移除该类型
                        eventDic[goInstanceID].Item2.Remove(type);
                    }

                }
            }
        }
        /// <summary>
        /// 为这个物体移除所有事件
        /// </summary>
        public static void RemoveAllEvent(this GameObject go)
        {
            int goInstanceID = go.GetInstanceID();
            // 如果字典中存在这个GameObject
            if (eventDic.ContainsKey(goInstanceID))
            {
                // 获取该GameObject对应的所有事件类型
                Dictionary<EventTriggerType, List<UnityAction>> eventTypes = eventDic[goInstanceID].Item2;

                // 遍历事件类型并移除事件回调
                foreach (var eventType in eventTypes.Keys)
                {
                    List<UnityAction> actionList = eventTypes[eventType];
                    EventTrigger trigger = go.GetComponent<EventTrigger>();

                    // 移除所有事件回调
                    foreach (var action in actionList)
                    {
                        EventTrigger.Entry foundEntry = trigger.triggers.Find(x => x.eventID == eventType);
                        if (foundEntry != null)
                        {
                            foundEntry.callback.RemoveListener((eventData) => { action(); });
                        }
                    }
                }

                // 清空事件字典中与该GameObject相关的条目
                eventDic.Remove(goInstanceID);
            }
        }
        private static void TriggerEvent(UnityAction action)
        {
            action.Invoke();
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