using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForkLift_Anim : MonoBehaviour
{
    [SerializeField]
    GameObject forkArm;
    float forkArmHeight;
    float armUpSpeed;
    public Transform startPos;
    public Transform arrivePos;
    //public List<Transform> pos = new List<Transform>();
    public int posNum;

    Vector3 curveStartPos;
    Vector3 curvePoint;
    Vector3 curveEndPos;

    Vector3 bezierPos_1;
    Vector3 bezierPos_2;
    public float moveDuration;
    float moveSpeed;

    Animator animator;
    public bool armUp;

    private void Awake()
    {
        forkArm = this.transform.GetChild(1).gameObject;
        forkArmHeight = 1.2f;
        forkArm.transform.localPosition = new Vector3(0, 0, 0);

        posNum = 0;
        moveSpeed = 5.0f;
        armUpSpeed = 0.5f;

        animator = this.transform.GetChild(0).GetComponent<Animator>();
    }

    private void Start()
    {
        transform.localEulerAngles = new Vector3(0, 0, 0);
        MoveAtPos();
    }

    private void Update()
    {
        if (armUp)
        {
            ForkArmUp();
        }
        else
        {
            ForkArmDown();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveAtPos();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveBack();
        }
    }
    /*
    public void MoveAtPos()
    {
        transform.position = Vector3.Lerp(transform.position, pos[posNum].transform.position, moveSpeed);
        if (Vector3.Distance(transform.position, pos[posNum].transform.position) < 0.2)
        {
            animator.SetBool("isMove", false);
            time += Time.deltaTime;
            if (time > waitSecond)
            {
                time = 0;
                if (posNum >= this.pos.Count - 1)
                {
                    posNum = 0;
                }
                else
                {
                    posNum++;
                }
            }
        }
        else
        {
            animator.SetBool("isMove", true);
        }
    }
    */

    public void MoveAtPos()
    {
        curveStartPos = transform.position;
        curveEndPos = arrivePos.transform.position;
        curvePoint = new Vector3(curveEndPos.x, 0, curveStartPos.z);
        StartCoroutine(CurveMove(false));
    }

    public void MoveBack()
    {
        curveStartPos = transform.position;
        curveEndPos = startPos.transform.position;
        curvePoint = new Vector3(curveStartPos.x, 0, curveEndPos.z);
        StartCoroutine(CurveMove(true));
    }

    IEnumerator CurveMove(bool back)
    {
        float time = 0f;
        moveDuration = Vector3.Distance(transform.position, curveEndPos) / moveSpeed;

        while (true)
        {
            if (time > 1f)
            {
                yield break;
            }
            bezierPos_1 = Vector3.Lerp(curveStartPos, curvePoint, time);
            bezierPos_2 = Vector3.Lerp(curvePoint, curveEndPos, time);
            transform.position = Vector3.Lerp(bezierPos_1, bezierPos_2, time);

            /*
            if (Vector3.Distance(transform.position, curveEndPos) < 0.001f) 
            {
                yield break;
            }
            bezierPos_1 = Vector3.MoveTowards(curveStartPos, curvePoint, time);
            bezierPos_2 = Vector3.MoveTowards(curvePoint, curveEndPos, time);
            transform.position = Vector3.MoveTowards(bezierPos_1, bezierPos_2, time);
            */
            if (back)
            {
                SetRotation(bezierPos_1);
            }
            else
            {
                SetRotation(bezierPos_2);
            }

            time += Time.deltaTime / moveDuration;
            //time += Time.deltaTime * moveSpeed;

            yield return null;
        }
    }

    void SetRotation(Vector3 target)
    {
        if (Vector3.Distance(target, transform.position) > 0.01)
        {
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
            transform.localEulerAngles += new Vector3(0, 180, 0);
        }
    }

    public void ForkArmUp()
    {
        if (forkArm.transform.localPosition.y < forkArmHeight)
        {
            forkArm.transform.Translate(new Vector3(0, 0, forkArmHeight) * armUpSpeed * Time.deltaTime);
        }
        else
        {
            forkArm.transform.localPosition = new Vector3(forkArm.transform.localPosition.x, forkArmHeight, forkArm.transform.localPosition.z);
        }
    }

    public void ForkArmDown()
    {
        if (forkArm.transform.localPosition.y > 0)
        {
            forkArm.transform.Translate(new Vector3(0, 0, -1) * armUpSpeed * Time.deltaTime);
        }
        else
        {
            forkArm.transform.localPosition = new Vector3(forkArm.transform.localPosition.x, 0, forkArm.transform.localPosition.z);
        }
    }
}
