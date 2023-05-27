using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WManager;

///<summary>
///功能：
///</summary>
public class HttpExample : MonoBehaviour
{
    public InputField urlInput;
    public InputField Title;
    public InputField Body;
    public InputField userID;
    public Text resultText;

    private void Start()
    {
        //WebRequest.Instance.Get("https://jsonplaceholder.typicode.com/todos/1", OnGetComplete);
    }

    public void OnGetClick()
    {
        string url = "https://jsonplaceholder.typicode.com/todos/1";
        WebRequest.Instance.Get(url, OnGetComplete);
    }
    public class postData
    {
        public string title;
        public string body;
        public int userId;
    }
    public void OnPostClick()
    {
        string url = urlInput.text;
        postData d = new postData
        {
            title = Title.text,
            body = Body.text,
            userId = int.Parse(userID.text)
        };
        string data = JsonUtility.ToJson(d);

        //byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
        WebRequest.Instance.Post(url, data, "application/json;charset=UTF-8", OnPostComplete);
    }

    private void OnGetComplete(HttpCallBackArgs args)
    {
        if (args.HasError)
        {
            Debug.LogError(args.Value);
            resultText.text = "错误: " + args.Value;
        }
        else
        {
            Debug.Log(args.Value);
            resultText.text = args.Value;
        }
    }

    private void OnPostComplete(HttpCallBackArgs args)
    {
        if (args.HasError)
        {
            Debug.LogError(args.Value);
            resultText.text = "错误: " + args.Value;
        }
        else
        {
            Debug.Log(args.Value);
            resultText.text = args.Value;
        }
    }
}
