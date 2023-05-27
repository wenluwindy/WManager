using System;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 条件事件（直到...）
    /// </summary>
    public class UntilAction : AbstractAction
    {
        private readonly Func<bool> predicate;

        public UntilAction(Func<bool> predicate, UnityAction action)
        {
            this.predicate = predicate;
            onCompleted = action;
        }

        protected override void OnInvoke()
        {
            isCompleted = predicate.Invoke();
        }
    }
    public class UntilButtonClickAction : AbstractAction
    {
        private bool isClick = false;
        bool start = false;
        public UntilButtonClickAction(Button button, UnityAction action)
        {
            button.onClick.AddListener(OnButtonClick);
            onCompleted = () =>
            {
                button.onClick.RemoveListener(OnButtonClick);
                action?.Invoke();
            };
        }
        protected override void OnInvoke()
        {
            isCompleted = isClick;
            start = true;
        }
        protected override void OnReset()
        {
            base.OnReset();
            isClick = false;
        }
        private void OnButtonClick()
        {
            if (start)
            {
                isClick = true;
            }
        }
    }
    public class UntilGameObjectClickAction : AbstractAction
    {
        private bool isClick;
        bool start = false;
        public UntilGameObjectClickAction(GameObject gameObject, UnityAction action)
        {
            gameObject.OnClickAddListener(OnButtonClick);
            onCompleted = () =>
            {
                gameObject.OnClickRemoveListener(OnButtonClick);
                action?.Invoke();
            };
        }
        protected override void OnInvoke()
        {
            isCompleted = isClick;
            start = true;
        }
        protected override void OnReset()
        {
            base.OnReset();
            isClick = false;
        }
        private void OnButtonClick()
        {
            if (start)
            {
                isClick = true;
            }
        }
    }
    public static class UnityActionExTension
    {
        public static UntilButtonClickAction isClickBtn(this Button button, UnityAction action = null)
        {
            return new UntilButtonClickAction(button, action);
        }
        public static UntilGameObjectClickAction isClickObj(this GameObject gameObject, UnityAction action = null)
        {
            return new UntilGameObjectClickAction(gameObject, action);
        }
    }
}