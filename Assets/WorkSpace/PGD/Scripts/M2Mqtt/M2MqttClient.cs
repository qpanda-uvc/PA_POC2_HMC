using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

// Mqtt 데이터 가공
public class M2MqttClient : MonoBehaviour
{
    //public RobotInfoUI robotInfoUI;

    //public URRobot urRobot;

    //public List<MQTTInfo> robotInfoList = new List<MQTTInfo>();

    //public void ArrangeData(string msg, string topic)
    //{
    //    string msgJson = "{\"data\":" + msg + "}";
    //    print("msgJson" + msgJson);
    //    robotInfoList.Clear();

    //    var info = FromJson<MQTTInfo>(msgJson);

    //    foreach (var data in info)
    //    {
    //        robotInfoList.Add(data);
    //    }

    //    urRobot.Rotate(robotInfoList);
    //    robotInfoUI.SetCurrentRobotRealtimeInfo(robotInfoList);

        //if (topic.Equals("test"))
        //{
        //    foreach (var data in info)
        //    {
        //        robotInfoList.Add(data);
        //    }

        //    urRobot.Rotate(robotInfoList);
        //    //doosanRobot.Move(robotInfoList);
        //    //robotInfoUI.SetCurrentRobotRealtimeInfo(robotInfoList);
        //    //doosanRobot.GetComponent<RobotPopupUI>().SetJointInfoAndTCP(doosanRobot.angles, robotInfoList);
        //}
        //else
        //{
        //    Debug.LogError("topic error");
        //}
  //  }

    //public static T[] FromJson<T>(string json)
    //{
    //    Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
    //    return wrapper.data;
    //}

    //[System.Serializable]
    //private class Wrapper<T>
    //{
    //    public T[] data;
    //}
}
