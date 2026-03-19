using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ForkLiftTask
{
    VMSToTruck,
    TruckToVMS,
    ULDToSettlement,
    SettlementToULD
}

public class ForkLift : MonoBehaviour
{
    [SerializeField]
    GameObject forkArm;

    GameObject cargo;

    public GameObject exCargo 
    {
        get
        {
            return cargo;
        }
        set
        {
            cargo = value;
        }
    }

    float forkArmHeight;
    float armUpSpeed;

    List<GameObject> wheels = new List<GameObject>();

    public Transform startPos;
    public Transform arrivePos;
    //public List<Transform> pos = new List<Transform>();
    public int posNum;

    Vector3 curveStartPos;
    Vector3 curvePoint;
    Vector3 curveEndPos;

    Vector3 bezierPos_1;
    Vector3 bezierPos_2;

    Vector3 originPose;
    public float moveDuration;
    float moveSpeed;

    public List<GameObject> waypoints;
    public Conveyor vmsConvyor; 
    public bool armUp;
    [HideInInspector] public bool isMoving;
    GameObject handPivot;
    public float basicFloorHeight;
    int index; 
    private void Awake()
    {
        forkArm = this.transform.GetChild(0).gameObject;
        forkArmHeight = 1.0f;

        posNum = 0;
        moveSpeed = 1.0f;
        armUpSpeed = 1.5f;
        basicFloorHeight = 2.49f;
        handPivot = forkArm.transform.Find(nameof(handPivot)).gameObject;
        
    }

    private void Start()
    {
        //transform.localEulerAngles = new Vector3(0, 0, 0);
        //MoveAtPos(arrivePos.position);
    }

    private void Update()
    {
        //Debug.Log(transform.localPosition);
    }


    public void MoveAtPos(Vector3 destination)
    {
        curveStartPos = transform.position;
        destination.y = basicFloorHeight;
        curveEndPos = destination;

        Vector3 tmpCurvePoint = Vector3.zero;

        //if (!armUp)
        //{
        //    curvePoint = new Vector3(9.8f, 2.49f, -23.64f);
            
        //}
        //else
        //{
        //    curvePoint = new Vector3(3.8f, 2.49f, -17.64f);
        //}


        //curvePoint = new Vector3(curveEndPos.x, 0, curveStartPos.z);
        StartCoroutine(CurveMove(false));
    }

    public void PullCargoToVMS(Vector3 coordinate, GameObject cargo)
    {
        if (isMoving) { Debug.LogWarning("[ForkLift] PullCargoToVMS 중복 호출 차단"); return; }
        isMoving = true;
        Vector3 targetLocation = new Vector3();
        this.exCargo = cargo;
        coordinate.y = this.transform.position.y;
        targetLocation = coordinate;


        originPose = transform.position;
        StartCoroutine(MoveToTruckCargo(transform.position, targetLocation));

    }

    IEnumerator MoveToTruckCargo(Vector3 startPoint, Vector3 EndPoint)
    {
        float time = 0f;

        EndPoint.y = startPoint.y;

        Vector3 direction = EndPoint - startPoint;
        if (direction.sqrMagnitude > 0.001f)
            this.transform.rotation = Quaternion.LookRotation(direction);

        while (!(Vector3.Distance(transform.position, EndPoint) < 0.01f))
        {
            transform.position = Vector3.Lerp(startPoint, EndPoint, time);

            
            time += Time.deltaTime;

            yield return null;
        }

        Vector3 tmpRotation = Vector3.zero;
        tmpRotation.y = 180f - transform.rotation.eulerAngles.y;

        transform.Rotate(tmpRotation);

        StartCoroutine(TruckCargoArmUp(exCargo));
        yield break;

    }

    IEnumerator TruckCargoArmUp(GameObject cargo)
    {
        armUp = true;
        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;

        cargo.transform.SetParent(handPivot.transform);
        cargo.transform.localPosition = Vector3.zero;
        cargo.transform.localEulerAngles = Vector3.zero;

        while (forkArm.transform.localPosition.y < forkArmHeight)
        {
            forkArm.transform.Translate(new Vector3(0, forkArmHeight, 0) * armUpSpeed * Time.deltaTime);
            yield return null;
        }


        StartCoroutine(TruckCargoToVMS(transform.position, originPose));
        yield break;
    }


    IEnumerator TruckCargoToVMS(Vector3 startPoint, Vector3 EndPoint)
    {
        float time = 0f;

        EndPoint.y = startPoint.y;

        Vector3 direction = EndPoint - startPoint;
        if (direction.sqrMagnitude > 0.001f)
            this.transform.rotation = Quaternion.LookRotation(direction);

        while (!(Vector3.Distance(transform.position, EndPoint) < 0.01f))
        {
            transform.position = Vector3.Lerp(startPoint, EndPoint, time);


            time += Time.deltaTime;

            yield return null;
        }
        Vector3 tmpRotation = Vector3.zero;
        tmpRotation.y = 90f - transform.rotation.eulerAngles.y;

        transform.Rotate(tmpRotation);
        StartCoroutine(LiftDownToVMS());
    }

    IEnumerator LiftDownToVMS()
    {

        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;


        while (forkArm.transform.localPosition.y > -0.48f)
        {
            if (vmsConvyor.isDropable && (vmsConvyor.waitingCargo == null || vmsConvyor.waitingCargo == exCargo.GetComponent<Cargo>()))
            {
                forkArm.transform.Translate(new Vector3(0, -1, 0) * armUpSpeed * Time.deltaTime);
            }

            

            yield return null;
        }

        if (exCargo != null)
        {
            exCargo.transform.SetParent(null);
            
        }

        isMoving = false;
        SimulationModeTaskManager.Instance.ForkLiftUnloadToVMSEnded(exCargo);
        //exCargo = null;

        yield break;
    }



    public void PullCargoToULD(int settlementIndex, GameObject cargo)
    {
        if (isMoving) { Debug.LogWarning("[ForkLift] PullCargoToULD 중복 호출 차단"); return; }
        isMoving = true;

        this.exCargo = cargo;

        // 인덱스 범위 체크
        int waypointIdx = (settlementIndex % (waypoints.Count - 1)) + 1;
        if (waypointIdx >= waypoints.Count) waypointIdx = 1;

        Vector3 targetLocation = waypoints[waypointIdx].transform.position;
        originPose = transform.position;
        StartCoroutine(MoveToULDSettlement(transform.position, targetLocation));
    }


    IEnumerator MoveToULDSettlement(Vector3 startPoint, Vector3 EndPoint)
    {
        float time = 0f;

        EndPoint.y = startPoint.y;

        Vector3 direction = EndPoint - startPoint;
        if (direction.sqrMagnitude > 0.001f)
            this.transform.rotation = Quaternion.LookRotation(direction);

        while (!(Vector3.Distance(transform.position, EndPoint) < 0.01f))
        {
            transform.position = Vector3.Lerp(startPoint, EndPoint, time);


            time += Time.deltaTime;

            yield return null;
        }
        Vector3 tmpRotation = Vector3.zero;
        tmpRotation.y = 270f - transform.rotation.eulerAngles.y;

        transform.Rotate(tmpRotation);


        StartCoroutine(ULDSettlementArmUp(exCargo));
        yield break;
    }

    IEnumerator ULDSettlementArmUp(GameObject target)
    {
        armUp = true;
        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;

        target.transform.SetParent(handPivot.transform);
        target.transform.localPosition = Vector3.zero;
        target.transform.localEulerAngles = Vector3.zero;

        while (forkArm.transform.localPosition.y < forkArmHeight)
        {
            forkArm.transform.Translate(new Vector3(0, forkArmHeight, 0) * armUpSpeed * Time.deltaTime);
            yield return null;
        }


        StartCoroutine(MoveToULD(transform.position, originPose));
        yield break;

    }

    IEnumerator MoveToULD(Vector3 startPoint, Vector3 EndPoint)
    {
        float time = 0f;

        EndPoint.y = startPoint.y;

        Vector3 direction = EndPoint - startPoint;
        if (direction.sqrMagnitude > 0.001f)
            this.transform.rotation = Quaternion.LookRotation(direction);

        while (!(Vector3.Distance(transform.position, EndPoint) < 0.01f))
        {
            transform.position = Vector3.Lerp(startPoint, EndPoint, time);


            time += Time.deltaTime;

            yield return null;
        }

        Vector3 tmpRotation = Vector3.zero;
        tmpRotation.y = 90 - transform.rotation.eulerAngles.y;

        transform.Rotate(tmpRotation);

        StartCoroutine(LiftDownToULD());

        yield break;
    }
    
    IEnumerator LiftDownToULD()
    {
        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;

        while (forkArm.transform.localPosition.y > -0.6f)
        {
            forkArm.transform.Translate(new Vector3(0, -1, 0) * armUpSpeed * Time.deltaTime);
            yield return null;
        }

        if (exCargo != null)
        {
            exCargo.transform.SetParent(null);
        }

        armUp = false;
        isMoving = false;
        SimulationModeTaskManager.Instance.ForkLiftJobreport(this, exCargo);

        exCargo = null;

        yield break;
    }




    public void MoveBack()
    {
        curveStartPos = transform.position;
        curveEndPos = startPos.transform.position;
        curvePoint = new Vector3(0, curveStartPos.y, 0);
        StartCoroutine(CurveMove(true));
    }

    IEnumerator CurveMove(bool back)
    {
        float time = 0f;
        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;

        while (!(Vector3.Distance(transform.position, curveEndPos) < 0.01f))
        {
            bezierPos_1 = Vector3.Lerp(curveStartPos, curvePoint, time);
            bezierPos_2 = Vector3.Lerp(curvePoint, curveEndPos, time);
            transform.position = Vector3.Lerp(bezierPos_1, bezierPos_2, time);

            if (back)
            {
                SetRotation(bezierPos_1);
                //wheelSpeed = 600.0f;
            }
            else
            {
                SetRotation(bezierPos_2);
                //wheelSpeed = -600.0f;
            }

            time += Time.deltaTime / moveDuration;

            yield return null;
        }

        if (!armUp)
        {
            SimulationModeTaskManager.Instance.ForkLiftArriveAtTruck();
        }
        else if (armUp)
        {
            if ( exCargo != null)
                SimulationModeTaskManager.Instance.ForkLiftArriveAtVMS();
            else
                SimulationModeTaskManager.Instance.ForkLiftArriveAtTruck();
        }
        yield break;


    }

    void SetRotation(Vector3 target)
    {
        if (Vector3.Distance(target, transform.position) > 0.01) 
        {
            transform.rotation = Quaternion.LookRotation(transform.position - target);
        }
    }
    IEnumerator ForkArmUpAni(GameObject target)
    {
        armUp = true;
        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;

        target.transform.SetParent(handPivot.transform);
        target.transform.localPosition = Vector3.zero;
        target.transform.localEulerAngles = Vector3.zero;

        while (forkArm.transform.localPosition.y < forkArmHeight)
        {
            forkArm.transform.Translate(new Vector3(0, forkArmHeight, 0) * armUpSpeed * Time.deltaTime);
            yield return null;
        }

        SimulationModeTaskManager.Instance.ForkLiftUpEnded();

    }
    public void ForkArmUp(GameObject target) 
    {
        exCargo = target;
        StartCoroutine(ForkArmUpAni(target));
    }

    IEnumerator ForkArmDownAni(GameObject target)
    {
        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;

        while (forkArm.transform.localPosition.y > -0.8f)
        {
            forkArm.transform.Translate(new Vector3(0, -1, 0) * armUpSpeed * Time.deltaTime);
            yield return null;
        }

        
        if ( exCargo != null)
        {
            exCargo.transform.SetParent(null);
            Vector3 cargoPosition = exCargo.transform.position;
            cargoPosition.y = 1.9f;
            exCargo.transform.position = cargoPosition;
        }
        
        isMoving = false;
        SimulationModeTaskManager.Instance.ForkLiftUnloadToVMSEnded(exCargo);

        exCargo = null;

    }

    public void ForkArmDown(GameObject targetConveyor)
    {
        StartCoroutine(ForkArmDownAni(targetConveyor));
    }

}
