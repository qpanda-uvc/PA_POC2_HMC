using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightInfo
{
    public string flightName;
    public string flightType;
    public string[] airplaneRallyPoint;

    public Dictionary<string, ULDInfo> uldInfos = new Dictionary<string, ULDInfo>();

    public FlightInfo()
    {

    }

    public FlightInfo(string flightName, string flightType)
    {
        this.flightName = flightName;
        this.flightType = flightType;
    }
}
