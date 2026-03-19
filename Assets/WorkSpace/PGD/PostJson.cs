using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
/// <summary>
/// Not USE
/// </summary>

//namespace LH
//{

//    //서버에 데이터를 올리기 위해 Post방식을 사용합니다.
//    //URL_String의 주소를 기반으로, type은 올리는 데이터의 종류에 따라 ULD, Cargo등이 입력됩니다.
//    public class PostJson : MonoBehaviour
//    {
//        string json = "";
//        [SerializeField]
//        string URL_String = "http://220.90.135.109:3000/";

//        public void UploadClass(object inputclass,string type,System.Action<bool, string> ResultCallback)
//        {
//            json = JsonConvert.SerializeObject(inputclass);
//            StartCoroutine(Upload(URL_String + type, json, (result) =>
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

//        IEnumerator Upload(string URL, string json, System.Action<string> callback)
//        {
//            using (UnityWebRequest request = UnityWebRequest.Post(URL, json))
//            {
//                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

//                request.uploadHandler.Dispose();
//                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
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