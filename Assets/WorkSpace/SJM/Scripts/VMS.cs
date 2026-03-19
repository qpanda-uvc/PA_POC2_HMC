using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VMS : MonoBehaviour
{
    bool isActive;

    Color originMatColor;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ConveyorObject>() != null)
        {
            other.GetComponent<ConveyorObject>().Scan();
            isActive = true;
            originMatColor = other.gameObject.GetComponent<MeshRenderer>().material.color;
            other.gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<ConveyorObject>() != null)
        {
            other.GetComponent<ConveyorObject>().ScanEnd();
            isActive = false;
            other.gameObject.GetComponent<MeshRenderer>().material.color = originMatColor;
        }
    }

    public bool IsActive()
    {
        return isActive;
    }
}
