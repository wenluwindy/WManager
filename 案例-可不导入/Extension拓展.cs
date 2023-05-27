using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using WManager;

public class Extension拓展 : MonoBehaviour
{
    void Start()
    {
        string[] exampleArray = new string[] { "AAA", "BBB" };
        //遍历
        exampleArray.ForEach((i, s) => Debug.Log(string.Format("{0}.{1}", i + 1, s)));
        //倒序遍历
        exampleArray.ForEachReverse(m => Debug.Log(m));
        //倒序遍历
        exampleArray.ForEachReverse((i, s) => Debug.Log(string.Format("{0}.{1}", i + 1, s)));
        //Array合并
        string[] target = new string[] { "CCC", "DDD" };
        string[] merge = exampleArray.Merge(target);
        merge.ForEach((i, s) => Debug.Log(string.Format("{0}.{1}", i + 1, s)));
 
        int[] intArray = new int[] { 55, 32, 57, 89, 13, 87 , 9, 21};
        // //希尔排序
        // intArray.SortInsertion();
        // //选择排序
        // intArray.SortSelection();
        // //冒泡排序
        // intArray.SortBubble();
        // //插入排序
        // intArray.SortInsertion();
        // intArray.ForEach((i, s) => Debug.Log(string.Format("{0}.{1}", i + 1, s)));

        // bool flag = false;
        // //如果flag为true 则会打印日志true
        // flag.Execute(() => Debug.Log("true"));
        // //如果flag为true 则会打印日志true 否则打印日志false
        // flag.Execute(isTrue => Debug.Log(isTrue));
        // //如果flag为true 则会打印日志true 否则打印日志false
        // flag.Execute(() => Debug.Log("true"), () => Debug.Log("false"));

        // Dictionary<int, string> dic = new Dictionary<int, string>() { { 5, "AAA" }, { 10, "BBB" } };
        // //遍历字典
        // dic.ForEach(m => Debug.Log(string.Format("Key{0} Value{1}", m.Key, m.Value)));
        // Dictionary<int, string> target = new Dictionary<int, string>() { { 11, "CCC" }, { 20, "DDD" } };
        // //合并字典
        // dic.AddRange(target);
        // //将字典的所有值放入到一个列表中
        // List<string> list = dic.Values.ToList();
        // //将字典的所有值放入到一个Array中
        // string[] array = dic.Values.ToArray();
        // //尝试添加 
        // if (dic.TryAdd(20, "DDD")) Debug.Log("添加成功");

        // Queue<string> queue = new Queue<string>();
        // queue.Enqueue("A");
        // queue.Enqueue("b");
        // queue.Enqueue("e");
        // queue.Enqueue("g");
        // //遍历队列
        // queue.ForEach(m => Debug.Log(m));
    }
}
