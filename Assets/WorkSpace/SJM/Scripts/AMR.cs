using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PGD;

public class AMR : MonoBehaviour
{
    public Transform amr_target;
    public GameObject lift;
    float originMoveSpeed;
    public float currentMoveSpeed;
    float accelerationRate;
    float breakStartDistance;

    public string id;

    public GameObject cargo;
    Quaternion targetRotation;
    public AMRGroup group;
    public bool isWaiting;
    bool isDecelerate;
    public bool isArrive;
    public int currentIndex; 

    Rigidbody amrRigidbody;
    public BoxCollider myCollider;
    public BoxCollider AMRDetectorCollider;
    float AMRSize;
    float AMRdetectRange;
    public bool priority;

    float liftSpeed;
    float liftOriginHeight;
    float liftHeight;
    bool isActive;
    public AMRTask currentTask;

    public bool isBooked;

    public List<GameObject> locations;

    public Panel_011 floatingUI;

    private float remainingBattery = 100f;
    string destination = "";

    private void Awake()
    {
        isArrive = true;
        originMoveSpeed = 3.0f;
        accelerationRate = 1.5f;
        breakStartDistance = 4.5f;

        //if (transform.GetChild(0).gameObject.GetComponent<BoxCollider>() == null)
        //{
        //    AMRDetectorCollider = transform.GetChild(0).gameObject.AddComponent<BoxCollider>(); 
        //}
        //else
        //{
        //    AMRDetectorCollider = transform.GetChild(0).gameObject.GetComponent<BoxCollider>();
        //}
        
        //if (gameObject.GetComponent<BoxCollider>() == null)
        //{
        //    myCollider = gameObject.AddComponent<BoxCollider>();
        //}
        //else
        //{
        //    myCollider = gameObject.GetComponent<BoxCollider>();
        //}



        Bounds localBounds = new Bounds(GetComponent<BoxCollider>().center, GetComponent<BoxCollider>().size);
        AMRSize = localBounds.size.x;
        AMRdetectRange = localBounds.size.z * 3;

        //AMRDetectorCollider.isTrigger = true;
        //AMRDetectorCollider.size = new Vector3(AMRSize * 5, 1, AMRdetectRange);
        //AMRDetectorCollider.center = new Vector3(0, 0, AMRdetectRange / 2);

        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
        }
        amrRigidbody = gameObject.GetComponent<Rigidbody>();
        amrRigidbody.useGravity = false;
        amrRigidbody.isKinematic = true;


        liftSpeed = 0.5f;
        liftOriginHeight = lift.transform.position.y;
        liftHeight = liftOriginHeight + 1.0f;

       
        if ( this.name.Contains("In"))
        {
            this.group = AMRGroup.InputGroup;
        }
        else if (this.name.Contains("Out"))
        {
            this.group = AMRGroup.OutputGroup;
        }
    }

    private void Update()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.UpdateAMRPanelRealTimeData(id, transform.position);

        string tmp = cargo == null ? "" : cargo.GetComponent<Cargo>().cargoName;

        UIManager.Instance.UpdateAMRPanelData(
             this.id,
             destination,
             tmp,
             currentTask.ToString().Split("To")[0],
             currentTask.ToString().Split("To")[1],
             remainingBattery,
             1f,
             1.2f
         );
    }

    #region Spin And Move
    public void SpinAndMove()
    {
        //Debug.Log("���");
        isActive = true;
        isArrive = false;
      
        if (locations.Count <= currentIndex)
        {
            SimulationModeTaskManager.Instance.AMRJobEndReport(this, cargo);
            remainingBattery = 90f;
            if (floatingUI != null) floatingUI.UpdateAMRData(this.id, "�����", remainingBattery);

            string tmp = cargo == null ? "" : cargo.GetComponent<Cargo>().cargoName;

            //UIManager.Instance.UpdateAMRPanelData(
            //    this.id,
            //    destination,
            //    tmp,
            //    currentTask.ToString().Split("To")[0],
            //    currentTask.ToString().Split("To")[1],
            //    remainingBattery,
            //    1f,
            //    1.2f
            //);

            return;
        }

        amr_target = locations[currentIndex].transform;

        targetRotation = Quaternion.LookRotation(amr_target.transform.position - transform.position);

        SetUIData();

        if (currentIndex == 1)
        {
            switch (currentTask)
            {
                case AMRTask.VMSToASRS:
                case AMRTask.ASRSToVMSWaiting:
                    StartCoroutine(Undocking(amr_target.transform.position));
                    break;

                case AMRTask.ASRSToULDSettlment:
                case AMRTask.ULDSettlementToWaiting:
                case AMRTask.VMSWatingToVMS:
                case AMRTask.WaitingToASRSEntry:
                case AMRTask.EntryToASRS:
                    StartCoroutine(Spin());
                    break;

                default:
                    break;

            }
        }
        else
        {
            StartCoroutine(Spin());
        }

    }

    public void SetUIData()
    {
        switch (currentTask)
        {
            case AMRTask.VMSToASRS:
                destination = "ASRS";
                break;
            case AMRTask.ASRSToVMSWaiting:
                destination = "������";
                break;
            case AMRTask.ASRSToULDSettlment:
                destination = "������";
                break;
            case AMRTask.ULDSettlementToWaiting:
                destination = "������";
                break;

            case AMRTask.VMSWatingToVMS:
                destination = "VMS";
                break;
            case AMRTask.WaitingToASRSEntry:
                destination = "������";
                break;
            case AMRTask.EntryToASRS:
                destination = "ASRS";
                break;

            default:
                break;

        }

        if (floatingUI != null) floatingUI.UpdateAMRData(this.id, destination, remainingBattery);
        string tmp = cargo == null ? "" : cargo.GetComponent<Cargo>().cargoName;

        //UIManager.Instance.UpdateAMRPanelData(
        //    this.id,
        //    destination,
        //    tmp,
        //    currentTask.ToString().Split("To")[0],
        //    currentTask.ToString().Split("To")[1],
        //    remainingBattery,
        //    1f,
        //    1.2f
        //);
    }



    public void TakeOrder(AMRSignal inputSignal)
    {
        StartCoroutine(LiftUpDown(inputSignal.loaded));

        Vector3 movePoint = new Vector3(inputSignal.X, 0f, inputSignal.Y);
        StartCoroutine(MoveAtPos(amr_target.transform.position));

    }

    IEnumerator LiftUpDown(bool upDown)
    {
        while (true)
        {
            if (upDown)
            {
                if (Vector3.Distance(lift.transform.position, new Vector3(lift.transform.position.x, liftHeight, lift.transform.position.z)) < 0.01)
                {
                    yield break;
                }
                else
                {
                    lift.transform.Translate(Vector3.up * Time.deltaTime * liftSpeed);
                    yield return null;
                }
            }
            else
            {
                if (Vector3.Distance(lift.transform.position, new Vector3(lift.transform.position.x, liftOriginHeight, lift.transform.position.z)) < 0.01)
                {
                    yield break;
                }
                else
                {
                    lift.transform.Translate(Vector3.down * Time.deltaTime * liftSpeed);
                    yield return null;
                }
            }
        }
    }

    IEnumerator Undocking(Vector3 target)
    {
        StartCoroutine(Acceleration());
        bool isBreak = false;
        while (true)
        {

            currentMoveSpeed = 3f;
            transform.Translate(Vector3.forward*(-1) * Time.deltaTime * currentMoveSpeed);
            if (!isWaiting && !isBreak)
            {
                if (Vector3.Distance(transform.position, target) < breakStartDistance)
                {
                    isBreak = true;
                    StartCoroutine(Deceleration(target, 0.65f * (originMoveSpeed / 2.0f)));
                }
            }

            if (isArrive)
            {
                isActive = false;
                currentIndex++;
                SpinAndMove();
                //Debug.Log("����");
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }

    }


    IEnumerator Spin()
    {
        //float spinrate = 10f;
        //Quaternion initialRotation = transform.rotation;
        //while (true)
        //{
        //    Vector3 direction = amr_target.position - transform.position;
        //    Quaternion toRotation = Quaternion.LookRotation(direction);

        //    transform.rotation = Quaternion.Lerp(initialRotation, toRotation, spinrate * Time.deltaTime);

        //    // ȸ�� �� ��ģ ��
        //    if ((int)toRotation.eulerAngles.y == (int)transform.rotation.eulerAngles.y) 
        //    {
        //        StartCoroutine(MoveAtPos(amr_target.transform.position));
        //        yield break;
        //    }

        //    yield return null;
        //}

        float spinSpeed = 25f;
        targetRotation = Quaternion.LookRotation(amr_target.transform.position - transform.position);

        while (true)
        {
            // ȸ�� �� ��ģ ��
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1)
            {
                StartCoroutine(MoveAtPos(amr_target.transform.position));
                yield break;
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, spinSpeed * Time.deltaTime);
            }
            yield return null;
        }
    }

    IEnumerator MoveAtPos(Vector3 target)
    {
        StartCoroutine(Acceleration());
        bool isBreak = false;
        isWaiting = false;
        while (true)
        {

            currentMoveSpeed = 3f;
            transform.Translate(Vector3.forward * Time.deltaTime * currentMoveSpeed);
            if (!isWaiting && !isBreak)
            {
                if (Vector3.Distance(transform.position, target) < breakStartDistance)
                {
                    isBreak = true;
                    StartCoroutine(Deceleration(target, 0.65f * (originMoveSpeed / 2.0f)));
                }
            }

            if (isArrive)
            {
                isActive = false;
                currentIndex++;
                SpinAndMove();
                //Debug.Log("����");
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }
    }
    IEnumerator Acceleration()
    {
        currentMoveSpeed = 0;
        while (true)
        {
            if (!isWaiting && !isDecelerate)
            {
                if (originMoveSpeed > currentMoveSpeed + (accelerationRate * Time.deltaTime))
                {
                    currentMoveSpeed += accelerationRate * Time.deltaTime;
                }
                else
                {
                    currentMoveSpeed = originMoveSpeed;
                    yield break;
                }
            }
            else
            {
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Deceleration(Vector3 target, float decelerationRate)
    {
        //Debug.Log(decelerationRate);
        isDecelerate = true;
        while (true)
        {
            if (Vector3.Distance(transform.position, target) < 0.1)
            {
                isArrive = true;
                currentMoveSpeed = 0;
                isDecelerate = false;
                yield break;
            }
            if(currentMoveSpeed > decelerationRate * Time.deltaTime)
            {
                currentMoveSpeed -= decelerationRate * Time.deltaTime;
            }
            else
            {
                currentMoveSpeed = 0;
                isDecelerate = false;
                yield break;
            }

            yield return new WaitForFixedUpdate(); ;
        }
    }
    #endregion

    #region Detect Other AMR
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<AMR>() != null/* && other.gameObject.GetComponent<AMR>().isWaiting == false*/) 
        {
            isWaiting = true;
            //Debug.Log("��� ����");
            /*
            float rand = Random.Range(0.05f, 0.1f);
            transform.Translate(-Vector3.forward * rand);
            */
            StartCoroutine(Deceleration(other.transform.position, Vector3.Distance(transform.position, other.transform.position) / (20.0f / originMoveSpeed)));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<AMR>() != null)
        {
            isWaiting = false;
            StartCoroutine(Acceleration());
        }
    }
    #endregion

    public bool IsActive()
    {
        return isActive;
    }
}
