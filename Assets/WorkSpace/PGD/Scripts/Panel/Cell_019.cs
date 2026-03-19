using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Cell_019 : MonoBehaviour
{
    [SerializeField] private TMP_Text text_occurrenceTime;
    [SerializeField] private TMP_Text text_Message;
    [SerializeField] private TMP_Text text_actionTime;
    [SerializeField] private TMP_Text text_breakTime;
    [SerializeField] private TMP_Text text_count;


    public void SetData(Alert_Class AC)
    {
        text_occurrenceTime.text = AC.createdAt;
        text_Message.text = AC.alarmMessage;
        text_actionTime.text = AC.updatedAt;
        text_breakTime.text = AC.stopTime;
        text_count.text = AC.count.ToString();
    }
}
