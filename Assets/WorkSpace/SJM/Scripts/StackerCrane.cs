using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackerCrane : MonoBehaviour
{
    public GameObject craneBody;
    public GameObject storage;
    public GameObject cargo;
    public Transform storageLocation;
    public Transform outLocation;
    public float originHeight;
    float cargoHeight;
    public Cell targetCell;
    public float speed_LeftRight;
    public float speed_UpDown;
    public float speed_Put;
    Vector3 origin;
    public bool mode;
    bool arriveZ;
    bool arriveUp;
    float currentMoveSpeed;

    private bool internalBool;

    int isActive;
    



    //-----//

    float spec_LiftHorizontalSpeed;
    float spec_LiftVerticalSpeed;
    float spec_LiftPullPushSpeed; 

    event Action operate;
    Vector3 targetPosition;
    
    Vector3 inputSettlementPosition;
    Vector3 outputSettlementPosition;
    Vector3 startPosition;
    Vector3 originPosition;
    
    ASRS parentASRS;
    Cargo targetCargo;
    Conveyor inputSettlement;
    Conveyor outputSettlement;

    Transform targetTransform; 

    public StackerJobType currentJobType;

    float xAxisProceeded;
    float yAxisProceeded;
    float zAxisProceeded;

    bool hasCargo;
    bool isPullOrder;
    bool isPlacedInOrigin; 

    public bool isWorking
    {
        get { return internalBool; }
        set
        {
            internalBool = value;
//            Debug.Log("set " + value);
        }
    }

    /// <summary>
    /// 스테커 크래인의 리프트를 x,y 축에 대해 움직임 
    /// </summary>
    void MoveXAndYAxis()
    {
        xAxisProceeded += spec_LiftHorizontalSpeed * Time.deltaTime;
        yAxisProceeded += spec_LiftVerticalSpeed * Time.deltaTime;

        float tmpXCoordinate = Mathf.Lerp(startPosition.x, targetPosition.x, xAxisProceeded);
        float tmpYCoordinate = Mathf.Lerp(startPosition.y, targetPosition.y, yAxisProceeded);

        transform.position = new Vector3(tmpXCoordinate, tmpYCoordinate, this.transform.position.z);

        if ( xAxisProceeded >= 1f && yAxisProceeded >= 1f)
        {
            operate -= MoveXAndYAxis;
            JudgeNextOperate();    
        }
    }


    /// <summary>
    /// 스태커 크래인의 리프트를 각 rack 방향으로 넣고 뺌 
    /// </summary>
    void MoveZAxis()
    {

        if (isPullOrder)
        {
            if ( currentJobType.Equals(StackerJobType.PushCargoToOutput))
            {
                if (!outputSettlement.isDropable)
                {
                    return;
                }
            }
        }

        zAxisProceeded += spec_LiftPullPushSpeed * Time.deltaTime;

        transform.position = Vector3.Lerp(startPosition, targetPosition, zAxisProceeded);

        if (zAxisProceeded >= 1f)
        {
            operate -= MoveZAxis;
            
            if (hasCargo)
            {
                targetCargo.transform.SetParent(targetTransform);
                
                targetCargo = null;
                hasCargo = false;

                if (currentJobType.Equals(StackerJobType.PushCargoToOutput))
                {
                    outputSettlement.isDropable = false;
                }
            }
            else
            {
                targetCargo.transform.SetParent(this.transform);
                hasCargo = true;
            }

            zAxisProceeded = 0f;
            startPosition = this.transform.position;
            targetPosition = new Vector3(startPosition.x, startPosition.y, originPosition.z);
            operate += MoveOriginZAxis;
        }
    }

    void MoveOriginZAxis()
    {
        zAxisProceeded += spec_LiftPullPushSpeed * Time.deltaTime;

        transform.position = Vector3.Lerp(startPosition, targetPosition, zAxisProceeded);

        if (zAxisProceeded >= 1f)
        {
            operate -= MoveOriginZAxis;
            JudgeNextOperate();
        }

    }

    /// <summary>
    /// 각 operate 종료 후 다음 할 일 진행 
    /// </summary>
    void JudgeNextOperate()
    {
        StackerJobType tmptype = currentJobType;

        xAxisProceeded = 0f;
        yAxisProceeded = 0f;
        zAxisProceeded = 0f;

        startPosition = this.transform.position;

        switch (currentJobType)
        {
            case StackerJobType.MoveInputForPull:

                currentJobType = StackerJobType.PullCargoFromInput;
                operate += MoveZAxis;

                break;

            case StackerJobType.PullCargoFromInput:
                
                targetPosition = targetCell.transform.position;
                currentJobType = StackerJobType.MoveTargetCellForPush;
                operate += MoveXAndYAxis;

                break;

            case StackerJobType.MoveTargetCellForPush:

                targetPosition = targetTransform.position;
                currentJobType = StackerJobType.PushCargoToCell;
                operate += MoveZAxis;

                break;

            case StackerJobType.PushCargoToCell:

                parentASRS.ReportStacker(currentJobType);

                break;

            case StackerJobType.MoveTargetCellForPull:

                currentJobType = StackerJobType.PullCargoFromCell;
                operate += MoveZAxis;

                break;

            case StackerJobType.PullCargoFromCell:

                targetPosition = outputSettlementPosition;
                currentJobType = StackerJobType.MoveOutputForPush;
                operate += MoveXAndYAxis;
                
                break;

            case StackerJobType.MoveOutputForPush:

                currentJobType = StackerJobType.PushCargoToOutput;
                operate += MoveZAxis;

                break;

            case StackerJobType.PushCargoToOutput:

                parentASRS.ReportStacker(currentJobType);

                break;

            case StackerJobType.BackToOrigin:


                isPlacedInOrigin = true;
                parentASRS.ReportStacker(currentJobType);

                break;

            default:

                break;


        }

        Debug.Log(tmptype + " to " + currentJobType);

    }


    /// <summary>
    /// 화물을 자동 창고에서 뺄 때 호출 
    /// </summary>
    /// <param name="targetPosition"></param>
    public void Command_Pull(Cell targetCell, Cargo targetCargo)
    {
        this.targetCell = targetCell;
        this.targetCargo = targetCargo;

        startPosition = this.transform.position;
        targetPosition = targetCell.transform.position;
        targetTransform = outputSettlement.transform;

        isWorking = true;
        isPullOrder = true;
        
        currentJobType = StackerJobType.MoveTargetCellForPull;
        operate += MoveXAndYAxis;
        Debug.Log(currentJobType);
    }

    public void Commnad_Push(Cell targetCell, Cargo targetCargo)
    {
        this.targetCell = targetCell;
        this.targetCargo = targetCargo;

        startPosition = this.transform.position;
        targetPosition = inputSettlementPosition;
        targetTransform = targetCell.transform;

        isWorking = true;
        isPullOrder = false;

        if (isPlacedInOrigin)
        {
            currentJobType = StackerJobType.PullCargoFromInput;
            isPlacedInOrigin = false;
            operate += MoveZAxis;
            Debug.Log(currentJobType);
        }
        else if (!isPlacedInOrigin)
        {
            currentJobType = StackerJobType.MoveInputForPull;
            operate += MoveXAndYAxis;
            Debug.Log(currentJobType);
        }
    }
    public void Command_BackToOrigin()
    {
        isWorking = true;

        startPosition = this.transform.position;
        targetPosition = originPosition;

        currentJobType = StackerJobType.BackToOrigin;
        operate += MoveXAndYAxis;
        Debug.Log(currentJobType);
    }

    /// <summary>
    /// 초기화 함수
    /// cell 갯수 가변으로 인한 인입/인출 안착대 위치 설정 필요 
    /// </summary>
    /// <param name="inputSettlement">인입 안착대의 transform  </param>
    /// <param name="outSettlementPosition">인출 안착대의 transfrom</param>
    /// <param name="parentASRS"> 이 stacker crane을 제어하는 ASRS </param>
    public void Initialize(Conveyor inputSettlement, Conveyor outputSettlement, ASRS parentASRS)
    {
        this.inputSettlementPosition = inputSettlement.transform.position;
        this.outputSettlementPosition = outputSettlement.transform.position;
        this.inputSettlement = inputSettlement;
        this.outputSettlement = outputSettlement;
        this.parentASRS = parentASRS;

        originPosition = this.transform.position;
    }

    void Idle()
    {

    }
    

    private void Awake()
    {
        operate += Idle;

        isPlacedInOrigin = true;
        isWorking = false;
        hasCargo = false;

        spec_LiftHorizontalSpeed = 0.5f;
        spec_LiftVerticalSpeed = 0.5f;
        spec_LiftPullPushSpeed = 0.5f;
    }

    private void Start()
    {
        originHeight = transform.position.y;
        origin = transform.transform.position;
    }

    private void Update()
    {
        operate();   
    }

}
