using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LWJ;

public class AMRWaypointManager : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> VmsToASRS = new List<GameObject>();
    public List<GameObject> ASRSToVMSWaiting = new List<GameObject>();
    public List<GameObject> VMSWaitingToVMS = new List<GameObject>();
    


    public List<GameObject> ASRSToULD = new List<GameObject>();
    public List<GameObject> ULDToASRSWaiting = new List<GameObject>();
    public List<GameObject> ASRSEntryWaiting = new List<GameObject>();
    public List<GameObject> WaitingToASRS = new List<GameObject>();

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
