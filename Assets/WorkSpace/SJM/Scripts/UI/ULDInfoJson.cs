using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ULDInfoJson
{
    public bool main;
    public int posIndex;
    public string uldIndex;

    // Stack View
    public string uldType;
    public List<string> cargoInfos = new List<string>();
    public List<string> cargos = new List<string>();
    public List<string> cargoPos = new List<string>();
    public string id;
    public float wVolume;
    public float sVolume;
    public float workTime;
    public List<string> scc = new List<string>();

    // Load Table
    public float volume;
    public float weight;
    public string destination;
    public int workStage;

    // Time Table
    public List<string> timeTableList_Name = new List<string>();

    public List<ObjectWorkTimeRecord> objectWorkTimeRecords = new List<ObjectWorkTimeRecord>();
}

public class ObjectWorkTimeRecord
{
    public string objectName;
    public List<float> conStartTime = new List<float>();
    public List<float> conEndTime = new List<float>();
    public List<float> simStartTime = new List<float>();
    public List<float> simEndTime = new List<float>();
}


