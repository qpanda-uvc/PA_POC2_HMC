using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public class FlightManager : MonoBehaviour
{
    public FlightCanvas flightCanvas;
    ActiveCheck activeCheck;

    public List<FlightInfo> flightInfos = new List<FlightInfo>();

    public FlightInfo selectedFlight;
    public FlightInfo currentWorkFlightInfo;
    public ULDInfo currentWorkULDInfo;

    public List<FlightInfo> statisticalFlightInfos = new List<FlightInfo>();

    public void Initialize()
    {
        activeCheck = FindObjectOfType<ActiveCheck>();
    }

    public void RequestFlightInfoByDate(string selectedDate)
    {
        //DeserializeFlightInfoJson();

        //DeserializeULDInfos(tmpULDJson);
        //DeserializeFlightInfoJson(tmpFlightJson);
    }
    /*
    internal void DeserializeULDInfoJson(object tmpULDJson)
    {
        throw new System.NotImplementedException();
    }
    */

    /*
    public void DeserializeULDInfos(List<string> tmpULDJson)
    {
        foreach (string uldJsons in tmpULDJson)
        {
            DeserializeULDInfoJson(uldJsons);
        }
    }
    */
    public ULDInfo DeserializeULDInfoJson(string jsonString)
    {
        ULDInfoJson tmpULDInfoJson = new ULDInfoJson();

        tmpULDInfoJson = JsonConvert.DeserializeObject<ULDInfoJson>(jsonString);
        
        ULDInfo tmpULDInfo = new ULDInfo();

        tmpULDInfo.main = tmpULDInfoJson.main;
        tmpULDInfo.posIndex = tmpULDInfoJson.posIndex;
        tmpULDInfo.uldIndex = tmpULDInfoJson.uldIndex;
        tmpULDInfo.uldType = tmpULDInfoJson.uldType;
        for(int i = 0; i < tmpULDInfoJson.cargoInfos.Count; i++)
        {
            tmpULDInfo.cargoInfos.Add(DeserializeCargoInfoJson(tmpULDInfoJson.cargoInfos[i]));
        }
        tmpULDInfo.cargos = tmpULDInfoJson.cargos;
        for (int i = 0; i < tmpULDInfoJson.cargoPos.Count; i++)
        {
            Vector3 cargoPosVector = new Vector3();
            cargoPosVector = JsonUtility.FromJson<Vector3>(tmpULDInfoJson.cargoPos[i]);
            tmpULDInfo.cargoPos.Add(cargoPosVector);
        }
        tmpULDInfo.id = tmpULDInfoJson.id;
        tmpULDInfo.wVolume = tmpULDInfoJson.wVolume;
        tmpULDInfo.sVolume = tmpULDInfoJson.sVolume;
        tmpULDInfo.workTime = tmpULDInfoJson.workTime;
        tmpULDInfo.scc = tmpULDInfoJson.scc;
        tmpULDInfo.volume = tmpULDInfoJson.volume;
        tmpULDInfo.weight = tmpULDInfoJson.weight;
        tmpULDInfo.destination = tmpULDInfoJson.destination;
        tmpULDInfo.workStage = tmpULDInfoJson.workStage;

        foreach (var item in tmpULDInfoJson.objectWorkTimeRecords)
        {
            tmpULDInfo.timeTableList_Name.Add(item.objectName);
            tmpULDInfo.conStartTime.Add(item.objectName, item.conStartTime);
            tmpULDInfo.conEndTime.Add(item.objectName, item.conEndTime);
            tmpULDInfo.simStartTime.Add(item.objectName, item.simStartTime);
            tmpULDInfo.simEndTime.Add(item.objectName, item.simEndTime);
        }
        return tmpULDInfo;
    }

    public void DeserializeFlightInfoJson(List<string> jsonString)
    {
        flightInfos.Clear();
        FlightInfoJson tmpFlightInfoJson = new FlightInfoJson();
        
        foreach (string flightStrings in jsonString)
        {
            tmpFlightInfoJson = JsonConvert.DeserializeObject<FlightInfoJson>(flightStrings);

            FlightInfo tmpFlightInfo = new FlightInfo();
            tmpFlightInfo.flightName = tmpFlightInfoJson.flightName;
            tmpFlightInfo.flightType = tmpFlightInfoJson.flightType;
            tmpFlightInfo.airplaneRallyPoint = tmpFlightInfoJson.airplaneRallyPoint;

            foreach (string uldInfoJsons in tmpFlightInfoJson.uldInfoJsons)
            {
                ULDInfo tmpULDInfo = DeserializeULDInfoJson(uldInfoJsons);
                tmpFlightInfo.uldInfos.Add(tmpULDInfo.uldIndex, tmpULDInfo);
            }

            // 반복으로 분리한 flight 추가
            flightInfos.Add(tmpFlightInfo);
        }

        flightCanvas.GenerateFlight_btn(flightInfos[0]); // 실제 flight버튼 생성
        flightCanvas.flightsNum = 0;
    }

    public void GenerateFlightInfoJson(FlightInfo newFlightInfo)
    {
        FlightInfoJson tmpFlightInfoJson = new FlightInfoJson();
        tmpFlightInfoJson.flightName = newFlightInfo.flightName;
        tmpFlightInfoJson.flightType = newFlightInfo.flightType;
        tmpFlightInfoJson.airplaneRallyPoint = newFlightInfo.airplaneRallyPoint;

        List<string> uldInfoString = new List<string>();
        foreach (ULDInfo uldInfos in newFlightInfo.uldInfos.Values)
        {
            uldInfoString.Add(GenerateULDInfoJson(uldInfos));
        }
        string[] uldInfoArray = uldInfoString.ToArray();
        tmpFlightInfoJson.uldInfoJsons = uldInfoArray;

        string json = JsonConvert.SerializeObject(tmpFlightInfoJson);

        TestManager testManager;
        testManager = FindObjectOfType<TestManager>();

        testManager.tmpCurrentWorkFlightJson = json;

        Debug.Log(json);
    }
    
    public string GenerateULDInfoJson(ULDInfo newULDInfo)
    {
        ULDInfoJson tmpULDInfoJson = new ULDInfoJson();
        tmpULDInfoJson.main = newULDInfo.main;
        tmpULDInfoJson.posIndex = newULDInfo.posIndex;
        tmpULDInfoJson.uldIndex = newULDInfo.uldIndex;
        tmpULDInfoJson.uldType = newULDInfo.uldType;
        for (int i = 0; i < newULDInfo.cargoInfos.Count; i++) 
        {
            tmpULDInfoJson.cargoInfos.Add(GenerateCargoInfoJson(newULDInfo.cargoInfos[i]));
        }
        tmpULDInfoJson.cargos = newULDInfo.cargos;
        for(int i = 0; i < newULDInfo.cargoPos.Count; i++)
        {
            string cargoPosVector = JsonUtility.ToJson(newULDInfo.cargoPos[i]);
            tmpULDInfoJson.cargoPos.Add(cargoPosVector);
        }

        tmpULDInfoJson.id = newULDInfo.id;
        tmpULDInfoJson.wVolume = newULDInfo.wVolume;
        tmpULDInfoJson.sVolume = newULDInfo.sVolume;
        tmpULDInfoJson.workTime = newULDInfo.workTime;
        tmpULDInfoJson.scc = newULDInfo.scc;
        tmpULDInfoJson.volume = newULDInfo.volume;
        tmpULDInfoJson.weight = newULDInfo.weight;
        tmpULDInfoJson.destination = newULDInfo.destination;
        tmpULDInfoJson.workStage = newULDInfo.workStage;
        tmpULDInfoJson.timeTableList_Name = newULDInfo.timeTableList_Name;

        foreach (var item in tmpULDInfoJson.timeTableList_Name)
        {
            ObjectWorkTimeRecord tmpObjectWorkTimeRecord = new ObjectWorkTimeRecord();
            tmpObjectWorkTimeRecord.objectName = item;
            tmpObjectWorkTimeRecord.conStartTime = newULDInfo.conStartTime[item];
            tmpObjectWorkTimeRecord.conEndTime = newULDInfo.conEndTime[item];
            tmpObjectWorkTimeRecord.simStartTime = newULDInfo.simStartTime[item];
            tmpObjectWorkTimeRecord.simEndTime = newULDInfo.simEndTime[item];
            tmpULDInfoJson.objectWorkTimeRecords.Add(tmpObjectWorkTimeRecord);
        }

        string json = JsonConvert.SerializeObject(tmpULDInfoJson);

        return json;
    }
    
    public CargoInfo DeserializeCargoInfoJson(string jsonString)
    {
        CargoInfoJson tmpCargoInfoJson = new CargoInfoJson();

        tmpCargoInfoJson = JsonConvert.DeserializeObject<CargoInfoJson>(jsonString);

        CargoInfo tmpCargoInfo = new CargoInfo();

        tmpCargoInfo.cargoName = tmpCargoInfoJson.name;
        tmpCargoInfo.pou = tmpCargoInfoJson.pou;
        tmpCargoInfo.wVolume = tmpCargoInfoJson.wVolume;
        tmpCargoInfo.sVolume = tmpCargoInfoJson.sVolume;
        tmpCargoInfo.volume = tmpCargoInfoJson.volume;
        tmpCargoInfo.weight = tmpCargoInfoJson.weight;
        tmpCargoInfo.scc = tmpCargoInfoJson.scc;

        return tmpCargoInfo;
    }

    public string GenerateCargoInfoJson(CargoInfo cargoInfo)
    {
        CargoInfoJson tmpCargoInfoJson = new CargoInfoJson();

        tmpCargoInfoJson.name = cargoInfo.cargoName;
        tmpCargoInfoJson.pou = cargoInfo.pou;
        tmpCargoInfoJson.wVolume = cargoInfo.wVolume;
        tmpCargoInfoJson.sVolume = cargoInfo.sVolume;
        tmpCargoInfoJson.volume = cargoInfo.volume;
        tmpCargoInfoJson.weight = cargoInfo.weight;
        tmpCargoInfoJson.scc = cargoInfo.scc;

        string json = JsonConvert.SerializeObject(tmpCargoInfoJson);

        return json;
    }
    

    public void AddNewFlight(FlightInfo flightInfo)
    {
        GenerateFlightInfoJson(flightInfo);
        currentWorkFlightInfo = flightInfo;
    }

    public void AddNewULDInfo(ULDInfo newULDInfo)
    {
        activeCheck.TimeInitialize();

        // 새 uld 생성 필요

        currentWorkULDInfo = new ULDInfo();
        currentWorkULDInfo = newULDInfo;

        if (newULDInfo.main)
        {
            int count = 0;
            foreach(ULDInfo uldInfos in currentWorkFlightInfo.uldInfos.Values)
            {
                if (uldInfos.main)
                {
                    count++;
                }
            }
            newULDInfo.posIndex = count + 1;
        }
        else
        {
            int count = 0;
            foreach (ULDInfo uldInfos in currentWorkFlightInfo.uldInfos.Values)
            {
                if (!uldInfos.main)
                {
                    count++;
                }
            }
            newULDInfo.posIndex = count + 1;
        }
        currentWorkFlightInfo.uldInfos.Add(currentWorkULDInfo.uldIndex, currentWorkULDInfo);
        GenerateFlightInfoJson(currentWorkFlightInfo);
    }

    public void UpdateAddCargoData(CargoInfo cargo, Vector3 cargoPos)
    {
        currentWorkULDInfo.cargoInfos.Add(cargo);
        currentWorkULDInfo.cargos.Add(cargo.cargoName);
        currentWorkULDInfo.cargoPos.Add(cargoPos);
        currentWorkULDInfo.wVolume += cargo.wVolume;
        currentWorkULDInfo.sVolume += cargo.sVolume;
        currentWorkULDInfo.volume += cargo.volume;
        currentWorkULDInfo.weight += cargo.weight;
        currentWorkULDInfo.scc.AddRange(cargo.scc);

        currentWorkULDInfo.workTime = activeCheck.uldWorkTime;

        currentWorkFlightInfo.uldInfos[currentWorkULDInfo.uldIndex] = currentWorkULDInfo;
        GenerateFlightInfoJson(currentWorkFlightInfo);
    }

    public void ULDWorkEnd()
    {
        activeCheck.ULDWorkEnd();

        currentWorkFlightInfo.uldInfos[currentWorkULDInfo.uldIndex] = currentWorkULDInfo;
        GenerateFlightInfoJson(currentWorkFlightInfo);
    }

    public void StatisticalDeserializeFlightInfoJson(List<string> jsonString)
    {
        FlightInfoJson tmpFlightInfoJson = new FlightInfoJson();

        foreach (string flightStrings in jsonString)
        {
            tmpFlightInfoJson = JsonConvert.DeserializeObject<FlightInfoJson>(flightStrings);

            FlightInfo tmpFlightInfo = new FlightInfo();
            tmpFlightInfo.flightName = tmpFlightInfoJson.flightName;
            tmpFlightInfo.flightType = tmpFlightInfoJson.flightType;
            tmpFlightInfo.airplaneRallyPoint = tmpFlightInfoJson.airplaneRallyPoint;

            foreach (string uldInfoJsons in tmpFlightInfoJson.uldInfoJsons)
            {
                ULDInfo tmpULDInfo = DeserializeULDInfoJson(uldInfoJsons);
                tmpFlightInfo.uldInfos.Add(tmpULDInfo.uldIndex, tmpULDInfo);
            }

            statisticalFlightInfos.Add(tmpFlightInfo);
        }
    }

    public void StatisticalReport()
    {
        List<string> allSCC = new List<string>();

        float averageLoadingRate = 0;
        float averageWorkingHours = 0;
        float uldCount = 0;

        float vms = 0;
        float amr = 0;
        float asrs = 0;

        float loadingRateSum = 0;
        float workingHoursSum = 0;
        
        foreach (FlightInfo flightInfos in statisticalFlightInfos)
        {
            foreach (ULDInfo uldInfos in flightInfos.uldInfos.Values)
            {
                allSCC.AddRange(uldInfos.scc);

                loadingRateSum += uldInfos.sVolume;
                workingHoursSum += uldInfos.workTime;
            }
            uldCount += flightInfos.uldInfos.Count;
        }
        averageLoadingRate = loadingRateSum / uldCount;
        averageWorkingHours = workingHoursSum / uldCount;

        // 추천이행 추가 필요

    }

    /*
    public float[] AwbKpiArray()
    {
        float[] tmpfloat;

        return tmpfloat;
    }

    public float[] pouTop3Array()
    {

    }
    */
    public float[] sccTop3Array()
    {
        List<string> allSCC = new List<string>();


        foreach(FlightInfo flightInfos in statisticalFlightInfos)
        {
            foreach(ULDInfo uldInfos in flightInfos.uldInfos.Values)
            {
                allSCC.AddRange(uldInfos.scc);
            }
        }

        Dictionary<string, int> elementCounts = new Dictionary<string, int>();

        // allSCC 리스트의 요소들을 순회하며 개수를 세기
        foreach (string element in allSCC)
        {
            if (elementCounts.ContainsKey(element))
            {
                elementCounts[element]++;
            }
            else
            {
                elementCounts[element] = 1;
            }
        }

        // 중복된 요소 개수를 기준으로 내림차순 정렬
        var sortedCounts = elementCounts.OrderByDescending(pair => pair.Value);

        // 상위 3개 요소를 가져오기
        var top3 = sortedCounts.Take(3);


        List<float> tmpfloatArray = new List<float>();
        return tmpfloatArray.ToArray();
    }

}
