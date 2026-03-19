using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
/// <summary>
/// NOT USE
/// </summary>
//namespace LH
//{
//    public class GetJson : MonoBehaviour
//    {
//        [SerializeField]
//        string URL_String = "http://220.90.135.156:3000/";

//        public void GetClass(string type,System.Action<bool,string> ResultCallback)
//        {
//            StartCoroutine(Upload(URL_String+type, (result) =>
//            {
//                if (result != null)
//                {
//                    ResultCallback(true, result);
//                }
//                else
//                {
//                    ResultCallback(false, null);
//                }
//            }));
//        }

//        IEnumerator Upload(string URL,System.Action<string> callback)
//        {
//            using (UnityWebRequest request = UnityWebRequest.Get(URL))
//            {
//                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
//                request.SetRequestHeader("Content-Type", "application/json");
                

//                yield return request.SendWebRequest();

//                if (request.isNetworkError || request.isHttpError)
//                {
//                    Debug.Log(request.error);
//                    callback(null);
//                }
//                else
//                {
//                    Debug.Log(request.downloadHandler.text);
//                    callback(request.downloadHandler.text);
//                }
//            }
//        }
//    }
//}