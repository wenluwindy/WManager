using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// 事件链系统
    /// 1.Sequence:依次执行的事件,只有上一个事件执行结束后,才会开始执行下一个事件
    /// 2.Concurrent:并发执行的事件,在事件链启动时同时开启执行,在所有的事件都执行完成后,事件链终止
    /// 3.Timeline:时间轴事件链
    /// </summary>
    public static class ActionChain
    {
        public static void Execute(IAction action, MonoBehaviour executer)
        {
            executer.StartCoroutine(ExecuteCoroutine(action));
        }
        private static IEnumerator ExecuteCoroutine(IAction self)
        {
            while (!self.Invoke())
            {
                yield return null;
            }
        }
        /// <summary>
        /// 序列事件链
        /// </summary>
        /// <returns></returns>
        public static IActionChain Sequence()
        {
            return new SequenceActionChain(ActionMaster.Instance);
        }
        /// <summary>
        /// 序列事件链
        /// </summary>
        /// <param name="executer"></param>
        /// <returns></returns>
        public static IActionChain Sequence(this MonoBehaviour executer)
        {
            return new SequenceActionChain(executer);
        }
        /// <summary>
        /// 并发事件链
        /// </summary>
        /// <returns></returns>
        public static IActionChain Concurrent()
        {
            return new ConcurrentActionChain(ActionMaster.Instance);
        }
        /// <summary>
        /// 并发事件链
        /// </summary>
        /// <param name="executer"></param>
        /// <returns></returns>
        public static IActionChain Concurrent(this MonoBehaviour executer)
        {
            return new ConcurrentActionChain(executer);
        }
        /// <summary>
        /// 时间轴事件链
        /// </summary>
        /// <returns></returns>
        public static IActionChain Timeline()
        {
            return new TimelineActionChain(ActionMaster.Instance);
        }
        /// <summary>
        /// 时间轴事件链
        /// </summary>
        /// <param name="executer"></param>
        /// <returns></returns>
        public static IActionChain Timeline(this MonoBehaviour executer)
        {
            return new TimelineActionChain(executer);
        }
        /// <summary>
        /// 普通事件
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IActionChain Event(this IActionChain chain, UnityAction action)
        {
            return chain.Append(new SimpleAction(action));
        }
        /// <summary>
        /// 普通事件
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static IActionChain Events(this IActionChain chain, params UnityAction[] actions)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                chain.Append(new SimpleAction(actions[i]));
            }
            return chain;
        }
        /// <summary>
        /// 延迟事件
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="duration">时间秒</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IActionChain Delay(this IActionChain chain, float duration, UnityAction action = null)
        {
            return chain.Append(new DelayAction(duration, action));
        }
        /// <summary>
        /// 定时事件
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="duration">定时时长秒</param>
        /// <param name="isReverse">true为倒计时,false为正计时</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IActionChain Timer(this IActionChain chain, float duration, bool isReverse, UnityAction<float> action)
        {
            return chain.Append(new TimerAction(duration, isReverse, action));
        }
        /// <summary>
        /// 条件事件:条件成立时，调用回调函数，事件结束。
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="predicate">条件</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IActionChain Until(this IActionChain chain, Func<bool> predicate, UnityAction action = null)
        {
            return chain.Append(new UntilAction(predicate, action));
        }
        /// <summary>
        /// 条件事件:判断是否点击了按钮
        /// 例：Until(button.isClickBtn())
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="untilButtonClickAction"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IActionChain Until(this IActionChain chain, UntilButtonClickAction untilButtonClickAction, UnityAction action = null)
        {
            return chain.Append(untilButtonClickAction);
        }
        public static IActionChain Until(this IActionChain chain, UntilGameObjectClickAction untilGameObjectClickAction, UnityAction action = null)
        {
            return chain.Append(untilGameObjectClickAction);
        }
        /// <summary>
        /// 条件事件:回调函数在条件成立时一直被调用，当条件不再成立时，事件结束。
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="predicate">条件</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IActionChain While(this IActionChain chain, Func<bool> predicate, UnityAction action = null)
        {
            return chain.Append(new WhileAction(predicate, action));
        }
        /// <summary>
        /// 动画事件：需要指定Animator组件和Animator Controller中动画状态State的名称，动画播放完后，事件结束。
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="animator">Animator组件</param>
        /// <param name="stateName">状态名称</param>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        public static IActionChain Animate(this IActionChain chain, Animator animator, string stateName, int layerIndex = 0)
        {
            return chain.Append(new AnimateAction(animator, stateName, layerIndex));
        }
        public static IActionChain Animation(this IActionChain chain, Animation animation, string stateName)
        {
            return chain.Append(new AnimationAction(animation, stateName));
        }
        /// <summary>
        /// 追加事件：事件链之间互相嵌套
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="beginTime"></param>
        /// <param name="duration"></param>
        /// <param name="playAction"></param>
        /// <returns></returns>
        public static IActionChain Append(this IActionChain chain, float beginTime, float duration, UnityAction<float> playAction)
        {
            return chain.Append(new TimelineAction(beginTime, duration, playAction));
        }
    }
}