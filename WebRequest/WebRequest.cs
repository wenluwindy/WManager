using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace WManager
{
    /// <summary>
    /// Http访问器
    /// </summary> 
    public class WebRequest : MonoBehaviour
    {
        private static WebRequest instance;
        public static WebRequest Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (WebRequest)FindObjectOfType(typeof(WebRequest));
                    if (instance == null)
                    {
                        // 创建gameObject并添加组件
                        instance = new GameObject("WebRequest").AddComponent<WebRequest>();
                    }
                }
                return instance;
            }
        }
        private static bool initialized = false;
        static WebRequest()
        {
            Instance.Init();
        }
        private void Init()
        {
            if (!initialized)
            {
                initialized = true;
                HttpRetry = 5;
                HttpRetryInterval = 0.5f;
                DontDestroyOnLoad(this);
            }
        }

        #region 属性
        /// <summary>
        /// Http请求回调
        /// </summary>
        private HttpSendDataCallBack m_CallBack;

        /// <summary>
        /// Http请求回调数据
        /// </summary>
        private HttpCallBackArgs m_CallBackArgs;

        /// <summary>
        /// 是否繁忙
        /// </summary>
        public bool IsBusy { get; private set; }

        /// <summary>
        /// 当前重试次数
        /// </summary>
        private int m_CurrRetry = 0;

        private string m_Url;
        /// <summary>
        /// Http调用失败后重试次数
        /// </summary>
        public static int HttpRetry { get; private set; }
        /// <summary>
        /// Http调用失败后重试间隔（秒）
        /// </summary>
        public static float HttpRetryInterval { get; private set; }
        // 定义一个静态字符串变量来保存Post请求的Content-Type
        public static string PostContentType = "application/json";

        /// <summary>
        /// 发送的数据
        /// </summary>
        private byte[] m_Data;
        private string m_ContentType;
        private UnityWebRequest m_Request;
        #endregion

        public WebRequest()
        {
            m_CallBackArgs = new HttpCallBackArgs();
        }

        #region SendData 发送web数据
        /// <summary>
        /// 发送web数据
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callBack"></param>
        public void Get(string url, HttpSendDataCallBack callBack = null)
        {
            if (IsBusy) return;
            IsBusy = true;

            m_Url = url;
            m_CallBack = callBack;

            GetUrl(m_Url);
        }
        /// <summary>
        /// 发送Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data">byte数组</param>
        /// <param name="contentType"></param>
        /// <param name="callBack"></param>
        public void Post(string url, byte[] data = null, string contentType = null, HttpSendDataCallBack callBack = null)
        {
            if (IsBusy) return;
            IsBusy = true;

            m_Url = url;
            m_CallBack = callBack;
            m_Data = data;
            m_ContentType = contentType;

            PostUrl(m_Url);
        }
        /// <summary>
        /// 发送Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="json">json值</param>
        /// <param name="contentType"></param>
        /// <param name="callBack"></param>
        public void Post(string url, string json = null, string contentType = null, HttpSendDataCallBack callBack = null)
        {
            if (IsBusy) return;
            IsBusy = true;

            m_Url = url;
            m_CallBack = callBack;
            m_Data = System.Text.Encoding.UTF8.GetBytes(json); ;
            m_ContentType = contentType;

            PostUrl(m_Url);
        }

        public void Cancel()
        {
            if (m_Request != null)
            {
                m_Request.Abort();
                m_Request.Dispose();
                m_Request = null;
            }
            IsBusy = false;
        }
        #endregion

        #region GetUrl Get请求
        /// <summary>
        /// Get请求
        /// </summary>
        /// <param name="url"></param>
        private void GetUrl(string url)
        {
            Debug.Log($"WebRequest:<color=aqua>Get请求>></color>\n内容:{m_Url}\n重试次数:{m_CurrRetry}\n");
            UnityWebRequest data = UnityWebRequest.Get(url);
            m_Request = data;
            Instance.StartCoroutine(Request(data));
        }
        #endregion

        #region PostUrl Post请求
        /// <summary>
        /// Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        private void PostUrl(string url)
        {
            UnityWebRequest unityWeb = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            unityWeb.downloadHandler = new DownloadHandlerBuffer();
            if (m_Data != null)
            {
                if (m_CurrRetry == 0 && m_ContentType == "application/json")
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic["value"] = Encoding.UTF8.GetString(m_Data);
                    m_Data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(dic));
                }
                unityWeb.uploadHandler = new UploadHandlerRaw(m_Data);

                if (!string.IsNullOrWhiteSpace(m_ContentType))
                    unityWeb.SetRequestHeader("Content-Type", m_ContentType);
            }

            Debug.Log($"WebRequest:<color=aqua>Post请求>></color>\n地址:{m_Url}\n重试次数:{m_CurrRetry}\n内容:{Encoding.UTF8.GetString(m_Data)}\n");
            m_Request = unityWeb;
            Instance.StartCoroutine(Request(unityWeb));
        }
        #endregion

        #region Request 请求服务器
        /// <summary>
        /// 请求服务器
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private IEnumerator Request(UnityWebRequest data)
        {
            data.timeout = 5;
            yield return data.SendWebRequest();
            if (data.result == UnityWebRequest.Result.Success)
            {
                IsBusy = false;
                m_CallBackArgs.HasError = false;
                m_CallBackArgs.Value = data.downloadHandler.text;
                m_CallBackArgs.Data = data.downloadHandler.data;
            }
            else
            {
                //报错了 进行重试
                if (m_CurrRetry > 0) yield return new WaitForSeconds(HttpRetryInterval);
                m_CurrRetry++;
                if (m_CurrRetry <= HttpRetry)
                {
                    switch (data.method)
                    {
                        case UnityWebRequest.kHttpVerbGET:
                            GetUrl(m_Url);
                            break;
                        case UnityWebRequest.kHttpVerbPOST:
                            PostUrl(m_Url);
                            break;
                    }
                    yield break;
                }

                IsBusy = false;
                m_CallBackArgs.HasError = true;
                m_CallBackArgs.Value = data.error;
            }

            if (!string.IsNullOrWhiteSpace(m_CallBackArgs.Value)) Debug.Log($"WebRequest:<color=aqua>WebAPI回调>></color>\n地址:{m_Url}\nHttp请求回调数据:{JsonUtility.ToJson(m_CallBackArgs)}\n");
            m_CallBack?.Invoke(m_CallBackArgs);

            m_CurrRetry = 0;
            m_Url = null;
            m_Data = null;
            m_ContentType = null;
            m_CallBackArgs.Data = null;
            if (data != null)
            {
                data.Dispose();
                data = null;
            }
            m_Request = null;
        }
        #endregion
    }
}