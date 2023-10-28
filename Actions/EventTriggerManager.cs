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
        public class ActionBase
        {
            public List<UnityAction> actions = new();
            public List<UnityAction<BaseEventData>> callbacks = new();
        }
        public class EventRelatedGroup
        {
            public GameObject go;
            public Dictionary<EventTriggerType, ActionBase> types = new();
        }
        //用一个字典组来存储每个GameObject上不同事件类型对应的EventTriggerType和UnityAction
        private static Dictionary<int, EventRelatedGroup> eventDic = new();

        /// <summary>
        /// 添加事件
        /// </summary>
        /// <param name="gObj">要添加事件的物体</param>
        /// <param name="type">事件类型</param>
        /// <param name="action">事件</param>
        public static void AddEvent(GameObject gObj, EventTriggerType type, UnityAction action)
        {
            // 检查参数合法性
            if (gObj == null) return;
            //将物体改为非静态
            gObj.isStatic = false;
            //生成唯一键值
            int key = gObj.GetInstanceID();
            // 如果字典中已有该GameObject
            if (eventDic.ContainsKey(key))
            {
                // 如果存在这个类型
                if (eventDic[key].types.ContainsKey(type))
                {
                    //如果存在这个方法
                    if (eventDic[key].types[type].actions.Contains(action))
                    {
                        return;
                    }
                    else
                    {
                        //添加方法
                        eventDic[key].types[type].actions.Add(action);
                        //添加回调
                        void ABD(BaseEventData eventData)
                        {
                            TriggerEvent(action);
                        }
                        eventDic[key].types[type].callbacks.Add(ABD);

                        //为这个物体上的EventTrigger组件的这个类型添加事件
                        var trigger = gObj.GetComponent<EventTrigger>();
                        EventTrigger.Entry foundEntry = trigger.triggers.Find(x => x.eventID == type);
                        foundEntry.callback.AddListener(ABD);
                    }
                }
                else
                {
                    //创建类型
                    ActionBase actionBase = new ActionBase();
                    actionBase.actions.Add(action);
                    void ABD(BaseEventData eventData)
                    {
                        TriggerEvent(action);
                    }
                    actionBase.callbacks.Add(ABD);
                    //添加到字典
                    eventDic[key].types.Add(type, actionBase);

                    //为这个物体上的EventTrigger组件添加类型及事件
                    var trigger = gObj.GetComponent<EventTrigger>();
                    EventTrigger.Entry entry = new()
                    {
                        eventID = type
                    };
                    entry.callback.AddListener(ABD);
                    trigger.triggers.Add(entry);
                }
            }
            else
            {
                //创建ActionBase
                ActionBase actionBase = new();
                actionBase.actions.Add(action);
                void ABD(BaseEventData eventData) => TriggerEvent(action);
                actionBase.callbacks.Add(ABD);
                //创建EventRelatedGroup
                EventRelatedGroup eventRelatedGroup = new()
                {
                    go = gObj,
                };
                eventRelatedGroup.types.Add(type, actionBase);
                //加入字典
                eventDic.Add(key, eventRelatedGroup);
                //在这个物体上添加EventTrigger组件和Collider组件
                var trigger = gObj.GetComponent<EventTrigger>() ?? gObj.AddComponent<EventTrigger>();
                var collider = gObj.GetComponent<Collider>() ?? gObj.AddComponent<Collider>();
                //添加事件
                EventTrigger.Entry entry = new()
                {
                    eventID = type
                };
                entry.callback.AddListener(ABD);
                trigger.triggers.Add(entry);
            }
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="go">要移除的物体</param>
        /// <param name="type">事件类型</param>
        /// <param name="action">要移除的事件</param>
        public static void RemoveEvent(GameObject go, EventTriggerType type, UnityAction action)
        {
            // 检查参数合法性
            if (go == null) return;
            // 获取GameObject的实例ID
            int key = go.GetInstanceID();
            // 如果字典中存在该GameObject
            if (eventDic.ContainsKey(key))
            {
                // 获取对应事件响应组
                // 如果该组包含要移除的事件类型
                if (eventDic[key].types.ContainsKey(type))
                {
                    // 获取该类型的响应集合
                    // 如果响应集合中存在要移除的响应方法
                    if (eventDic[key].types[type].actions.Contains(action))
                    {
                        int index = eventDic[key].types[type].actions.IndexOf(action);
                        //移除物体上的EventTrigger组件中的指定方法
                        var trigger = go.GetComponent<EventTrigger>();
                        // 找到对应事件类型的Entry
                        EventTrigger.Entry foundEntry = trigger.triggers.Find(x => x.eventID == type);
                        foundEntry.callback.RemoveListener(eventDic[key].types[type].callbacks[index]);

                        // 移除响应方法
                        eventDic[key].types[type].actions.RemoveAt(index);
                        // 移除对应的回调方法
                        eventDic[key].types[type].callbacks.RemoveAt(index);

                        // 如果响应集合为空,则移除该事件类型
                        if (eventDic[key].types[type].actions.Count == 0)
                        {
                            eventDic[key].types.Remove(type);
                        }
                    }
                }

                // 如果事件响应组没有任何事件类型,则从字典中移除该GameObject
                if (eventDic[key].types.Count == 0)
                {
                    eventDic.Remove(key);
                }
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
        /// 为这个物体移除鼠标点击事件
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
        /// 为这个物体移除鼠标进入事件
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
        /// 为这个物体移除鼠标退出事件
        /// </summary>
        public static void OnExitRemoveListener(this GameObject go, UnityAction action, EventTriggerType type = EventTriggerType.PointerExit)
        {
            EventTriggerManager.RemoveEvent(go, type, action);
        }
    }
}