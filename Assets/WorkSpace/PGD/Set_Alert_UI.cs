using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetAlertUI : MonoBehaviour
{
    [SerializeField]
    TMP_Text Alert_Time;
    [SerializeField]
    TMP_Text Alert_Message;
    [SerializeField]
    TMP_Text Alert_EndTime;
    [SerializeField]
    TMP_Text Alert_Stop;
    [SerializeField]
    TMP_Text Alert_Count;

    public void SetData(Alert_Class AC)
    {
        Alert_Time.text = AC.createdAt;
        Alert_Message.text = AC.alarmMessage;
        Alert_EndTime.text = AC.updatedAt;
        Alert_Stop.text = AC.stopTime;
        Alert_Count.text = AC.count.ToString();
    }
}
