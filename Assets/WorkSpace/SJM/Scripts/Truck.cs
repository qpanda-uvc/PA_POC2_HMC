using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Truck : MonoBehaviour
{
    List<GameObject> wheels = new List<GameObject>();
    float wheelSpeed;

    public Transform startPos;
    public Transform arrivePos;
    public Transform endPos;

    public float moveSpeed;
    
    
    public float originMoveSpeed;
    public float accelerationRate;
    public float decelerationRate;
    public float breakStartDistance;
    public bool gotoArrivePos;
    public bool end;
    bool isDoorOpen;
    bool canMove;
    bool isbreak;

    bool isActive;

    Animator animator;

    private void Awake()
    {
        //moveSpeed = 0.002f;
        //originMoveSpeed = 5.0f;
        //accelerationRate = 1.22f;
        //decelerationRate = 1.22f;
        //breakStartDistance = 10.0f;
        gotoArrivePos = true;
        end = false;
        isDoorOpen = false;

        animator = GetComponent<Animator>();

        transform.position = startPos.transform.position;

        for (int i = 0; i < transform.GetChild(1).childCount; i++)
        {
            wheels.Add(transform.GetChild(1).gameObject.transform.GetChild(i).gameObject);
        }
        wheelSpeed = 100;
    }

    private void Start()
    {
        //animator.SetBool("isMove", true);
        //StartCoroutine(MoveAtPos());

        //Debug.Log("Start in start");

    }

    public void DriveToRallyPoint()
    {
        animator.SetBool("isMove", true);
        StartCoroutine(MoveAtPos());

    }

    public void DriveToEndPoint()
    {
        end = true;
    }

    IEnumerator MoveAtPos()
    {
        isActive = true;
        animator.SetBool("isMove", true);
        StartCoroutine(Acceleration());
        bool endAcceleration = true;
        while (true)
        {
            WheelRoate();
            if (gotoArrivePos)
            {
                //transform.position = Vector3.Lerp(transform.position, arrivePos.transform.position, moveSpeed);
                transform.position = Vector3.MoveTowards(transform.position, arrivePos.transform.position, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, arrivePos.transform.position) < breakStartDistance && isbreak == false)
                {
                    isbreak = true;
                    StartCoroutine(Deceleration(arrivePos.transform.position));
                }
                if (Vector3.Distance(transform.position, arrivePos.transform.position) < 1f) // µµÂø
                {
                    moveSpeed = 0;
                    isbreak = false;
                    animator.SetBool("isMove", false);
                    OpenTheDoor();
                    gotoArrivePos = false;
                    SimulationModeTaskManager.Instance.TruckArrive();

                }
            }
            if(end)
            {
                CloseTheDoor();
                if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f && animator.GetCurrentAnimatorStateInfo(0).IsName("Stay"))
                {
                    canMove = true;
                }
                if (canMove)
                {
                    //transform.position = Vector3.Lerp(transform.position, endPos.transform.position, moveSpeed);
                    if (endAcceleration)
                    {
                        StartCoroutine(Acceleration());
                        endAcceleration = false;
                    }
                    transform.position = Vector3.MoveTowards(transform.position, endPos.transform.position, moveSpeed * Time.deltaTime);

                    if (Vector3.Distance(transform.position, endPos.transform.position) < 0.1)
                    {
                        moveSpeed = 0;
                        animator.SetBool("isMove", false);
                        isActive = false;
                        Destroy(gameObject);
                    }
                    else
                    {
                        animator.SetBool("isMove", true);
                    }
                }
            }
            yield return null;
        }
    }

    IEnumerator Acceleration()
    {
        moveSpeed = 0;
        while (true)
        {
            if (originMoveSpeed > moveSpeed + (accelerationRate * Time.deltaTime))
            {
                moveSpeed += accelerationRate * Time.deltaTime;
            }
            else
            {
                moveSpeed = originMoveSpeed;
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Deceleration(Vector3 target)
    {
        while (true)
        {
            if (Vector3.Distance(transform.position, target) < 0.1)
            {
                moveSpeed = 0;
                yield break;
            }
            if (moveSpeed > decelerationRate * Time.deltaTime)
            {
                moveSpeed -= decelerationRate * Time.deltaTime;
            }
            else
            {
                moveSpeed = 0;
                yield break;
            }

            yield return new WaitForFixedUpdate(); ;
        }
    }
        void OpenTheDoor()
    {
        if (!isDoorOpen)
        {
            animator.SetTrigger("OpenDoor");
            isDoorOpen = true;
            
        }
        canMove = false;
    }
    void CloseTheDoor()
    {
        if (isDoorOpen)
        {
            animator.SetTrigger("CloseDoor");
            isDoorOpen = false;
        }
    }

    void WheelRoate()
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            wheels[i].transform.Rotate(moveSpeed * wheelSpeed * Time.deltaTime, 0, 0);
        }
    }

    public bool IsActive()
    {
        return isActive;
    }
}


