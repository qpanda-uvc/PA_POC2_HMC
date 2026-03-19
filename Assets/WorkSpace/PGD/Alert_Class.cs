using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Alert_Class
{
    public int id;
    public string equipmentName;
#nullable enable
    public string? responseTime;
    public string? stopTime;
    public int? count;
    public string? deletedAt;
#nullable disable
    public string alarmMessage;
    public string createdAt;
    public string updatedAt;
    
}

[System.Serializable]
public class Alert
{
    public int statusCode;
    public string message;
    public List<Alert_Class> data;
}
