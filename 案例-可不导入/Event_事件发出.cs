using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;

///<summary>
///功能：发出消息
///</summary>
public class Event_事件发出 : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            //发出的消息事件，后面的对象为object，可以为int、bool等变量、GameObject对象、组件等
            EventManager.SetData("你好", 45);
            EventManager.EmitEvent("你好", gameObject);
            Debug.Log("发送信息");
        }
    }
}
