using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveCheck : MonoBehaviour
{
    FlightManager flightManager;

    public bool isConnectedMode;

    public float time;
    public float uldWorkTime;


    public void Initialize()
    {
        flightManager = FindObjectOfType<FlightManager>();
    }

    private void Update()
    {
        time += Time.deltaTime;
        uldWorkTime += Time.deltaTime;
    }

    public void TimeInitialize()
    {
        time = 0;
        uldWorkTime = 0;
    }

    public void ULDWorkEnd()
    {
        flightManager.currentWorkULDInfo.workTime = uldWorkTime;
    }

    public void NoticeWorkTime(string objectName, bool isActiveStart)
    {
        ULDInfo currentULDInfo = flightManager.currentWorkULDInfo;

        if (!currentULDInfo.timeTableList_Name.Contains(objectName)) 
        {
            currentULDInfo.timeTableList_Name.Add(objectName);
        }

        if (isConnectedMode)
        {
            if (isActiveStart)
            {
                currentULDInfo.conStartTime[objectName].Add(time);
            }
            else
            {
                currentULDInfo.conEndTime[objectName].Add(time);
            }
        }
        else
        {
            if (isActiveStart)
            {
                currentULDInfo.simStartTime[objectName].Add(time);
            }
            else
            {
                currentULDInfo.simEndTime[objectName].Add(time);
            }
        }
        flightManager.currentWorkFlightInfo.uldInfos[flightManager.currentWorkULDInfo.uldIndex] = flightManager.currentWorkULDInfo;
        flightManager.GenerateFlightInfoJson(flightManager.currentWorkFlightInfo);
    }

}
