//using M2MqttUnity;
//using System.Collections;
//using System.Collections.Generic;
//using System.Xml.XPath;
//using UnityEngine;
//using UnityEngine.UI;
//using Newtonsoft.Json;
//using TMPro;
//using System;
//using UnityEngine.SubsystemsImplementation;

//namespace LH
//{
//    public class Alert_UI : MonoBehaviour
//    {
//        GetJson GJ;
//        MQTT_Connect MC;
//        [SerializeField]
//        Image Alert_Image;
//        [SerializeField]
//        GameObject Alert_prefab;
//        [SerializeField]
//        RectTransform Log_Canvas;
//        [SerializeField]
//        TMP_Dropdown Year;
//        [SerializeField]
//        TMP_Dropdown Month;

//        List<GameObject> Alert_List = new List<GameObject>();

//        string createAtTo = "20230831";
//        string createAtFrom = "20230801";

//        public Alert AC = new Alert();
//        Start is called before the first frame update
//        void Start()
//        {
//            var mm = Managers_Instance.Instance;
//            GJ = mm.GetComponent<GetJson>();
//            MC = mm.GetComponent<MQTT_Connect>();
//            if (MC != null)
//            {
//                MC.OnTopic4Received += GetAlert;
//            }
//        }

//        void GetAlert()
//        {
//            Alert_Image.color = Color.yellow;
//        }

//        public void CheckAlert()
//        {
//            Alert_Image.color = Color.white;
//        }

//        public void GetAlertLog()
//        {
//            GJ.GetClass($"alarm?createdAtTo={createAtTo}&createdAtFrom={createAtFrom}", (result, text) =>
//            {
//                if (result)
//                {
//                    AC = JsonConvert.DeserializeObject<Alert>(text);
//                    CreateAlertLog(AC);
//                }
//                else
//                {
//                    Debug.Log("알람 데이터를 정상적으로 받아오지 못하였습니다. 다시 시도해주세요");
//                }
//            });
//        }

//        public void SetAlertDate()
//        {
//            string selectedYear = Year.options[Year.value].text;
//            string SelectedMonth = Month.options[Month.value].text;
//            int lastday = DateTime.DaysInMonth(Convert.ToInt32(selectedYear), Convert.ToInt32(SelectedMonth));
//            string LastDayofMonth = lastday.ToString();
//            createAtFrom = selectedYear + SelectedMonth + "01";
//            createAtTo = selectedYear + SelectedMonth + LastDayofMonth;
//            GetAlertLog();
//        }

//        public void CreateAlertLog(Alert AC)
//        {
//            foreach (var a in Alert_List)
//            {
//                Destroy(a);
//            }
//            int i = 1;
//foreach (var A in AC.data)
//{
//    Log_Canvas.sizeDelta = new Vector2(Log_Canvas.sizeDelta.x, AC.data.Count * 120f + 120);
//    GameObject NewLog = Instantiate(Alert_prefab, Log_Canvas);
//    NewLog.GetComponent<Set_Alert_UI>().Alert_Set_UI(A);
//    NewLog.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -i * 120 - 60);
//    i++;
//    Alert_List.Add(NewLog);
//}
//        }

//    }
//}