using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;
using System;

public class TimerDemo : MonoBehaviour
{
    //定时器
    private Countdown countdown;
    // Start is called before the first frame update
    void Start()
    {
        //将2小时转换为秒
        var a = TimeTool.Convert2Seconds(2, TimeUnit.Hour);
        print(a);
        //获取当前时间戳
        var b = TimeTool.GetTimeStamp(DateTime.Now);
        print(b);
        //将8923秒转化为HH:mm:ss格式字符串
        var c = TimeTool.ToStandardTimeFormat(8923);
        print(c);
        //将8923秒转化为mm:ss格式字符串
        var d = TimeTool.ToMSTimeFormat(8923);
        print(d);
        //将8923秒转化为HH:mm:ss:fff格式字符串
        var e = TimeTool.ToHMSFTimeFormat(8923);
        print(e);
        //将8923秒转化为mm:ss:fff格式字符串
        var f = TimeTool.ToMSFTimeFormat(8923);
        print(f);



        countdown = Timer.Countdown(5f)
            .OnLaunch(() => Debug.Log("定时器启动"))
            .OnExecute(s => Debug.Log(string.Format("剩余时间{0}", s)))
            .OnPause(() => Debug.Log("定时器暂停"))
            .OnResume(() => Debug.Log("定时器恢复"))
            .OnStop(() => Debug.Log("定时器停止"));
    }
    private void OnGUI()
    {
        if (GUILayout.Button("启动", GUILayout.Width(200f), GUILayout.Height(50f)))
        {
            countdown.Launch();
        }
        if (GUILayout.Button("暂停", GUILayout.Width(200f), GUILayout.Height(50f)))
        {
            countdown.Pause();
        }
        if (GUILayout.Button("恢复", GUILayout.Width(200f), GUILayout.Height(50f)))
        {
            countdown.Resume();
        }
        if (GUILayout.Button("终止", GUILayout.Width(200f), GUILayout.Height(50f)))
        {
            countdown.Stop();
        }
    }
}
