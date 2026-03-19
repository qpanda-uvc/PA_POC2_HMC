using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

class ASRS_History
{
    public string inOutType;
    public int count;
    public string Asrs;
    public string Awb;

}

class ReqeustSimulation
{
    public string UldCode;
    public bool simulation;

}

public class PSResultInfo
{
    public int statusCode;
    public string message;
    public Wrapper data;
}

public class Wrapper
{
    public int code;
    public List<ResultInfo> result;
}

[System.Serializable]
public class ResultInfo
{
    public List<AWBInfo> AWBInfoList;
    public int AWBsSquareVolume = 0;
    public float AWBsWaterVolume = 0f;
    public float AWBsWeight = 0;
    public int UldId = 0;
    public bool isDone; 
    public string squareVolumeRatio = "";
    public string uldVolume = "";
    public List<object> unpackItems = new List<object>();
    public string version = "";
    public string waterVolumeRatio = "";
}

[System.Serializable]
public class AWBInfo
{
    public string Awbld = "";
    public List<string> SCCs = new List<string>();
    public List<Coordinate> coordinate = new List<Coordinate>();
    public string depth = "";
    public string length = "";
    public string name = "";
    public int order = 0;
    public string squareVolume = "";
    public int storageId = 0;
    public float waterVolume = 0;
    public string weight = "";
    public string width = "";
}

[System.Serializable]
public class Coordinate
{
    public string p1x = "";
    public string p1y = "";
    public string p1z = "";
    public string p2x = "";
    public string p2y = "";
    public string p2z = "";
    public string p3x = "";
    public string p3y = "";
    public string p3z = "";
    public string p4x = "";
    public string p4y = "";
    public string p4z = "";
    public string p5x = "";
    public string p5y = "";
    public string p5z = "";
    public string p6x = "";
    public string p6y = "";
    public string p6z = "";
    public string p7x = "";
    public string p7y = "";
    public string p7z = "";
    public string p8x = "";
    public string p8y = "";
    public string p8z = "";
}

[System.Serializable]
public class VMSAwbInfo
{
    public string barcode;
    public string prefab;
    public float waterVolume;
    public float squareVolume;
    public float width;
    public float length;
    public float depth;
    public float weight;
    public int piece;
    public string state;
    public int parent;
    public string modelPath;
    public bool simulation;
    public string path;
    public int spawnRatio;
    public string description;
    public string[] scc;
    public int AirCraftSchedule;

    public VMSAwbInfo(string barcode, string prefab, float waterVolume,
                      float squareVolume, float width, float length, float depth,
                      float weight, int piece, string state, int parent,
                      string modelPath, bool simulation, string path,
                      int spawnRatio, string description, string[] scc,
                      int AirCraftSchedule)
    {
        this.barcode = barcode;
        this.prefab = prefab;
        this.waterVolume = waterVolume;
        this.squareVolume = squareVolume;
        this.width = width;
        this.length = length;
        this.depth = depth;
        this.weight = weight;
        this.piece = piece;
        this.state = state;
        this.parent = parent;
        this.modelPath = modelPath;
        this.simulation = simulation;
        this.path = path;
        this.spawnRatio = spawnRatio;
        this.description = description;
        this.scc = scc;
        this.AirCraftSchedule = AirCraftSchedule;
    }
}

public class CreateULDResultInfo
{
    public int statusCode;
    public string message;
    public CreateULDJson data; 
}

public class CreateULDJson
{
    public string code;
    public string prefab;
    public string airplaneType;
    public bool simulation;
    public string boundaryPrefab;
    public float loadRate;
    public string UldType;
    //public int id;
}

public class CreateULDResultInfoReturn
{
    public int statusCode;
    public string message;
    public CreateULDJsonReturn data;
}

public class CreateULDJsonReturn
{
    public string code;
    public string prefab;
    public string airplaneType;
    public bool simulation;
    public string boundaryPrefab;
    public float loadRate;
    public string UldType;
    public int id;
}

public class ULDHistory
{
    public float x;
    public float y;
    public float z;
    public int pieceCount;
    public bool recommend;
    public string worker;
    public int BuildUpOrder;
    public int SkidPlatform;
    public int Uld;
    public int Awb; 
}

public class CargoInfoReturn
{
    public int statusCode;
    public string message;
    public List<CargoInfoID> data;
}

public class CargoInfoID
{
    public int id; 
}

public class NetworkManager : MonoBehaviour
{
    [Header("Offline Mode - 서버 없이 시뮬레이션")]
    public bool offlineMode = true;

    [SerializeField] private string alarmUrl = "http://220.90.135.156:3000/";
    [SerializeField] private string REST_SERVER_URL = "http://220.90.135.109:3000/";

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(ResetASRSHistory());
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    IEnumerator ResetASRSHistory()
    {
        string tmp = "";


        UnityWebRequest request;

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(tmp);


        using (request = UnityWebRequest.Post(REST_SERVER_URL + "asrs-history/reset", tmp))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.uploadHandler.Dispose();
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (CheckReqeustError(request))
            {

            }
            

        }

    }

    public void CheckConnection()
    {
        if (offlineMode) return;
        StartCoroutine(ServerConnectionCehck());
    }

    public bool CheckReqeustError(UnityWebRequest returnValue)
    {
        bool tmpReturn = false;

        Debug.Log(returnValue.result);

        switch (returnValue.result)
        {
            case UnityWebRequest.Result.Success:

                tmpReturn = true;
                break;

            case UnityWebRequest.Result.ConnectionError:
                tmpReturn = false;
                break;

            case UnityWebRequest.Result.DataProcessingError:
                tmpReturn = false;
                break;

            default:
                
                break;

        }

        if ( !tmpReturn )
        {
            Debug.Log(returnValue.downloadHandler.text);
        }
        
        return tmpReturn;

    }
    IEnumerator ServerConnectionCehck()
    {
        UnityWebRequest request;

        using (request = UnityWebRequest.Get(REST_SERVER_URL + "/check"))
        {
            yield return request.SendWebRequest();

            Debug.Log(request.downloadHandler.data);

            if (CheckReqeustError(request))
            {

            }

        }

    }


    public void Post_CellCargoUpdate(Cargo cargo, bool isIn)
    {
        if (offlineMode) return;
        ASRS_History tmpHistory = new ASRS_History();
        string tmpData; 

        tmpHistory.inOutType = isIn ? "in" : "out";
        tmpHistory.count = 0;
        tmpHistory.Asrs = "â��" + cargo.currentLocation;
        tmpHistory.Awb = cargo.cargoID;

        tmpData = JsonConvert.SerializeObject(tmpHistory);

        StartCoroutine(PostCargoInCell(tmpData));

    }

    IEnumerator PostCargoInCell(string data)
    {
        Debug.Log(data);

        UnityWebRequest request;

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(data);


        using (request = UnityWebRequest.Post(REST_SERVER_URL + "asrs-history", data))
        {
            request.uploadHandler.Dispose();
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (CheckReqeustError(request))
            {
                
            }
            else
            {
                Debug.Log(data);
            }


        }

    }

    public void Post_StartSimulation(string ULDType, bool isSimulation)
    {
        if (offlineMode) return;
        ReqeustSimulation tmpReqeustSim = new ReqeustSimulation();
        string tmpData;

        tmpReqeustSim.UldCode = ULDType;
        tmpReqeustSim.simulation = isSimulation;

        tmpData = JsonConvert.SerializeObject(tmpReqeustSim);

        StartCoroutine(PostStartSimulation(tmpData)); 
    }

    IEnumerator PostStartSimulation(string data)
    {
        UnityWebRequest request;
        Debug.Log(data);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(data);
        string convertString;
        PSResultInfo tmpInfo = new PSResultInfo();

        using (request = UnityWebRequest.Post(REST_SERVER_URL + "simulator-result/ps-all", data))
        {

            request.uploadHandler.Dispose();
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (CheckReqeustError(request))
            {
                
                convertString = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                Debug.Log(convertString);
                tmpInfo = JsonConvert.DeserializeObject<PSResultInfo>(convertString);
                SimulationModeTaskManager.Instance.ParseSimulationData(tmpInfo.data.result[0]);
                
            }
            else
            {
                
            }


        }
    }

    public void GetClass(string type, System.Action<bool, string> ResultCallback)
    {
        StartCoroutine(Upload(alarmUrl + type, (result) =>
        {
            if (result != null)
            {
                ResultCallback(true, result);
            }
            else
            {
                ResultCallback(false, null);
            }
        }));
    }

    IEnumerator Upload(string URL, System.Action<string> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(URL))
        {
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            Debug.Log("Awb Upload");

            yield return request.SendWebRequest();

            if (CheckReqeustError(request))
            {
                Debug.Log(request.error);
                callback(null);
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
                callback(request.downloadHandler.text);
            }
        }
    }

    public void PostVMSAwb(string jsonFile)
    {
        if (offlineMode) return;
        StartCoroutine(UploadVMSAwb(REST_SERVER_URL + "awb", jsonFile));
    }

    IEnumerator UploadVMSAwb(string URL, string jsonfile)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(URL, jsonfile))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonfile);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            Debug.Log(jsonfile);
            yield return request.SendWebRequest();
            if (CheckReqeustError(request))
            {
                Debug.Log(request.downloadHandler.text);              
            }
            else
            {
                Debug.Log(request.error);
            }
        }
    }

    public void PostCreateULD(string ULDType, string ULDId, string flightID, bool isSimulation)
    {
        if (offlineMode) return;
        CreateULDJson tmpJson = new CreateULDJson();
        string tmpData;

        tmpJson.code = System.DateTime.Now.ToString();
        tmpJson.prefab = "DTTestPrefab";
        tmpJson.airplaneType = "DTTestPlaneType";
        tmpJson.simulation = isSimulation;
        tmpJson.boundaryPrefab = "DTTestBoundaryPrefab";
        tmpJson.loadRate = 0f;
        tmpJson.UldType = ULDType;

        tmpData = JsonConvert.SerializeObject(tmpJson);
        StartCoroutine(PostNewULDCreate(tmpData));
    }

    IEnumerator PostNewULDCreate(string data)
    {
        UnityWebRequest request;
        Debug.Log(data);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(data);
        string convertString;
        CreateULDResultInfoReturn tmpInfo = new CreateULDResultInfoReturn();
        ULDInfoJson tmpULDInfo = new ULDInfoJson();

        using (request = UnityWebRequest.Post(REST_SERVER_URL + "uld", data))
        {

            request.uploadHandler.Dispose();
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (CheckReqeustError(request))
            {
                convertString = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                Debug.Log(convertString);
                tmpInfo = JsonConvert.DeserializeObject<CreateULDResultInfoReturn>(convertString);
                SimulationModeTaskManager.Instance.currentJobULDCode = tmpInfo.data.code;
                SimulationModeTaskManager.Instance.currentJobULDId = tmpInfo.data.id;
                Debug.Log("Current Working ULD code is" + tmpInfo.data.id);
            }


        }
    }

    public void PostULDInCargo(string cargoID, Vector3 cargoPlacedLocation, int ULDId)
    {
        if (offlineMode) return;
        ULDHistory tmpJson = new ULDHistory();

        tmpJson.x = cargoPlacedLocation.x;
        tmpJson.y = cargoPlacedLocation.y;
        tmpJson.z = cargoPlacedLocation.z;
        tmpJson.pieceCount = 0;
        tmpJson.recommend = true;
        tmpJson.worker = "DTTest";
        tmpJson.BuildUpOrder = 21414;
        tmpJson.SkidPlatform = 1;
        tmpJson.Uld = ULDId; 


        StartCoroutine(TryGetCargoID(tmpJson, cargoID));
    }

    IEnumerator TryGetCargoID(ULDHistory ULDdata, string cargoID)
    {
        UnityWebRequest request;
        string convertString;
        CargoInfoReturn tmpInfo = new CargoInfoReturn();
        ULDInfoJson tmpULDInfo = new ULDInfoJson();
        string tmpString; 

        using (request = UnityWebRequest.Get(REST_SERVER_URL + "awb?barcode=" + cargoID))
        {
            yield return request.SendWebRequest();

            if (CheckReqeustError(request))
            {
                Debug.Log(request.downloadHandler.data);
                convertString = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                tmpInfo = JsonConvert.DeserializeObject<CargoInfoReturn>(convertString);
                ULDdata.Awb = tmpInfo.data[0].id;
                tmpString = JsonConvert.SerializeObject(ULDdata);
                StartCoroutine(PostULDInCargo(tmpString));
            }

        }
    }

    IEnumerator PostULDInCargo(string data)
    {

        UnityWebRequest request;
        Debug.Log(data);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(data);

        using (request = UnityWebRequest.Post(REST_SERVER_URL + "uld-history", data))
        {
            request.uploadHandler.Dispose();
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (CheckReqeustError(request))
            {
                Debug.Log(request.downloadHandler.text);
                
            }

        }
    }
}
