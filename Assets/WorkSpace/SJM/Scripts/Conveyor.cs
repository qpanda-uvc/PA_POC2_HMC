using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ConveyorType
{
    VMSIn,
    VMSOut,
    ASRSIn,
    ASRSOut,
    NoSpecific

}


public class Conveyor : MonoBehaviour
{
    public Cargo movingCargo;
    public Cargo waitingCargo;
    public string station;
    public ConveyorType conveyorType;
    public GameObject skid;
    public Conveyor nextConveyor;
    public bool isSettlement;
    public bool isDropable;
    public bool hasCargo;

    public float beltSpeed = 2.5f;

    public void Start()
    {
        this.station = gameObject.name;
        isDropable = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Cargo>(out var incomeCargo))
        {
            hasCargo = true;

            if ( incomeCargo.beltOccupy.Count == 0)
            {
                if (this.isSettlement)
                {
                    movingCargo = incomeCargo;
                    Debug.Log("[Conveyor] " + station + " settlement cargo enter: " + incomeCargo.cargoID);
                    return;
                }


                incomeCargo.transform.SetParent(this.transform);
                Vector3 tmpVector = incomeCargo.transform.localPosition;
                tmpVector.x = this.GetComponent<BoxCollider>().center.x;
                tmpVector.y = 0.25f;
                incomeCargo.transform.localPosition = tmpVector;


                if (skid != null)
                {
                    GameObject tmpSkid = Instantiate(skid);
                    tmpSkid.AddComponent<Tag_SKID>();
                    tmpSkid.transform.SetParent(incomeCargo.transform);
                    tmpSkid.transform.localScale = Vector3.one;
                    Vector3 vector = Vector3.zero;
                    vector.y = -0.15f;
                    tmpSkid.transform.localPosition = vector;
                }

                incomeCargo.transform.SetParent(null);
            }

            incomeCargo.AddQueue(this);
            waitingCargo = incomeCargo;

            Debug.Log("[Conveyor] " + station + " (" + conveyorType + ") cargo enter: " + incomeCargo.cargoID);
        }

    }

    private void OnTriggerExit(Collider other)
    {

        if (other.gameObject.TryGetComponent<Cargo>(out var incomeCargo))
        {
            hasCargo = false;
            if (!isSettlement)
            {
                incomeCargo.ExitQueue(this);
            }

            isDropable = true;
            Debug.Log("[Conveyor] " + station + " (" + conveyorType + ") cargo exit: " + incomeCargo.cargoID);
            SimulationModeTaskManager.Instance.ConveyorJobReport(this, other.gameObject);
            movingCargo = null;

            if ( waitingCargo != null)
            {
                movingCargo = waitingCargo;
                waitingCargo = null;
            }
        }
    }

    private void FixedUpdate()
    {
        if ( this.conveyorType == ConveyorType.ASRSIn ||
                this.conveyorType == ConveyorType.ASRSOut)
        {
            return;
        }

        if (waitingCargo != null && waitingCargo.beltOccupy.Count > 0 && waitingCargo.beltOccupy.Peek() == this)
        {
            movingCargo = waitingCargo;
            waitingCargo = null;
        }

        if ( movingCargo != null && movingCargo.isFlowable)
        {
            if (nextConveyor != null && nextConveyor.movingCargo != null)
            {
                isDropable = false;
                return;
            }

            isDropable = true;
            movingCargo.currentOwner = this;

            Rigidbody rb = movingCargo.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.MovePosition(movingCargo.transform.position + this.transform.forward * beltSpeed * Time.fixedDeltaTime);
            }
            else
            {
                movingCargo.transform.position += this.transform.forward * beltSpeed * Time.fixedDeltaTime;
            }
        }

        if ( waitingCargo == null)
        {
            isDropable = true;
        }
    }


}
