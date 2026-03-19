using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AMRSignal
{
    public string AMR_ID;
    public float X;
    public float Y;
    public float H;
    public bool loaded;

}

public enum AMRTask
{
    VMSToASRS,
    ASRSToVMSWaiting,
    VMSWatingToVMS,
    ASRSToULDSettlment,
    ULDSettlementToWaiting,
    WaitingToASRSEntry,
    EntryToASRS

}

public class AMRTaskParser : MonoBehaviour
{
    AMRWaypointManager waypointManager;
    public List<AMR> inputAMRs;
    public List<AMR> outputAMRs;

    public Queue<AMR> AMR_VMSWaitingToVMS_Queue = new Queue<AMR>();
    public Queue<AMR> AMR_VMStoASRS_Queue = new Queue<AMR>();

    public Queue<AMR> AMR_ASRStoWorkStation_Queue = new Queue<AMR>();
    public Queue<AMR> AMR_WaitingToEntry_Queue = new Queue<AMR>();
    public Queue<AMR> AMR_EntryToASRS_Queue = new Queue<AMR>();


    public AMR outputAMR;

    public GameObject pathReference;

    int outputSettlementIndex;
    int outputWaitingIndex;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Init()
    {
        waypointManager = FindObjectOfType<AMRWaypointManager>();
        InitializeAMRList();
        EnqueInitialQueue();
    }

    public void InitializeAMRList()
    {
        List<AMR> tmpAMR = new List<AMR>();
        tmpAMR.AddRange(FindObjectsOfType<AMR>());

        foreach(var item in tmpAMR)
        {
            switch (item.group)
            {
                case AMRGroup.InputGroup:
                    inputAMRs.Add(item);
                    break;

                case AMRGroup.OutputGroup:
                    outputAMRs.Add(item);
                    break;

                default:
                    Debug.Log(item.name + " has no group");
                     break;

            }
        }
    }


    public void EnqueInitialQueue()
    {
        float tmpFloat = 0f;
        Vector3 tmpTargetPosition = waypointManager.VmsToASRS[0].transform.position;
        List<KeyValuePair<AMR, float>> sortList = new List<KeyValuePair<AMR, float>>();

        foreach (var item in inputAMRs)
        {
            tmpFloat = Vector3.Distance(item.transform.position, tmpTargetPosition);
            sortList.Add(new KeyValuePair<AMR, float>(item, tmpFloat));
        }

        sortList.Sort((x, y) => x.Value.CompareTo(y.Value));

        AMR_VMStoASRS_Queue.Enqueue(sortList[0].Key);
        AMR_VMSWaitingToVMS_Queue.Enqueue(sortList[1].Key);

        sortList.Clear();

        tmpFloat = 0f;
        tmpTargetPosition = waypointManager.ASRSToULD[0].transform.position;

        foreach (var item in outputAMRs)
        {
            tmpFloat = Vector3.Distance(item.transform.position, tmpTargetPosition);
            sortList.Add(new KeyValuePair<AMR, float>(item, tmpFloat));
        }

        sortList.Sort((x, y) => x.Value.CompareTo(y.Value));

        
        AMR_ASRStoWorkStation_Queue.Enqueue(sortList[0].Key);
        AMR_EntryToASRS_Queue.Enqueue(sortList[1].Key);
        AMR_WaitingToEntry_Queue.Enqueue(sortList[2].Key);

    }

    

    public bool Order_VMStoASRS(GameObject cargo)
    {
        if (AMR_VMStoASRS_Queue.Count == 0)
        {
            Debug.LogWarning("[AMR] Order_VMStoASRS: 가용 AMR 없음, 보류");
            return false;
        }

        AMR closestAMR = AMR_VMStoASRS_Queue.Dequeue();

        closestAMR.cargo = cargo;

        cargo.transform.SetParent(closestAMR.lift.transform);
        cargo.transform.localPosition = new Vector3(0f, 0.18f, 0f) ;
        closestAMR.locations = waypointManager.VmsToASRS;
        closestAMR.currentIndex = 1;
        closestAMR.currentTask = AMRTask.VMSToASRS;

        Debug.Log("[AMR] " + closestAMR.id + " → VMStoASRS");
        closestAMR.SpinAndMove();
        return true;
    }


    public bool Order_VMSWaitingtoVMS(AMR amr)
    {
        if (AMR_VMSWaitingToVMS_Queue.Count == 0)
        {
            Debug.LogWarning("[AMR] Order_VMSWaitingtoVMS: 가용 AMR 없음, 보류");
            return false;
        }

        AMR waitingAMR = AMR_VMSWaitingToVMS_Queue.Dequeue();

        waitingAMR.locations = waypointManager.VMSWaitingToVMS;
        waitingAMR.currentIndex = 1;
        waitingAMR.currentTask = AMRTask.VMSWatingToVMS;

        Debug.Log("[AMR] " + waitingAMR.id + " → VMSWaitingToVMS");
        waitingAMR.SpinAndMove();
        return true;
    }

    public void Order_ASRStoVMSWaitingZone(AMR amr)
    {
        amr.locations = waypointManager.ASRSToVMSWaiting;
        amr.currentIndex = 1;
        amr.currentTask = AMRTask.ASRSToVMSWaiting;

        Debug.Log("[AMR] " + amr.id + " → ASRStoVMSWaiting");
        amr.SpinAndMove();
    }


    // 씬에서 남아있는 출고 Receipt를 캐시
    private GameObject cachedOutputReceipt;

    public bool Order_ASRStoULDSettlement(Cargo cargo)
    {
        if (AMR_ASRStoWorkStation_Queue.Count == 0)
        {
            Debug.LogWarning("[AMR] Order_ASRStoULDSettlement: 가용 AMR 없음, 보류");
            return false;
        }

        AMR closestAMR = AMR_ASRStoWorkStation_Queue.Dequeue();

        closestAMR.cargo = cargo.gameObject;

        cargo.transform.SetParent(closestAMR.lift.transform);
        cargo.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        // 남아있는 Receipt를 찾아서 목적지로 사용
        if (cachedOutputReceipt == null)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene.isLoaded && go.name.Contains("AMR_Receipt") && go.activeInHierarchy && !go.name.Contains("(10)"))
                {
                    cachedOutputReceipt = go;
                    break;
                }
            }
        }

        List<GameObject> tmpWaypoints = new List<GameObject>();
        tmpWaypoints.Add(null);
        if (cachedOutputReceipt != null)
            tmpWaypoints.Add(cachedOutputReceipt);

        closestAMR.locations = tmpWaypoints;
        closestAMR.currentIndex = 1;
        closestAMR.currentTask = AMRTask.ASRSToULDSettlment;

        closestAMR.SpinAndMove();
        return true;
    }

    public void Order_ULDSettlementToWaiting(AMR amr)
    {
        List<GameObject> tmpWaypoints = new List<GameObject>();

        tmpWaypoints.Add(null);

        GameObject tmp = Instantiate(pathReference);
        Vector3 tmpVector = new Vector3();
        tmpVector = amr.transform.position;
        tmpVector.x = waypointManager.ULDToASRSWaiting[0].transform.position.x;
        tmp.transform.position = tmpVector;
        tmpWaypoints.Add(tmp);
        tmpWaypoints.Add(waypointManager.ULDToASRSWaiting[0]);

        GameObject tmp2 = Instantiate(pathReference);
        tmpVector = waypointManager.ULDToASRSWaiting[0].transform.position;
        tmpVector.x = waypointManager.ULDToASRSWaiting[outputWaitingIndex + 1].transform.position.x;
        tmp2.transform.position = tmpVector;
        tmpWaypoints.Add(tmp2);
        tmpWaypoints.Add(waypointManager.ULDToASRSWaiting[outputWaitingIndex + 1]);

        amr.locations = tmpWaypoints;
        amr.currentIndex = 1;
        amr.currentTask = AMRTask.ULDSettlementToWaiting;

        Debug.Log("[AMR] " + amr.id + " → ULDSettlementToWaiting");
        outputWaitingIndex++;
        outputWaitingIndex = outputWaitingIndex % 3;

        amr.SpinAndMove();
    }

    public bool Order_WaitingToASRSEntry(AMR amr)
    {
        if (AMR_WaitingToEntry_Queue.Count == 0)
        {
            Debug.LogWarning("[AMR] Order_WaitingToASRSEntry: 가용 AMR 없음, 보류");
            return false;
        }

        AMR waitingAMR = AMR_WaitingToEntry_Queue.Dequeue();

        List<GameObject> tmpWaypoints = new List<GameObject>();

        tmpWaypoints.Add(null);

        GameObject tmp = Instantiate(pathReference);
        Vector3 tmpVector = new Vector3();
        tmpVector = waitingAMR.transform.position;
        tmpVector.z = waypointManager.ASRSEntryWaiting[0].transform.position.z;
        tmp.transform.position = tmpVector;
        tmpWaypoints.Add(tmp);
        tmpWaypoints.Add(waypointManager.ASRSEntryWaiting[0]);


        waitingAMR.locations = tmpWaypoints;
        waitingAMR.currentIndex = 1;
        waitingAMR.currentTask = AMRTask.WaitingToASRSEntry;

        Debug.Log("[AMR] " + waitingAMR.id + " → WaitingToASRSEntry");
        waitingAMR.SpinAndMove();
        return true;
    }

    public bool Order_ASRSEntryToASRS(AMR amr)
    {
        if (AMR_EntryToASRS_Queue.Count == 0)
        {
            Debug.LogWarning("[AMR] Order_ASRSEntryToASRS: 가용 AMR 없음, 보류");
            return false;
        }

        AMR waitingAMR = AMR_EntryToASRS_Queue.Dequeue();

        waitingAMR.locations = waypointManager.WaitingToASRS;
        waitingAMR.currentIndex = 1;
        waitingAMR.currentTask = AMRTask.EntryToASRS;

        Debug.Log("[AMR] " + waitingAMR.id + " → EntryToASRS");
        waitingAMR.SpinAndMove();
        return true;
    }

}



