using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace WManager
{
    public static class EventTriggerManager
    {
        // 存储每个GameObject上不同事件类型对应的EventTriggerType和UnityAction
        private static Dictionary<GameObject, Dictionary<EventTriggerType, List<UnityAction<BaseEventData>>>> eventDic = new Dictionary<GameObject, Dictionary<EventTriggerType, List<UnityAction<BaseEventData>>>>();
        // 存储每个GameObject上由本管理器添加的Collider类型
        private static Dictionary<GameObject, ColliderType> addedColliders = new Dictionary<GameObject, ColliderType>();
        
        private enum ColliderType
        {
            None,
            BoxCollider,
            MeshCollider
        }

        /// <summary>
        /// 添加事件（带EventData）
        /// </summary>
        /// <param name="go">游戏物体</param> 
        /// <param name="type">事件类型</param> 
        /// <param name="action">响应事件（带BaseEventData参数）</param>
        public static void AddEvent(GameObject go, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            go.isStatic = false;//将物体改为非静态
            
            if (go == null) return;
            if (action == null) return;
            
            // 如果字典中不存在这个GameObject,添加一个新的键值对
            if (!eventDic.ContainsKey(go))
            {
                eventDic.Add(go, new Dictionary<EventTriggerType, List<UnityAction<BaseEventData>>>());
                // 添加销毁监听
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<AutoCleanup>();
            }
            
            if (!eventDic[go].ContainsKey(type))
            {
                eventDic[go].Add(type, new List<UnityAction<BaseEventData>>());
            }

            // 给GameObject添加EventTrigger组件
            EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
            
            // 检查并添加Collider组件（仅当没有现有Collider时）
            if (!go.GetComponent<Collider>())
            {
                // 优先使用更轻量的BoxCollider
                BoxCollider boxCollider = go.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                addedColliders[go] = ColliderType.BoxCollider;
            }
            else
            {
                // 确保现有Collider是触发器
                Collider existingCollider = go.GetComponent<Collider>();
                existingCollider.isTrigger = true;
            }

            // 在EventTrigger组件上添加type类型的事件和响应action方法
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);

            // 将信息存入字典
            eventDic[go][type].Add(action);
        }
        
        /// <summary>
        /// 添加事件（不带EventData，兼容旧版本）
        /// </summary>
        /// <param name="go">游戏物体</param> 
        /// <param name="type">事件类型</param> 
        /// <param name="action">响应事件</param>
        public static void AddEvent(GameObject go, EventTriggerType type, UnityAction action)
        {
            if (action == null) return;
            AddEvent(go, type, (eventData) => { action(); });
        }

        /// <summary>
        /// 移除事件
        /// </summary> 
        /// <param name="go">游戏物体</param> 
        /// <param name="type">事件类型</param>
        public static void RemoveEvent(GameObject go, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            if (go == null) return;
            if (!eventDic.ContainsKey(go) || !eventDic[go].ContainsKey(type)) return;
            
            eventDic[go][type].Remove(action);

            // 找到并移除对应的EventTrigger.Entry
            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                // 找到所有匹配事件类型的Entry
                var entries = trigger.triggers.FindAll(e => e.eventID == type);
                
                // 移除包含该action的Entry
                foreach (var entry in entries)
                {
                    if (entry.callback.GetPersistentEventCount() > 0)
                    {
                        entry.callback.RemoveListener(action);
                        // 如果Entry的回调列表为空，则从triggers中移除
                        if (entry.callback.GetPersistentEventCount() == 0)
                        {
                            trigger.triggers.Remove(entry);
                        }
                    }
                }

                // 移除这个事件响应后,检查EventTrigger中是否还有其他事件
                if (trigger.triggers.Count == 0)
                {
                    GameObject.Destroy(trigger); // 删除EventTrigger组件
                    
                    // 删除由本管理器添加的Collider组件
                    if (addedColliders.ContainsKey(go))
                    {
                        Collider collider = go.GetComponent<Collider>();
                        if (collider != null)
                        {
                            GameObject.Destroy(collider);
                            addedColliders.Remove(go);
                        }
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
                // 移除销毁监听组件
                AutoCleanup cleanup = go.GetComponent<AutoCleanup>();
                if (cleanup != null)
                {
                    GameObject.Destroy(cleanup);
                }
            }
        }
        
        /// <summary>
        /// 移除事件（不带EventData，兼容旧版本）
        /// </summary> 
        /// <param name="go">游戏物体</param> 
        /// <param name="type">事件类型</param>
        public static void RemoveEvent(GameObject go, EventTriggerType type, UnityAction action)
        {
            if (go == null || action == null) return;
            if (!eventDic.ContainsKey(go) || !eventDic[go].ContainsKey(type)) return;
            
            // 查找对应的带EventData的action
            var actions = eventDic[go][type];
            foreach (var act in actions)
            {
                // 由于lambda表达式的特性，我们无法直接比较，所以需要遍历所有可能的action
                // 这里采用移除所有与该action相关的事件的方式
                RemoveEvent(go, type, act);
            }
        }
        
        /// <summary>
        /// 为这个物体移除所有事件
        /// </summary>
        public static void RemoveAllEvent(this GameObject go)
        {
            if (go == null) return;
            if (!eventDic.ContainsKey(go)) return;

            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                // 移除所有的事件响应
                trigger.triggers.Clear();

                // 删除EventTrigger组件
                GameObject.Destroy(trigger);
                
                // 删除由本管理器添加的Collider组件
                if (addedColliders.ContainsKey(go))
                {
                    Collider collider = go.GetComponent<Collider>();
                    if (collider != null)
                    {
                        GameObject.Destroy(collider);
                        addedColliders.Remove(go);
                    }
                }
            }
            
            // 移除销毁监听组件
            AutoCleanup cleanup = go.GetComponent<AutoCleanup>();
            if (cleanup != null)
            {
                GameObject.Destroy(cleanup);
            }
            
            // 从字典中移除这个GameObject
            eventDic.Remove(go);
        }
        
        /// <summary>
        /// 自动清理组件，用于监听GameObject销毁
        /// </summary>
        private class AutoCleanup : MonoBehaviour
        {
            private void OnDestroy()
            {
                // 清理字典中的引用
                if (eventDic.ContainsKey(gameObject))
                {
                    eventDic.Remove(gameObject);
                }
                if (addedColliders.ContainsKey(gameObject))
                {
                    addedColliders.Remove(gameObject);
                }
            }
        }
    }
    
    public static class CustomEventTrigger
    {
        /// <summary>
        /// 为这个物体添加事件（带EventData）
        /// </summary>
        /// <param name="type">事件类型</param>
        /// <param name="action">事件</param>
        public static void AddEvent(this GameObject go, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            EventTriggerManager.AddEvent(go, type, action);
        }
        
        /// <summary>
        /// 为这个物体添加事件（不带EventData）
        /// </summary>
        /// <param name="type">事件类型</param>
        /// <param name="action">事件</param>
        public static void AddEvent(this GameObject go, EventTriggerType type, UnityAction action)
        {
            EventTriggerManager.AddEvent(go, type, action);
        }
        
        /// <summary>
        /// 为这个物体移除事件（带EventData）
        /// </summary>
        /// <param name="type">事件类型</param>
        public static void RemoveEvent(this GameObject go, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            EventTriggerManager.RemoveEvent(go, type, action);
        }
        
        /// <summary>
        /// 为这个物体移除事件（不带EventData）
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
        /// 为这个物体添加鼠标点击事件（带EventData）
        /// </summary>
        public static void OnClickAddListener(this GameObject go, UnityAction<BaseEventData> action, EventTriggerType type = EventTriggerType.PointerClick)
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
        /// 为这个物体移除鼠标点击事件（带EventData）
        /// </summary>
        public static void OnClickRemoveListener(this GameObject go, UnityAction<BaseEventData> action, EventTriggerType type = EventTriggerType.PointerClick)
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
        /// 为这个物体添加鼠标进入事件（带EventData）
        /// </summary>
        public static void OnEnterAddListener(this GameObject go, UnityAction<BaseEventData> action, EventTriggerType type = EventTriggerType.PointerEnter)
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
        /// 为这个物体移除鼠标进入事件（带EventData）
        /// </summary>
        public static void OnEnterRemoveListener(this GameObject go, UnityAction<BaseEventData> action, EventTriggerType type = EventTriggerType.PointerEnter)
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
        /// 为这个物体添加鼠标退出事件（带EventData）
        /// </summary>
        public static void OnExitAddListener(this GameObject go, UnityAction<BaseEventData> action, EventTriggerType type = EventTriggerType.PointerExit)
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
        
        /// <summary>
        /// 为这个物体移除鼠标退出事件（带EventData）
        /// </summary>
        public static void OnExitRemoveListener(this GameObject go, UnityAction<BaseEventData> action, EventTriggerType type = EventTriggerType.PointerExit)
        {
            EventTriggerManager.RemoveEvent(go, type, action);
        }
    }
}