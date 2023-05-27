using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;

///<summary>
///功能：接收到消息并执行对应事件
///</summary>
public class Event_接收事件 : MonoBehaviour
{
    private void Start()
    {
        EventManager.StartListening("你好", MyFunction);
        Debug.Log("开始监听");
    }

    void MyFunction()
    {
        var a = EventManager.GetInt("你好");
        Debug.Log("接收到数据:" + a);
        var b = (GameObject)EventManager.GetSender("你好");
        Debug.Log("发送数据者:" + b.name);
    }
}
