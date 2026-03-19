using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AMRFakeSignal
{
    [SerializeField]
    public string AMR_ID;
    public string LogDT;
    public string PrcsCD;
    public string ACSMode;
    public string Mode;
    [SerializeField]
    public string X;
    [SerializeField]
    public string Y;
    [SerializeField]
    public string H;
    public string Speed;
    public string CurrentNode;
    public string StartNode;
    public string TargetNode;
    public string Connected;
    public string ErrorLevel;
    public string ErrorCode;
    public string ErrorInfo;
    public string CurState;
    public string PauseState;
    public string Loaded;
    public string MDir;
    public string TurnTableStatus;
    public string SOC;
    public string SOH;
    public string PLTNo;
    public string PLTType;
    public string TransNo;
    public string OrderNo;
    public string PartInfo;
    public string Paths;
    public string GroupNo;
    public string MissionNo;
    public string JobID;
    public string ActionID;
    public string Prog;
    public string DestTime;
    public string CreationTime;
    public string StartTime;
    public string EndTime;
    public string TrvelDist;
    public string OprTime;
    public string StopTime;
    public string StartBatteryLevel;
    public string LastBatteryLevel;
    public string AccuBattery;

}

public class VMSFakeSignal
{
    public FakeSignalBase[] fakeSignals;
}

public class BufferConveyorFakeSignal
{
    public FakeSignalBase[] fakeSignals;
}

public class LoadingConveyorFakeSignal
{
    public FakeSignalBase[] fakeSignals;
}

public class ASRSFakeSignal
{
    public FakeSignalBase[] fakeSignals;

}


public class FakeSignalBase
{
    public string code;
    public string name;
    public string value;
}

