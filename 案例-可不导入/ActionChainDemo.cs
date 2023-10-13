using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WManager;
using DG.Tweening;
using UnityEngine.Events;
using System;

///<summary>
///事件链案例
///</summary>
public class ActionChainDemo : MonoBehaviour
{
    public Button button;
    public GameObject Object;
    public Animator animator;
    public Animation animt;
    // IActionChain chain;

    public Button next;
    public Button Previous;
    // Start is called before the first frame update
    void Start()
    {
        // //编辑事件链：序列事件链
        // chain = ActionChain.Sequence()
        //     //普通事件
        //     .Event(() => Debug.Log("开始事件链"))
        //     //延迟2秒
        //     .Delay(2f)
        //     //普通事件
        //     .Event(() => Debug.Log("经过2秒"))
        //     .Event(() => Debug.Log("等待按下A键"))
        //     //直到按下键盘A键
        //     .Until(() => Input.GetKeyDown(KeyCode.A))
        //     //普通事件
        //     .Event(() => Debug.Log("按下A键"))
        //     //DoTween动画事件
        //     .Tween(() => transform.DOMove(new Vector3(0f, 1f, 0f), 2f))
        //     //按钮点击事件
        //     .Event(() => print("等待点击" + button.name))
        //     .Until(button.isClickBtn())
        //     .Event(() => print("点击了" + button.name))
        //     //物体点击事件
        //     .Event(() => print("等待点击" + Object.name))
        //     .Until(Object.isClickObj())
        //     .Event(() => print("点击了" + Object.name))
        //     //动画事件
        //     .Event(() => print("等待动画a"))
        //     .Animate(animator, "a")
        //     .Event(() => print("a播放完毕"))

        //     //嵌套一个并发事件链
        //     .Append(new ConcurrentActionChain()
        //         .Delay(1f, () => Debug.Log("1f"))
        //         .Delay(2f, () => Debug.Log("2f"))
        //         .Delay(3f, () => Debug.Log("3f"))
        //         as IAction)
        //     //并发事件链执行完成后 继续执行序列事件链

        //     //定时事件
        //     .Timer(3f, false, s => Debug.Log(s))
        //     .Event(() => print("等待动画q"))
        //     .Animation(animt, "q")
        //     .Event(() => print("q播放完毕"));

        // //执行事件链
        // chain.Begin()
        // .OnStop(() => Debug.Log("事件结束"));


        MethodManager.AddMethod(Begin1);
        MethodManager.AddMethod(Begin2);
        MethodManager.AddMethod(Begin3);
        next.onClick.AddListener(() =>
        {
            MethodManager.NextMethod();
        });
        Previous.onClick.AddListener(() =>
        {
            MethodManager.PreviousMethod();
        });
    }
    public void jump(int i)
    {
        MethodManager.JumpMethod(i);
    }
    private void Begin1()
    {
        //编辑事件链：序列事件链
        var chain = ActionChain.Sequence()
            //普通事件
            .Event(() => Debug.Log("开始事件链1"))
            //延迟2秒
            .Delay(2f)
            //普通事件
            .Event(() => Debug.Log("经过2秒"))
            .Event(() => Debug.Log("等待按下A键"))
            //直到按下键盘A键
            .Until(() => Input.GetKeyDown(KeyCode.A))
            //普通事件
            .Event(() => Debug.Log("按下A键"))
            .Event(() => Debug.Log("11111111"))
            ;
        MethodManager.currentChain = chain;
        MethodManager.currentChain.OnStop(() =>
        {
            Debug.Log("事件1结束");
        });
        MethodManager.currentChain.Begin();
    }
    private void Begin2()
    {
        //编辑事件链：序列事件链
        var chain = ActionChain.Sequence()
            //普通事件
            .Event(() => Debug.Log("开始事件链2"))
            //物体点击事件
            .Event(() => print("等待点击" + Object.name))
            .Until(Object.isClickObj())
            .Event(() => print("点击了" + Object.name))
            .Event(() => Debug.Log("4"))
            .Event(() => Debug.Log("5"))
            .Event(() => Debug.Log("6"))
            .Event(() => Debug.Log("2222222"))
            ;
        MethodManager.currentChain = chain;
        MethodManager.currentChain.OnStop(() =>
        {
            Debug.Log("事件2结束");
        });
        MethodManager.currentChain.Begin();
    }
    private void Begin3()
    {
        //编辑事件链：序列事件链
        var chain1 = ActionChain.Sequence()
            //普通事件
            .Event(() => Debug.Log("开始事件链1"))
            .Event(() => Debug.Log("等待按下S键"))
            //直到按下键盘A键
            .Until(() => Input.GetKeyDown(KeyCode.S))
            //普通事件
            .Event(() => Debug.Log("按下S键"))
            .Event(() => Debug.Log("666666666666"))
            ;
        MethodManager.currentChain = chain1;
        MethodManager.currentChain.OnStop(() =>
        {
            Debug.Log("事件3结束");
        });
        MethodManager.currentChain.Begin();
    }
}
