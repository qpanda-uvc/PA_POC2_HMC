using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;

public class TestManager : MonoBehaviour
{
    FlightManager flightManager;
    ActiveCheck activeCheck;

    public List<string> tmpFlightJsonList = new List<string>();
    public string tmpCurrentWorkFlightJson;

    public Truck truck;
    public List<ForkLift> forkLift = new List<ForkLift>();
    public List<AMR> amr = new List<AMR>();
    public Conveyor conveyor;
    public GameObject conveyorCargo;
    public StackerCrane stackerCrane;
    public List<string> timeTableList_Name = new List<string>();

    public ULD currentWorkULD;

    public List<GameObject> cargos = new List<GameObject>();
    public List<Vector3> cargoPos = new List<Vector3>();
    int cargoNum;

    public int flightsCount;
    public int flightULDCounts;

    public void Initialize()
    {
        flightManager = FindObjectOfType<FlightManager>();
        activeCheck = FindObjectOfType<ActiveCheck>();

        timeTableList_Name.Add("Stacker Crane");
        timeTableList_Name.Add("ForkLift");
        timeTableList_Name.Add("ForkLift (1)");
        timeTableList_Name.Add("AMR");
        timeTableList_Name.Add("AMR (1)");
        timeTableList_Name.Add("AMR (2)");
    }

    private void Update()
    {
        // 새 Flight가 생성될때 (Serialize해서 tmpFlightJson에 저장)
        if (Input.GetKeyDown(KeyCode.U))
        {
            flightManager.AddNewFlight(newFlight());
        }

        // 새 ULD 작업이 시작될때 
        if (Input.GetKeyDown(KeyCode.I))
        {
            AddNewULD();
        }

        // 현재 작업중인 ULD에 새 Cargo가 추가됐을 때
        if (Input.GetKeyDown(KeyCode.O))
        {
            AddCargo();
        }

        // 지금 작업중인 uld 작업이 끝나고 Serialize해서 tmpULDJson에 저장
        if ((Input.GetKeyDown(KeyCode.P)))
        {
            flightManager.ULDWorkEnd();
        }

        // TimeTable ActiveCheck Test
        if ((Input.GetKeyDown(KeyCode.J)))
        {
            activeCheck.NoticeWorkTime("Stacker Crane", true);
        }
        if ((Input.GetKeyDown(KeyCode.K)))
        {
            activeCheck.NoticeWorkTime("Stacker Crane", false);
        }

        // 지금 작업중인 항공편의 작업이 끝났을때
        if ((Input.GetKeyDown(KeyCode.L)))
        {
            tmpFlightJsonList.Add(tmpCurrentWorkFlightJson);
        }

        // 조회 버튼(Deserialize한 후에 생성해줌)(Day 변경시 기능으로 갈 것)
        if (Input.GetKeyDown(KeyCode.F))
        {
            flightManager.DeserializeFlightInfoJson(tmpFlightJsonList);
        }

        // 통계 리포트 확인 버튼 
        if (Input.GetKeyDown(KeyCode.G))
        {
            flightManager.StatisticalDeserializeFlightInfoJson(tmpFlightJsonList);
        }

    }

    public FlightInfo newFlight()
    {
        FlightInfo tmpFlightInfo = new FlightInfo();
        tmpFlightInfo.airplaneRallyPoint = new string[4];
        tmpFlightInfo.flightName = "KE8055";
        tmpFlightInfo.flightType = "B747-400F";
        tmpFlightInfo.airplaneRallyPoint[0] = "ATL";
        tmpFlightInfo.airplaneRallyPoint[1] = "TKO";
        tmpFlightInfo.airplaneRallyPoint[2] = "ICN";
        tmpFlightInfo.airplaneRallyPoint[3] = "JFK";

        ULDInfo tmpULDInfo = new ULDInfo();
        tmpULDInfo.main = false;
        tmpULDInfo.posIndex = 10;
        tmpULDInfo.uldIndex = "lower_uldIndex10";
        tmpULDInfo.uldType = "SCA";

        ULDInfo tmpULDInfo2 = new ULDInfo();
        tmpULDInfo2.main = false;
        tmpULDInfo2.posIndex = 11;
        tmpULDInfo2.uldIndex = "lower_uldIndex11";
        tmpULDInfo2.uldType = "SCA";

        tmpFlightInfo.uldInfos.Add(tmpULDInfo.uldIndex, tmpULDInfo);
        tmpFlightInfo.uldInfos.Add(tmpULDInfo2.uldIndex, tmpULDInfo2);

        return tmpFlightInfo;
    }


    public int addULDCount = 1;

    public void AddNewULD()
    {
        ULDInfo newULDInfo = new ULDInfo();
        newULDInfo.main = true;
        newULDInfo.posIndex = addULDCount;
        addULDCount++;
        newULDInfo.uldIndex = "uldIndex" + addULDCount.ToString();
        newULDInfo.uldType = "SCA";

        newULDInfo.id = "new ID";
        newULDInfo.wVolume = 25f;
        newULDInfo.sVolume = 14f;
        newULDInfo.workTime = 0f;
        newULDInfo.scc.Add("DIP");
        newULDInfo.scc.Add("VIP");
        newULDInfo.scc.Add("HEA");
        newULDInfo.scc.Add("BUA");

        newULDInfo.volume = 90f;
        newULDInfo.weight = 4000f;
        newULDInfo.destination = "Seoul";
        newULDInfo.workStage = 2;
        newULDInfo.timeTableList_Name = timeTableList_Name;

        newULDInfo.conStartTime = new Dictionary<string, List<float>>();
        newULDInfo.conEndTime = new Dictionary<string, List<float>>();
        newULDInfo.simStartTime = new Dictionary<string, List<float>>();
        newULDInfo.simEndTime = new Dictionary<string, List<float>>();

        foreach (string timeTableName in timeTableList_Name)
        {
            newULDInfo.conStartTime.Add(timeTableName, new List<float>());
        }
        foreach (string timeTableName in timeTableList_Name)
        {
            newULDInfo.conEndTime.Add(timeTableName, new List<float>());
        }
        foreach (string timeTableName in timeTableList_Name)
        {
            newULDInfo.simStartTime.Add(timeTableName, new List<float>());
        }
        foreach (string timeTableName in timeTableList_Name)
        {
            newULDInfo.simEndTime.Add(timeTableName, new List<float>());
        }
        newULDInfo.conStartTime[timeTableList_Name[3]].Add(15f);
        newULDInfo.conEndTime[timeTableList_Name[3]].Add(45f);
        newULDInfo.conStartTime[timeTableList_Name[3]].Add(75f);
        newULDInfo.conEndTime[timeTableList_Name[3]].Add(115f);
        newULDInfo.conStartTime[timeTableList_Name[3]].Add(175f);
        newULDInfo.conEndTime[timeTableList_Name[3]].Add(215f);
        newULDInfo.conStartTime[timeTableList_Name[3]].Add(255f);
        newULDInfo.conEndTime[timeTableList_Name[3]].Add(335f);
        newULDInfo.conStartTime[timeTableList_Name[3]].Add(425f);
        newULDInfo.conEndTime[timeTableList_Name[3]].Add(515f);
        newULDInfo.conStartTime[timeTableList_Name[3]].Add(655f);
        newULDInfo.conEndTime[timeTableList_Name[3]].Add(885f);
        newULDInfo.simStartTime[timeTableList_Name[3]].Add(15f);
        newULDInfo.simEndTime[timeTableList_Name[3]].Add(45f);
        newULDInfo.simStartTime[timeTableList_Name[3]].Add(75f);
        newULDInfo.simEndTime[timeTableList_Name[3]].Add(115f);
        newULDInfo.simStartTime[timeTableList_Name[3]].Add(175f);
        newULDInfo.simEndTime[timeTableList_Name[3]].Add(215f);
        newULDInfo.simStartTime[timeTableList_Name[3]].Add(255f);
        newULDInfo.simEndTime[timeTableList_Name[3]].Add(335f);
        newULDInfo.simStartTime[timeTableList_Name[3]].Add(425f);
        newULDInfo.simEndTime[timeTableList_Name[3]].Add(515f);
        newULDInfo.simStartTime[timeTableList_Name[3]].Add(655f);
        newULDInfo.simEndTime[timeTableList_Name[3]].Add(885f);

        newULDInfo.conStartTime[timeTableList_Name[4]].Add(95f);
        newULDInfo.conEndTime[timeTableList_Name[4]].Add(205f);
        newULDInfo.simStartTime[timeTableList_Name[4]].Add(95f);
        newULDInfo.simEndTime[timeTableList_Name[4]].Add(205f);
        newULDInfo.conStartTime[timeTableList_Name[4]].Add(225f);
        newULDInfo.conEndTime[timeTableList_Name[4]].Add(355f);
        newULDInfo.simStartTime[timeTableList_Name[4]].Add(455f);
        newULDInfo.simEndTime[timeTableList_Name[4]].Add(1520f);

        newULDInfo.conStartTime[timeTableList_Name[5]].Add(95f);
        newULDInfo.conEndTime[timeTableList_Name[5]].Add(205f);
        newULDInfo.simStartTime[timeTableList_Name[5]].Add(95f);
        newULDInfo.simEndTime[timeTableList_Name[5]].Add(205f);
        newULDInfo.conStartTime[timeTableList_Name[5]].Add(225f);
        newULDInfo.conEndTime[timeTableList_Name[5]].Add(355f);
        newULDInfo.simStartTime[timeTableList_Name[5]].Add(455f);
        newULDInfo.simEndTime[timeTableList_Name[5]].Add(1520f);

        flightManager.AddNewULDInfo(newULDInfo);
    }

    public void AddCargo()
    {
        GameObject workFinishCargo = Instantiate(cargos[cargoNum]);
        workFinishCargo.name = cargos[cargoNum].name;
        currentWorkULD.AddNewCargo(workFinishCargo, cargoPos[cargoNum]);
        flightManager.UpdateAddCargoData(workFinishCargo.GetComponent<CargoInfo>(), cargoPos[cargoNum]);
        //cargoNum++;
    }


}
