using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class Flight_btn : MonoBehaviour
{
    public TMP_Text flightName_txt;
    public TMP_Text flightType_txt;
    public List<TMP_Text> Text_airplaneRallypoint;

    public int myIndexNum;

    public void ShowThis(FlightInfo flightInfo)
    {
        flightName_txt.text = flightInfo.flightName;
        flightType_txt.text = flightInfo.flightType;
        for (int i = 0; i < flightInfo.airplaneRallyPoint.Length; i++)
        {
            Text_airplaneRallypoint[i].text = flightInfo.airplaneRallyPoint[i];
        }
    }

    
}
