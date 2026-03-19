using PGD;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WSH;

public enum StackerJobType
{
    MoveInputForPull,
    PullCargoFromInput, 
    MoveTargetCellForPush,
    PushCargoToCell,
    BackToOrigin,
    MoveTargetCellForPull,
    PullCargoFromCell,
    MoveOutputForPush,
    PushCargoToOutput,
}

public class ASRS : MonoBehaviour
{
    public List<Storage> storages = new List<Storage>();
    StackerCrane stacker;
    Queue<string> pullOrder = new Queue<string>();
    Queue<Cargo> pushOrder = new Queue<Cargo>();

    public int capacity;

    public Conveyor asrsInConveyor;
    public Conveyor asrsOutConveyor;

    GameObject OutputSettlement;

    public List<Cell> cellList = new List<Cell>();

    // Start is called before the first frame update
    void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void Initialize()
    {
        OutputSettlement = transform.Find("ASRSOut").gameObject;

        storages.AddRange(GetComponentsInChildren<Storage>());

        foreach (var item in storages)
        {
            item.Initialize();
        }

        foreach (var item in storages)
        {
            cellList.AddRange(item.cells);
        }

        if (UIManager.Instance != null) UIManager.Instance.CreateCellPopup(cellList);

        stacker = GameObject.FindObjectOfType<StackerCrane>();

        asrsInConveyor = GetComponentInChildren<Tag_ASRSIn>().GetComponent<Conveyor>();
        asrsOutConveyor = GetComponentInChildren<Tag_ASRSOut>().GetComponent<Conveyor>();

        stacker.Initialize(asrsInConveyor, asrsOutConveyor, this);

    }
    public void InputStorage(Cargo inputCargo)
    {
        int cellIndex = int.MaxValue;
        Cell findCell = null;
        bool findEmptyCell;

        findEmptyCell = false;


        foreach (var item in storages)
        {
            if ( item.FindEmptyCell(out var cell))
            {
                if ( cell.cellIndex < cellIndex)
                {
                    cellIndex = cell.cellIndex;
                    findCell = cell;
                    findEmptyCell = true;
                }
            }
        }

        if (stacker.isWorking)
        {
            pushOrder.Enqueue(inputCargo);
        }
        else if (!stacker.isWorking)
        {
            if ( findEmptyCell)
            {
                stacker.Commnad_Push(findCell, inputCargo);
            }
            else if (!findEmptyCell)
            {
                pushOrder.Enqueue(inputCargo);
            }
        }

    }

    public void PullStorage(string cargoID)
    {
        if (SimulationModeTaskManager.Instance.instantMode)
            return;

        if (stacker.isWorking)
        {
            pullOrder.Enqueue(cargoID);
        }
        else if (!stacker.isWorking)
        {
            Cell tmpTargetCell =  FindCargo(cargoID);
            Cargo tmpTargetCargo = tmpTargetCell.cargo;

            stacker.Command_Pull(tmpTargetCell, tmpTargetCargo);
        }
    }


    public Cell FindCargo(string cargoID)
    {
        foreach ( var item in storages)
        {
            if ( item.FindCellIDByCargoID(cargoID, out Cell foundedCell))
            {

                return foundedCell;
            }

        }

        return null;
    }

    public void ReportStacker(StackerJobType currentJobType)
    {
        stacker.isWorking = false;
        switch(currentJobType)
        {
            case StackerJobType.PushCargoToOutput:
                capacity--;
                // 셀에서 화물 꺼냈으므로 셀 상태 클리어
                if (stacker.targetCell != null && stacker.targetCell.hasCargo)
                {
                    stacker.targetCell.PullCargo();
                    Debug.Log("[ASRS] Cell " + stacker.targetCell.cellIndex + " → 출고 (PullCargo)");
                }
                // movingCargo가 null일 수 있으므로 (OnTriggerEnter 미발동) 대체 경로로 화물 찾기
                Cargo outCargo = asrsOutConveyor.movingCargo;
                if (outCargo == null)
                    outCargo = asrsOutConveyor.GetComponentInChildren<Cargo>();
                if (outCargo == null && stacker.targetCell != null)
                    outCargo = stacker.targetCell.cargo;
                if (outCargo == null)
                {
                    Debug.LogError("[ASRS] PushCargoToOutput: 출력 화물을 찾을 수 없음!");
                    break;
                }
                Debug.Log("[ASRS] PushCargoToOutput: " + outCargo.cargoID);
                SimulationModeTaskManager.Instance.StackerJobReport(outCargo);
                SimulationModeTaskManager.Instance.networkManager.Post_CellCargoUpdate(outCargo, false);

                break;
            case StackerJobType.PushCargoToCell:

                capacity++;

                // Stacker가 셀에 화물을 놓은 후 Cell의 상태를 업데이트
                Cargo placedCargo = stacker.targetCell.GetComponentInChildren<Cargo>();
                if (placedCargo != null && !stacker.targetCell.hasCargo)
                {
                    stacker.targetCell.PutCargo(placedCargo);
                    Debug.Log("[ASRS] Cell " + stacker.targetCell.cellIndex + " ← 화물 " + placedCargo.cargoID);
                }

                SimulationModeTaskManager.Instance.networkManager.Post_CellCargoUpdate(stacker.targetCell.cargo, true);

                if ( capacity == 18 || SimulationModeTaskManager.Instance.cargoExhausted)
                {
                    if (SimulationModeTaskManager.Instance.networkManager.offlineMode)
                    {
                        // 오프라인: 로컬에서 출고 트리거
                        SimulationModeTaskManager.Instance.LocalTriggerOutput();
                    }
                    else
                    {
                        SimulationModeTaskManager.Instance.CallReqeustPSResult();
                    }
                    SimulationModeTaskManager.Instance.cargoExhausted = false;
                }

                break;

            case StackerJobType.BackToOrigin:
                stacker.isWorking = false;
                break;

            default: 
                break;
        }

        CheckOtherCraneJob();

    }

    public void CheckOtherCraneJob()
    {
        if (pullOrder.Count != 0)
        {
            Debug.Log("[ASRS] CheckOtherCraneJob: pullOrder=" + pullOrder.Count + " → PullStorage");
            PullStorage(pullOrder.Dequeue());

            return;
        }
        else if (pullOrder.Count == 0)
        {
            if (SimulationModeTaskManager.Instance.ULDFullFlag)
            {
                SimulationModeTaskManager.Instance.ResetCurrentULD();
            }

            if ( pushOrder.Count != 0)
            {
                Debug.Log("[ASRS] CheckOtherCraneJob: pushOrder=" + pushOrder.Count + " → InputStorage");
                InputStorage(pushOrder.Dequeue());
            }

            return;
        }
    }

    /// <summary>
    /// Stacker가 idle인데 대기 작업이 있으면 킥스타트
    /// </summary>
    public void KickStacker()
    {
        if (!stacker.isWorking && (pullOrder.Count > 0 || pushOrder.Count > 0))
        {
            Debug.Log("[ASRS] KickStacker: stacker idle, pullOrder=" + pullOrder.Count + " pushOrder=" + pushOrder.Count);
            CheckOtherCraneJob();
        }
    }

    public string GetStatusDump()
    {
        return "pullOrder=" + pullOrder.Count + " pushOrder=" + pushOrder.Count + " stackerWorking=" + stacker.isWorking;
    }
}
