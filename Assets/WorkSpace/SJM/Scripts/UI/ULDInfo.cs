using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ULDInfo
{
    public bool main;
    public int posIndex;
    public string uldIndex;

    // Stack View
    public string uldType;
    public List<CargoInfo> cargoInfos = new List<CargoInfo>();
    public List<string> cargos = new List<string>();
    public List<Vector3> cargoPos = new List<Vector3>();
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

    public Dictionary<string, List<float>> conStartTime = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> conEndTime = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> simStartTime = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> simEndTime = new Dictionary<string, List<float>>();

    public ULDInfo()
    {

    }

    public ULDInfo(bool isMain, int posIndex, string uldIndex, string uldType, string destination)
    {
        this.main = isMain;
        this.posIndex = posIndex;
        this.uldIndex = uldIndex;
        this.uldType = uldType;
        this.destination = destination;
    }
}


