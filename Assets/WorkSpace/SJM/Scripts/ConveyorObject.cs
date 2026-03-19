using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorObject : MonoBehaviour
{
    public Vector3 vector;
    public float speed;
    float scanDecreaseRate;
    int changeNum;
    public bool stop;
    public bool isOnBelt;

    private void Awake()
    {
        vector = new Vector3(1, 0, 0);
        speed = 3f;
        scanDecreaseRate = 3.0f;
        isOnBelt = false;

        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    private void Start()
    {
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name.Contains("Conveyor") && changeNum < 3)
        {
            Debug.Log(other.name);
            VectorChange();
        }
    }

    public void StartConvyor()
    {
        StartCoroutine(MoveOnConveyor());
    }

    IEnumerator MoveOnConveyor()
    {
        while (true)
        {
            if (!stop)
            {
                transform.Translate(vector * speed * Time.deltaTime,Space.World);
            }
            yield return null;
        }
    }

    public void VectorChange()
    {
        
        if (changeNum == 0)
        {
            vector = Vector3.forward;
            changeNum++;
        }
        else if (changeNum == 1)
        {
            vector = Vector3.right;
            changeNum++;
        }
        else if ( changeNum == 2 )
        {
            vector = Vector3.zero;
            changeNum++;
            stop = true;
            //SimulationModeTaskManager.Instance.CargoArrivedAtVMSEnd(this.gameObject);
        }   

        //Debug.Log(changeNum);
    }

    public void Scan()
    {
        speed = speed / scanDecreaseRate;
    }

    public void ScanEnd()
    {
        speed = speed * scanDecreaseRate;
    }
}
