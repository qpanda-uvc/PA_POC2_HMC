using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PGD;
using INab.Dissolve;

public class SimulationModeTaskManager : MonoBehaviour
{
    private static SimulationModeTaskManager simulationModeTaskManagerInstance = null;
    public NetworkManager networkManager;
    AMRTaskParser amrTaskParser;
    public DataManager datamanager;
    public ForkLiftWaypointManager forkLiftWaypointManager;

    public List<Dictionary<string, object>> dataParsing = new List<Dictionary<string, object>>();
    List<Cargo> generatedCargo;
    Queue<string> pullQueue = new Queue<string>();
    public Queue<Cargo> cargoSpawnQueue = new Queue<Cargo>();
    public float initaialCargoRatio;

    public Panel_011 floatingPanel;
    public int settlementCargoIndex;

    public int tmpFlightULDCount = 30;

    float ULD_LOAD_SPEC = 17.863f;
    float currentLoadWeight;
    public bool cargoExhausted = false;
    public bool ULDFullFlag = false;
    

    Truck truck;
    ForkLift InForkLift;
    ForkLift OutForkLift;
    Vector3 inputForkLiftLoadLocation;
    Vector3 inputForkLiftUnloadLocation;

    public ASRS asrs;
    public ULD currentJobULD;
    public string currentJobULDCode;
    public int currentJobULDId;


    [SerializeField]
    public GameObject testCargo;

    public Queue<GameObject> onULDSettlementCargo = new Queue<GameObject>();

    public Queue<Cargo> asrsWaitingCargo = new Queue<Cargo>();

    [SerializeField]
    public bool instantMode;

    [SerializeField]
    public Queue<Cargo> instantQueue = new Queue<Cargo>();

    // 출고 AMR 디스패치 실패 시 재시도 카운터
    private int pendingWaitingToEntry = 0;
    private int pendingEntryToASRS = 0;

    // ULD 적재 추적
    private int uldLoadedCount = 0;
    private int uldExpectedCount = 0;

    // ForkLift 타임아웃 타이머
    private float outForkLiftStuckTimer = 0f;
    private float inForkLiftStuckTimer = 0f;

    [Header("Simulation Speed")]
    [Range(0.5f, 20f)]
    public float simulationSpeed = 1f;
    private float lastSimulationSpeed = 1f;

    private void Update()
    {
        if (!Mathf.Approximately(simulationSpeed, lastSimulationSpeed))
        {
            Time.timeScale = simulationSpeed;
            lastSimulationSpeed = simulationSpeed;
            Debug.Log("[SIM] Speed: x" + simulationSpeed);
        }
    }

    private void Start()
    {
        Debug.Log("[SIM] Start() BEGIN");
        if (simulationModeTaskManagerInstance == null)
        {
            simulationModeTaskManagerInstance = this;
        }
        else
        {
            Destroy(this);
        }

        Debug.Log("[SIM] 1. Finding objects...");
        truck = GameObject.FindObjectOfType<Truck>();
        Debug.Log("[SIM] truck: " + (truck != null));

        var inForkObj = GameObject.Find(nameof(InForkLift));
        Debug.Log("[SIM] InForkLift obj: " + (inForkObj != null));
        InForkLift = inForkObj != null ? inForkObj.GetComponent<ForkLift>() : null;

        var outForkObj = GameObject.Find(nameof(OutForkLift));
        Debug.Log("[SIM] OutForkLift obj: " + (outForkObj != null));
        OutForkLift = outForkObj != null ? outForkObj.GetComponent<ForkLift>() : null;

        var loadLocObj = GameObject.Find(nameof(inputForkLiftLoadLocation));
        Debug.Log("[SIM] inputForkLiftLoadLocation obj: " + (loadLocObj != null));
        inputForkLiftLoadLocation = loadLocObj != null ? loadLocObj.transform.position : Vector3.zero;

        var unloadLocObj = GameObject.Find(nameof(inputForkLiftUnloadLocation));
        Debug.Log("[SIM] inputForkLiftUnloadLocation obj: " + (unloadLocObj != null));
        inputForkLiftUnloadLocation = unloadLocObj != null ? unloadLocObj.transform.position : Vector3.zero;

        forkLiftWaypointManager = GameObject.FindObjectOfType<ForkLiftWaypointManager>();
        floatingPanel = GameObject.FindObjectOfType<Panel_011>();
        currentJobULD = GameObject.FindObjectOfType<ULD>();
        generatedCargo = new List<Cargo>();
        amrTaskParser = GameObject.FindObjectOfType<AMRTaskParser>();
        networkManager = GameObject.FindObjectOfType<NetworkManager>();
        Debug.Log("[SIM] floatingPanel: " + (floatingPanel != null) + ", amrTaskParser: " + (amrTaskParser != null));

        Debug.Log("[SIM] 2. ASRS init...");
        asrs = GameObject.FindObjectOfType<ASRS>();
        Debug.Log("[SIM] asrs: " + (asrs != null));
        if (asrs != null) asrs.Initialize();

        Debug.Log("[SIM] 3. DataManager init...");
        datamanager = GameObject.FindObjectOfType<DataManager>();
        Debug.Log("[SIM] datamanager: " + (datamanager != null));
        if (datamanager != null) datamanager.Init();


        List<string> tmpAMRID = new List<string>();
        List<AMR> tmpAMR = new List<AMR>();
        tmpAMR.AddRange( FindObjectsOfType<AMR>());
        foreach (var item in tmpAMR)
        {
            tmpAMRID.Add(item.id);
        }

        List<string> tmpASRSID = new List<string>();
        List<Storage> tmpASRS = new List<Storage>();
        tmpASRS.AddRange(FindObjectsOfType<Storage>());
        foreach (var item in tmpASRS)
        {
            tmpASRSID.Add(item.id);
        }

        tmpAMRID.Sort();
        tmpASRSID.Sort();

        if (floatingPanel != null)
        {
            floatingPanel.CreateAMRPopup(tmpAMRID);
            floatingPanel.CreateASRSPopup(tmpASRSID);
            floatingPanel.CreateVMSPopup("1");
            floatingPanel.CreateULDPopup("1");
        }

        InitCargoData();

        networkManager.PostCreateULD("SCA", null, null, true);

        // 자동 시뮬레이션 시작 (UI 우회, 2초 후 실행)
        Invoke("Igniter", 2f);

        // Watchdog: 시뮬레이션이 멈추면 자동 재시동
        StartCoroutine(SimulationWatchdog());
    }

    private IEnumerator SimulationWatchdog()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            yield return new WaitForSeconds(1f);

            // === 상태 덤프 ===
            Debug.Log("[WATCHDOG] === 상태 ===" +
                " cargoSpawn=" + cargoSpawnQueue.Count +
                " VMSqueue=" + VMStoASRSCargoQueue.Count +
                " asrsWait=" + asrsWaitingCargo.Count +
                " settlement=" + onULDSettlementCargo.Count +
                " ULD=" + uldLoadedCount + "/" + uldExpectedCount +
                " ASRS_cap=" + asrs.capacity +
                " InFork=" + (InForkLift != null ? InForkLift.isMoving : false) +
                " OutFork=" + (OutForkLift != null ? OutForkLift.isMoving : false) +
                " pendW=" + pendingWaitingToEntry +
                " pendE=" + pendingEntryToASRS +
                "\n  [AMR입고] toASRS=" + amrTaskParser.AMR_VMStoASRS_Queue.Count +
                " waitToVMS=" + amrTaskParser.AMR_VMSWaitingToVMS_Queue.Count +
                "\n  [AMR출고] toWork=" + amrTaskParser.AMR_ASRStoWorkStation_Queue.Count +
                " waitToEntry=" + amrTaskParser.AMR_WaitingToEntry_Queue.Count +
                " entryToASRS=" + amrTaskParser.AMR_EntryToASRS_Queue.Count +
                "\n  [ASRS] " + asrs.GetStatusDump());

            // === 입고 복구 ===
            // VMS 컨베이어에 화물이 없고, InForkLift가 idle일 때만 다음 화물 투입
            bool inForkBusy = InForkLift != null && InForkLift.isMoving;
            bool vmsHasCargo = (InForkLift != null && InForkLift.vmsConvyor != null) ? InForkLift.vmsConvyor.hasCargo : (vmsInConveyor != null && vmsInConveyor.hasCargo);
            if (cargoSpawnQueue.Count > 0 && !inForkBusy && !vmsHasCargo)
            {
                Debug.Log("[WATCHDOG] → TruckArrive 재시동");
                TruckArrive();
            }
            if (VMStoASRSCargoQueue.Count > 0 && amrTaskParser.AMR_VMStoASRS_Queue.Count > 0)
            {
                Debug.Log("[WATCHDOG] → 대기 화물 AMR 디스패치");
                amrTaskParser.Order_VMStoASRS(VMStoASRSCargoQueue.Dequeue().gameObject);
            }
            if (amrTaskParser.AMR_VMSWaitingToVMS_Queue.Count > 0 && amrTaskParser.AMR_VMStoASRS_Queue.Count == 0)
            {
                Debug.Log("[WATCHDOG] → VMSWaiting AMR 복귀");
                amrTaskParser.Order_VMSWaitingtoVMS(null);
            }

            // === 출고 AMR 펌핑: idle AMR을 toWork까지 순환시킴 ===
            if (amrTaskParser.AMR_ASRStoWorkStation_Queue.Count == 0)
            {
                // entryToASRS에 AMR 있으면 → ASRS로 보내서 → toWork에 복귀시킴
                if (amrTaskParser.AMR_EntryToASRS_Queue.Count > 0)
                {
                    Debug.Log("[WATCHDOG] → EntryToASRS AMR → ASRS 이동 (toWork 복귀 위해)");
                    amrTaskParser.Order_ASRSEntryToASRS(null);
                }
                // waitToEntry에 AMR 있으면 → Entry로 보냄 (다음 watchdog에서 ASRS로 감)
                else if (amrTaskParser.AMR_WaitingToEntry_Queue.Count > 0)
                {
                    Debug.Log("[WATCHDOG] → WaitingToEntry AMR → Entry 이동");
                    amrTaskParser.Order_WaitingToASRSEntry(null);
                }
            }

            // === 출고 복구 ===
            if (asrsWaitingCargo.Count > 0 && amrTaskParser.AMR_ASRStoWorkStation_Queue.Count > 0)
            {
                Debug.Log("[WATCHDOG] → ASRS 대기 화물 디스패치");
                amrTaskParser.Order_ASRStoULDSettlement(asrsWaitingCargo.Dequeue());
            }
            if (pendingWaitingToEntry > 0 && amrTaskParser.AMR_WaitingToEntry_Queue.Count > 0)
            {
                Debug.Log("[WATCHDOG] → 밀린 WaitingToEntry");
                pendingWaitingToEntry--;
                amrTaskParser.Order_WaitingToASRSEntry(null);
            }
            if (pendingEntryToASRS > 0 && amrTaskParser.AMR_EntryToASRS_Queue.Count > 0)
            {
                Debug.Log("[WATCHDOG] → 밀린 EntryToASRS");
                pendingEntryToASRS--;
                amrTaskParser.Order_ASRSEntryToASRS(null);
            }
            // OutForkLift 타임아웃 복구 (30초 이상 isMoving이면 강제 리셋)
            if (OutForkLift != null && OutForkLift.isMoving)
            {
                outForkLiftStuckTimer += 1f;
                if (outForkLiftStuckTimer > 30f)
                {
                    Debug.LogWarning("[WATCHDOG] OutForkLift 타임아웃 → isMoving 강제 리셋");
                    if (OutForkLift != null) OutForkLift.isMoving = false;
                    outForkLiftStuckTimer = 0f;
                }
            }
            else { outForkLiftStuckTimer = 0f; }

            if (onULDSettlementCargo.Count > 0 && !(OutForkLift != null && OutForkLift.isMoving))
            {
                Debug.Log("[WATCHDOG] → 정산 대기 화물 ULD 적재");
                ULDInputOrder(0, null);
            }

            // InForkLift 타임아웃 복구
            if (InForkLift != null && InForkLift.isMoving)
            {
                inForkLiftStuckTimer += 1f;
                if (inForkLiftStuckTimer > 30f)
                {
                    Debug.LogWarning("[WATCHDOG] InForkLift 타임아웃 → isMoving 강제 리셋");
                    if (InForkLift != null) InForkLift.isMoving = false;
                    inForkLiftStuckTimer = 0f;
                }
            }
            else { inForkLiftStuckTimer = 0f; }

            // === Stacker 킥스타트 ===
            asrs.KickStacker();

            // === 출고 파이프라인 stall 감지 ===
            // ULD 적재 진행 중인데 파이프라인이 완전히 비었으면 재트리거
            if (uldExpectedCount > 0 && uldLoadedCount < uldExpectedCount
                && asrsWaitingCargo.Count == 0 && onULDSettlementCargo.Count == 0
                && !(OutForkLift != null && OutForkLift.isMoving))
            {
                // Stacker도 안 돌고 있으면 출고를 다시 시도
                string asrsStatus = asrs.GetStatusDump();
                if (asrsStatus.Contains("stackerWorking=False") && asrsStatus.Contains("pullOrder=0"))
                {
                    Debug.Log("[WATCHDOG] → 출고 파이프라인 stall 감지, 재트리거 (loaded=" + uldLoadedCount + "/" + uldExpectedCount + ")");
                    uldExpectedCount = 0;
                    uldLoadedCount = 0;
                    pullQueue.Clear();
                    LocalTriggerOutput();
                }
            }

            // === ULD 사이클 완료 체크 및 재시작 ===
            bool outputIdle = (uldExpectedCount == 0 || uldLoadedCount >= uldExpectedCount);
            if (networkManager.offlineMode && outputIdle && asrs.capacity >= outputBatchSize)
            {
                Debug.Log("[WATCHDOG] → 출고 트리거 (capacity=" + asrs.capacity + ")");
                LocalTriggerOutput();
            }
        }
    }

    public static SimulationModeTaskManager Instance
    {
        get
        {
            return simulationModeTaskManagerInstance;
        }
    }

    public void ResetCurrentULD()
    {
        ULD tmpULD = GameObject.FindObjectOfType<ULD>();
        if (tmpULD != null)
        {
            for (int i = 0; i < tmpULD.cargos.Count; i++)
                Destroy(tmpULD.cargos[i]);
            tmpULD.cargos.Clear();
            tmpULD.cargoPos.Clear();
        }
        currentLoadWeight = 0f;
        ULDFullFlag = false;

        networkManager.PostCreateULD("SCA", null, null, true);
        Debug.Log("[SIM] ULD 리셋 완료");
    }

    public void ContinueAfterULDFull()
    {
        ResetCurrentULD();
        pullQueue.Clear();
        uldLoadedCount = 0;
        uldExpectedCount = 0;

        // ASRS에 화물이 남아있으면 다시 출고
        if (asrs.capacity >= outputBatchSize)
        {
            Debug.Log("[SIM] ULD 리셋 후 → 다시 출고 시작");
            LocalTriggerOutput();
        }
    }

    public void CallNetwork()
    {
        networkManager.CheckConnection();
    }

    public void CallReqeustPSResult()
    {
        networkManager.Post_StartSimulation(currentJobULDCode, true);

    }

    public void Igniter()
    {
        Debug.Log("[SIM] Igniter called. cargoSpawnQueue: " + cargoSpawnQueue.Count);
        Debug.Log("[SIM] InForkLift: " + (InForkLift != null) + ", forkLiftWaypointManager: " + (forkLiftWaypointManager != null));
        //truck.DriveToRallyPoint();
        TruckArrive();

    }

    // ForkLift 없는 모드용 VMS 컨베이어 참조
    private Conveyor vmsInConveyor;

    public void TruckArrive()
    {
        if (cargoSpawnQueue.Count == 0) return;

        // ForkLift 있는 경우 (원본 로직)
        if (InForkLift != null)
        {
            if (InForkLift.isMoving) return;
            Cargo tmpCargo = cargoSpawnQueue.Dequeue();
            generatedCargo.Add(tmpCargo);
            InForkLift.PullCargoToVMS(forkLiftWaypointManager.InputForkLiftPath[0].transform.position, tmpCargo.gameObject);
            if (cargoSpawnQueue.Count == 0) cargoExhausted = true;
            return;
        }

        // ForkLift 없는 경우: VMS 컨베이어에 직접 스폰
        if (vmsInConveyor == null)
        {
            var allConveyors = FindObjectsOfType<Conveyor>();
            foreach (var c in allConveyors)
                if (c.conveyorType == ConveyorType.VMSIn) { vmsInConveyor = c; break; }
        }
        if (vmsInConveyor == null || vmsInConveyor.hasCargo) return;

        Cargo cargo = cargoSpawnQueue.Dequeue();
        generatedCargo.Add(cargo);
        cargo.transform.position = vmsInConveyor.transform.position + new Vector3(0f, 0.25f, 0f);
        cargo.isFlowable = true;
        Rigidbody rb = cargo.GetComponent<Rigidbody>();
        if (rb != null) rb.MovePosition(cargo.transform.position);
        if (cargoSpawnQueue.Count == 0) cargoExhausted = true;
    }

    public void ForkLiftArriveAtTruck()
    {
    
    }

    public void ForkLiftArriveAtVMS()
    {
        //InForkLift.ForkArmDown(null);
    }    

    public void ForkLiftUpEnded()
    {
        InForkLift.MoveAtPos(inputForkLiftUnloadLocation);
    }

    public void ForkLiftUnloadToVMSEnded(GameObject unloaedCargo)
    {
        if (unloaedCargo == null) { Debug.LogError("[SIM] ForkLiftUnloadToVMSEnded: cargo is null!"); return; }
        Cargo cargo = unloaedCargo.GetComponent<Cargo>();
        cargo.isFlowable = true;
        Debug.Log("[SIM] ForkLift unloaded cargo: " + cargo.cargoID + ", isFlowable=true");

        // 컨베이어 트리거가 안 잡혔으면 수동으로 VMS 컨베이어에 등록
        StartCoroutine(EnsureCargoOnConveyor(cargo));
    }

    private IEnumerator EnsureCargoOnConveyor(Cargo cargo)
    {
        // 물리엔진이 트리거 감지할 시간 부여
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (cargo == null || cargo.beltOccupy.Count > 0) yield break;

        // 트리거가 안 잡힘 → InForkLift의 vmsConveyor에 수동 등록
        Conveyor vmsConveyor = InForkLift.vmsConvyor;
        if (vmsConveyor == null) yield break;

        Debug.LogWarning("[SIM] 컨베이어 트리거 미감지 → 수동 등록: " + cargo.cargoID);

        // 화물 위치를 컨베이어 위로 맞춤
        cargo.transform.position = vmsConveyor.transform.position + new Vector3(0f, 0.25f, 0f);

        // Rigidbody가 있으면 위치를 물리엔진에 동기화
        Rigidbody rb = cargo.GetComponent<Rigidbody>();
        if (rb != null) rb.MovePosition(cargo.transform.position);

        // 2프레임 더 대기 후에도 안 잡히면 직접 컨베이어에 세팅
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (cargo.beltOccupy.Count > 0) yield break;

        Debug.LogWarning("[SIM] 강제 컨베이어 세팅: " + cargo.cargoID);
        cargo.AddQueue(vmsConveyor);
        vmsConveyor.movingCargo = cargo;
        vmsConveyor.hasCargo = true;
    }

    public void CargoOutCell(GameObject cargo)
    {
        //amrTaskParser.Order_ASRStoULDSettlement(cranecargo);
    }

    public Queue<Cargo> VMStoASRSCargoQueue = new Queue<Cargo>();

    public void ConveyorJobReport(Conveyor reportedConveyor, GameObject onBeltCargo)
    {
        Debug.Log("[SIM] ConveyorJobReport: " + reportedConveyor.conveyorType + " cargo=" + (onBeltCargo != null ? onBeltCargo.GetComponent<Cargo>().cargoID : "null"));
        switch(reportedConveyor.conveyorType)
        {
            case ConveyorType.VMSIn:
                Debug.Log("[SIM] VMSIn passed");
                break;

            case ConveyorType.VMSOut:
                Debug.Log("[SIM] VMSOut: AMR_VMStoASRS_Queue=" + amrTaskParser.AMR_VMStoASRS_Queue.Count);
                if(amrTaskParser.AMR_VMStoASRS_Queue.Count == 0)
                {
                    Debug.Log("[SIM] No AMR available, queuing cargo");
                    VMStoASRSCargoQueue.Enqueue(onBeltCargo.GetComponent<Cargo>());
                }
                else
                {
                    Debug.Log("[SIM] Dispatching AMR for VMStoASRS");
                    amrTaskParser.Order_VMStoASRS(onBeltCargo);
                }
                if (cargoSpawnQueue.Count != 0)
                {
                    TruckArrive();
                }

                onBeltCargo.GetComponent<Cargo>().isFlowable = false;
                break;

            case ConveyorType.ASRSIn:
                if (VMStoASRSCargoQueue.Count != 0  && amrTaskParser.AMR_VMStoASRS_Queue.Count != 0)
                {
                    amrTaskParser.Order_VMStoASRS(VMStoASRSCargoQueue.Dequeue().gameObject);
                }
                break;

            case ConveyorType.ASRSOut:
                // place stacker do next order 
                break;

            default:

                break;


        }
        
    }

    public void AMRJobEndReport(AMR reportAMR, GameObject amrCargo)
    {
        Debug.Log("[SIM] AMRJobEndReport: " + reportAMR.id + " task=" + reportAMR.currentTask);
        switch (reportAMR.currentTask)
        {
            case AMRTask.VMSToASRS:
                amrCargo.GetComponent<Cargo>().isFlowable = true;
                amrCargo.transform.SetParent(null);
                // 대기 AMR이 있으면 VMS로 보냄
                amrTaskParser.Order_VMSWaitingtoVMS(null);
                // 현재 AMR은 항상 대기 구역으로 복귀
                amrTaskParser.Order_ASRStoVMSWaitingZone(reportAMR);
                asrs.InputStorage(amrCargo.GetComponent<Cargo>());
                break;

            case AMRTask.ASRSToVMSWaiting:
                amrTaskParser.AMR_VMSWaitingToVMS_Queue.Enqueue(reportAMR);
                Debug.Log("[SIM] AMR_VMSWaitingToVMS_Queue 투입. Count=" + amrTaskParser.AMR_VMSWaitingToVMS_Queue.Count);
                // VMStoASRS 큐가 비어있으면 바로 VMS로 복귀 (watchdog 안 기다림)
                if (amrTaskParser.AMR_VMStoASRS_Queue.Count == 0)
                {
                    amrTaskParser.Order_VMSWaitingtoVMS(null);
                }
                break;

            case AMRTask.VMSWatingToVMS:
                amrTaskParser.AMR_VMStoASRS_Queue.Enqueue(reportAMR);
                Debug.Log("[SIM] AMR_VMStoASRS_Queue 투입. Count=" + amrTaskParser.AMR_VMStoASRS_Queue.Count + ", 대기화물=" + VMStoASRSCargoQueue.Count);

                if ( VMStoASRSCargoQueue.Count != 0)
                {
                    amrTaskParser.Order_VMStoASRS(VMStoASRSCargoQueue.Dequeue().gameObject);
                }
                break;

            case AMRTask.ASRSToULDSettlment:
                amrCargo.transform.SetParent(null);
                onULDSettlementCargo.Enqueue(amrCargo);
                amrTaskParser.Order_ULDSettlementToWaiting(reportAMR);
                // 큐에 AMR이 없으면 pending으로 저장, 복귀 시 재시도
                if (!amrTaskParser.Order_WaitingToASRSEntry(null))
                    pendingWaitingToEntry++;
                if (!amrTaskParser.Order_ASRSEntryToASRS(null))
                    pendingEntryToASRS++;
                // OutForkLift가 안 바쁠 때만 ULD 적재
                if (onULDSettlementCargo.Count > 0 && !(OutForkLift != null && OutForkLift.isMoving))
                {
                    ULDInputOrder(0, null);
                }
                break;

            case AMRTask.WaitingToASRSEntry:
                amrTaskParser.AMR_EntryToASRS_Queue.Enqueue(reportAMR);
                Debug.Log("[SIM] AMR_EntryToASRS_Queue 투입. Count=" + amrTaskParser.AMR_EntryToASRS_Queue.Count);
                // 밀린 EntryToASRS 디스패치 재시도
                if (pendingEntryToASRS > 0)
                {
                    pendingEntryToASRS--;
                    amrTaskParser.Order_ASRSEntryToASRS(null);
                }
                break;

            case AMRTask.EntryToASRS:
                amrTaskParser.AMR_ASRStoWorkStation_Queue.Enqueue(reportAMR);
                Debug.Log("[SIM] AMR_ASRStoWorkStation_Queue 투입. Count=" + amrTaskParser.AMR_ASRStoWorkStation_Queue.Count);
                if ( asrsWaitingCargo.Count != 0)
                {
                    amrTaskParser.Order_ASRStoULDSettlement(asrsWaitingCargo.Dequeue());
                }
                break;

            case AMRTask.ULDSettlementToWaiting:
                amrTaskParser.AMR_WaitingToEntry_Queue.Enqueue(reportAMR);
                Debug.Log("[SIM] AMR_WaitingToEntry_Queue 투입. Count=" + amrTaskParser.AMR_WaitingToEntry_Queue.Count);
                // 밀린 WaitingToASRSEntry 디스패치 재시도
                if (pendingWaitingToEntry > 0)
                {
                    pendingWaitingToEntry--;
                    amrTaskParser.Order_WaitingToASRSEntry(null);
                }
                break;
            default:
                break;
        }
    }

    public void ForkLiftJobreport(ForkLift reportLift, GameObject cargo)
    {
        if (cargo == null) { Debug.LogError("[SIM] ForkLiftJobreport: cargo is null!"); return; }

        var cargoModel = cargo.GetComponentInChildren<Tag_CargoModel>();
        if (cargoModel != null) cargoModel.transform.localPosition = Vector3.zero;

        if (cargo.GetComponentInChildren<Tag_Pallet>() != null)
            cargo.GetComponentInChildren<Tag_Pallet>().gameObject.SetActive(false);

        if ( cargo.GetComponentInChildren<Tag_SKID>() != null)
            cargo.GetComponentInChildren<Tag_SKID>().gameObject.SetActive(false);

        cargo.transform.SetParent(currentJobULD.GetComponentInChildren<Tag_ULDPivot>().transform);
        ULD tmpULD = GameObject.FindObjectOfType<ULD>();
        

        Cargo tmpCargo = cargo.GetComponent<Cargo>();

        Vector3 tmpLocation = tmpCargo.finalPosition;
        tmpULD.AddNewCargo(cargo, tmpCargo.finalPosition);

        tmpLocation.x += tmpCargo.width / 200f;
        tmpLocation.y += tmpCargo.depth / 200f;
        tmpLocation.z -= tmpCargo.length / 200f;

        cargo.transform.localScale = new Vector3(tmpCargo.width/100f , tmpCargo.depth/100f, tmpCargo.length/100f);

        cargo.transform.localPosition = tmpLocation;
        cargo.transform.SetLocalPositionAndRotation(tmpLocation, Quaternion.Euler(Vector3.zero));

        currentLoadWeight += tmpCargo.waterVolume;

        //FindObjectOfType<Popup_016>().UpdateData(currentLoadWeight / ULD_LOAD_SPEC, 0);

        networkManager.PostULDInCargo(tmpCargo.cargoID, tmpCargo.finalPosition, currentJobULDId);

        uldLoadedCount++;
        Debug.Log("[SIM] ULD 적재 " + uldLoadedCount + "/" + uldExpectedCount);

        // 다음 정산 대기 화물이 있으면 이어서 ULD 적재
        if (onULDSettlementCargo.Count > 0)
        {
            ULDInputOrder(0, null);
        }
        else if (uldExpectedCount > 0 && uldLoadedCount >= uldExpectedCount)
        {
            Debug.Log("[SIM] ULD 배치 완료 → 리셋 후 계속");
            UIManager.Instance.UpdateMainUI((float)uldLoadedCount / uldExpectedCount);
            Invoke("ContinueAfterULDFull", 2f);
        }
        
    }

    public void TestULDInput()
    {
        GameObject tmp = asrs.FindCargo(pullQueue.Dequeue()).cargo.gameObject;

        Cargo cargo = tmp.GetComponent<Cargo>(); 

        cargo.transform.SetParent(currentJobULD.transform);
        cargo.transform.localPosition = cargo.GetComponent<Cargo>().finalPosition;
        cargo.transform.eulerAngles = Vector3.zero;
    }

    public void StackerJobReport(Cargo outputCargo)
    {
        if (outputCargo == null) { Debug.LogError("[SIM] StackerJobReport: cargo is null!"); return; }
        Debug.Log("[SIM] StackerJobReport: " + outputCargo.cargoID + " toWork=" + amrTaskParser.AMR_ASRStoWorkStation_Queue.Count);

        if ( amrTaskParser.AMR_ASRStoWorkStation_Queue.Count == 0)
        {
            asrsWaitingCargo.Enqueue(outputCargo);
        }
        else
        {
            amrTaskParser.Order_ASRStoULDSettlement(outputCargo);
        }

        
    }


    public void ULDInputOrder(int settlementIndex, GameObject cargo)
    {
        if (onULDSettlementCargo.Count == 0)
        {
            return;
        }
        if (OutForkLift != null && OutForkLift.isMoving)
        {
            return;
        }

        if (OutForkLift != null)
        {
            OutForkLift.waypoints = forkLiftWaypointManager.OutputForkLiftPath;
            OutForkLift.PullCargoToULD(settlementCargoIndex, onULDSettlementCargo.Dequeue());
        }
        else
        {
            // OutForkLift 없음: 화물 바로 파괴
            GameObject outCargo = onULDSettlementCargo.Dequeue();
            if (outCargo != null) Destroy(outCargo);
            uldLoadedCount++;
            if (uldExpectedCount > 0 && uldLoadedCount >= uldExpectedCount)
                Invoke("ContinueAfterULDFull", 2f);
        }

        settlementCargoIndex++;
        settlementCargoIndex %= 4;
    }

    /// <summary>
    /// 오프라인 모드: 서버 없이 ASRS에 저장된 화물을 꺼내서 ULD로 출고
    /// </summary>
    [Header("Output Settings")]
    [Range(1, 18)]
    public int outputBatchSize = 5;

    public void LocalTriggerOutput()
    {
        Debug.Log("[SIM] LocalTriggerOutput: ASRS capacity=" + asrs.capacity);

        int pullCount = 0;
        float xPos = 0f;
        float yPos = 0f;
        float zPos = 0f;

        foreach (var cell in asrs.cellList)
        {
            if (cell.hasCargo && cell.cargo != null && pullCount < outputBatchSize)
            {
                // 간단한 격자 배치 (서버 대신 로컬 계산)
                int col = pullCount % 3;
                int row = pullCount / 3;
                xPos = col * (cell.cargo.width / 100f);
                yPos = row * (cell.cargo.depth / 100f);
                zPos = 0f;

                cell.cargo.finalPosition = new Vector3(xPos, yPos, zPos);

                pullQueue.Enqueue(cell.cargo.cargoID);
                asrs.PullStorage(cell.cargo.cargoID);
                pullCount++;
            }
        }

        if (pullCount > 0)
        {
            uldExpectedCount = pullCount;
            uldLoadedCount = 0;
            ULDFullFlag = false;
            Debug.Log("[SIM] 출고 명령: " + pullCount + "개 화물");
        }
        else
        {
            Debug.Log("[SIM] 출고할 화물 없음, ULD 리셋");
            ResetCurrentULD();
        }
    }

    public void ParseSimulationData(ResultInfo result)
    {
        List<AWBInfo> awbList = new List<AWBInfo>();
        Cell tmpCell; 
        awbList = result.AWBInfoList;
        Vector3 tmpVector = new Vector3();
        ULDFullFlag = result.isDone;

        

        foreach (var item in awbList)
        {
            if ( asrs.FindCargo(item.name) != null)
            {
                pullQueue.Enqueue(item.name);
                tmpCell = asrs.FindCargo(item.name);
                tmpVector.x = float.Parse(item.coordinate[0].p1x)/100f;
                tmpVector.y = float.Parse(item.coordinate[0].p1y)/100f;
                tmpVector.z = float.Parse(item.coordinate[0].p1z)/100f;
                tmpCell.cargo.finalPosition = tmpVector;
                asrs.PullStorage(item.name);
                instantQueue.Enqueue(tmpCell.cargo);
            }
            
        }

        Debug.Log("pull command Count" + instantQueue.Count);

        if ( instantQueue.Count == 0)
        {
            //ULDFullFlag = false;
            ResetCurrentULD();
        }

    }

    public void InitCargoData()
    {
        DataManager dataManager = GameManager.FindAnyObjectByType<DataManager>();
        List<string> headers = new List<string>();
        dataParsing = dataManager.ReadCopy(null, out headers);

        for ( int i = 0; i < dataParsing.Count; i++)
        //for (int i = 0; i < 3; i++)
        {
            GameObject tmp = Instantiate(testCargo);

            // Rigidbody가 있어야 컨베이어의 OnTriggerEnter가 발동됨
            Rigidbody rb = tmp.GetComponent<Rigidbody>();
            if (rb == null) rb = tmp.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            Cargo tmpCargo = tmp.AddComponent<Cargo>();

            tmpCargo.cargoID = dataParsing[i]["name"].ToString();
            tmpCargo.cargoName = dataParsing[i]["name"].ToString();
            tmpCargo.POU = dataParsing[i]["POU"].ToString();
            tmpCargo.width = (int.Parse(dataParsing[i]["width"].ToString()));
            tmpCargo.length = (int.Parse(dataParsing[i]["length"].ToString()));
            tmpCargo.depth = (int.Parse(dataParsing[i]["depth"].ToString()));
            tmpCargo.waterVolume = (float.Parse(dataParsing[i]["waterVolume"].ToString()));

            if ( int.TryParse(dataParsing[i]["weight"].ToString(), out var value))
            {
                tmpCargo.weight = (float)value;
            }
            else if (float.TryParse(dataParsing[i]["weight"].ToString(), out var fValue))
            {
                tmpCargo.weight = fValue;
            }

            tmpCargo.SCCs.AddRange(dataParsing[i]["SCCs"].ToString().Split('+'));
            tmpCargo.trackingEffect = tmpCargo.gameObject.transform.GetChild(2).GetComponent<Dissolver>();
            tmpCargo.transform.localScale = Vector3.one;
            cargoSpawnQueue.Enqueue(tmpCargo);

            
        }

        // ASRS 초기 화물 없이 시작 (initaialCargoRatio 무시)
        int sceneCellCount = 0;

        for (int i = 0; i < sceneCellCount; i++)
        {
            Cell tmpcell = asrs.storages[i % 2].cells[i / 2];
            tmpcell.cargo = cargoSpawnQueue.Dequeue();
            tmpcell.cargo.transform.SetParent(tmpcell.transform);
            tmpcell.cargo.transform.localPosition = Vector3.zero;
            tmpcell.cargo.currentLocation = tmpcell.cellIndex.ToString();

            tmpcell.PutCargo(tmpcell.cargo);
            tmpcell.isBooked = true;
            tmpcell.hasCargo = true;

            asrs.capacity++;

            //VMSAwbInfo vmsAwb = new VMSAwbInfo(
            //    tmpcell.cargo.cargoID,
            //    "DTTestPrefab",
            //    tmpcell.cargo.waterVolume,
            //    1,
            //    tmpcell.cargo.width,
            //    tmpcell.cargo.length,
            //    tmpcell.cargo.depth,
            //    tmpcell.cargo.weight,
            //    1,
            //    "saved",
            //    0,
            //    " ",
            //    true,
            //    "DTTestPath",
            //    1,
            //    "��ۼ���",
            //    tmpcell.cargo.SCCs.ToArray(),
            //    21);

            //string json = JsonUtility.ToJson(vmsAwb);
            //networkManager.PostVMSAwb(json);

            //networkManager.Post_CellCargoUpdate(tmpcell.cargo, true);

            //UIManager.Instance.UpdateASRSCellData(tmpcell.cellIndex.ToString(), true, "123123", "PPP", 1);
        }

    }

}
