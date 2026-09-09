// ----------------------------------------------------------------------------
// EventTriggerExample.cs
//
// 演示 EventTriggerManager：
// - 自动给目标物体加 BoxCollider（如果还没有）+ EventTrigger
// - 统一管理每个 (物体, 事件类型) 的回调列表
// - 提供 RemoveEvent / RemoveAllEvent
// ----------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.EventSystems;
using WManager;

namespace WManager.Example
{
    public class EventTriggerExample : MonoBehaviour
    {
        [Header("鼠标进出点击物体")]
        [SerializeField] private GameObject interactableCube;
        [Header("拖拽物体")]
        [SerializeField] private GameObject chest;

        private void Awake()
        {
            //判断主相机是否有射线组件
            if (!Camera.main.GetComponent<PhysicsRaycaster>())
                Camera.main.gameObject.AddComponent<PhysicsRaycaster>();
        }

        private void OnEnable()
        {
            // 1) 点击物体触发
            EventTriggerManager.AddEvent(
                interactableCube,
                EventTriggerType.PointerClick,
                OnClickInteractable);

            // 2) 鼠标进入/离开触发（光标变色）
            EventTriggerManager.AddEvent(
                interactableCube,
                EventTriggerType.PointerEnter,
                OnPointerEnter);

            EventTriggerManager.AddEvent(
                interactableCube,
                EventTriggerType.PointerExit,
                OnPointerExit);

            // 3) 拖拽开始 / 拖拽中
            EventTriggerManager.AddEvent(
                chest,
                EventTriggerType.BeginDrag,
                OnBeginDrag);

            EventTriggerManager.AddEvent(
                chest,
                EventTriggerType.Drag,
                OnDrag);
        }

        private void OnDisable()
        {
            // 移除指定事件类型（必须传相同的方法引用）
            EventTriggerManager.RemoveEvent(interactableCube, EventTriggerType.PointerClick, OnClickInteractable);
            EventTriggerManager.RemoveEvent(interactableCube, EventTriggerType.PointerEnter, OnPointerEnter);
            EventTriggerManager.RemoveEvent(interactableCube, EventTriggerType.PointerExit, OnPointerExit);

            EventTriggerManager.RemoveEvent(chest, EventTriggerType.BeginDrag, OnBeginDrag);
            EventTriggerManager.RemoveEvent(chest, EventTriggerType.Drag, OnDrag);
        }

        // ============================================================================
        // 回调实现（接收 BaseEventData）
        // ============================================================================
        private void OnClickInteractable(BaseEventData data)
        {
            Debug.Log("[EventTrigger] 点击了 Cube");
        }

        private void OnPointerEnter(BaseEventData data)
        {
            interactableCube.GetComponent<Renderer>().material.color = Color.yellow;
        }

        private void OnPointerExit(BaseEventData data)
        {
            interactableCube.GetComponent<Renderer>().material.color = Color.white;
        }

        private void OnBeginDrag(BaseEventData data)
        {
            Debug.Log("[EventTrigger] 开始拖拽 Chest");
        }

        private void OnDrag(BaseEventData data)
        {
            // 注意 EventData 类型转换
            if (data is PointerEventData ped)
            {
                Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(ped.position.x, ped.position.y, 10f));
                chest.transform.position = world;
            }
        }

        // ============================================================================
        // 一次性清空某物体上的所有事件（慎用：会清掉其他脚本注册的 EventTrigger 条目）
        // ============================================================================
        public void ClearChestEvents()
        {
            EventTriggerManager.RemoveAllEvent(chest);
        }
    }
}
