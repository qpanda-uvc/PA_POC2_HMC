using INab.Dissolve;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cargo : MonoBehaviour
{
    public Queue<Conveyor> beltOccupy = new Queue<Conveyor>();
    public bool isFlowable;
    public Vector3 whl = new Vector3();
    public GameObject onlycargo;

    public string cargoID;
    public string dbName;
    public string cargoName;
    public string POU;
    public string currentLocation;
    public float width;
    public float length;
    public float depth;
    public float waterVolume;
    public float weight;
    public List<string> SCCs = new List<string>();
    public string state;
    public Vector3 finalPosition;
    public List<string> convyerHistory = new List<string>();
    public Conveyor currentOwner;
    public bool isBookedForPull = false;

    public Dissolver trackingEffect;
    //public GameObject scanCompleteEffect;

    public void Initialize()
    {

    }

    // Start is called before the first frame update
    void Awake()
    {
        whl = new Vector3(1f, 1f, 1f);
        //onlycargo = this.transform.Find("Cargo");
    }

    // Update is called once per frame
    void Update()
    {
        //SetCargoScale();
    }

    public void AddQueue(Conveyor conveyor)
    {
        this.transform.rotation = Quaternion.Euler(Vector3.zero);
        this.beltOccupy.Enqueue(conveyor);
    }

    public void ExitQueue(Conveyor conveyor)
    {
        this.beltOccupy.Dequeue();
    }

    public void SetCargoScale()
    {
        onlycargo.transform.localScale = whl;
        onlycargo.transform.localPosition = new Vector3(0f, onlycargo.GetComponent<Mesh>().bounds.size.y / 2, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<VMSBeam>())
        {
            trackingEffect.gameObject.SetActive(true);
            trackingEffect.Dissolve();
            //trackingEffect = Instantiate(trackingEffect, gameObject.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<VMSBeam>())
        {
            trackingEffect.gameObject.SetActive(false);
            //Destroy(trackingEffect);
            //scanCompleteEffect = Instantiate(scanCompleteEffect, gameObject.transform);
            //scanCompleteEffect.transform.localPosition = new Vector3(0, 1f, 0);
            //Invoke("DestoryEffect", 1f);
        }  
    }

    //void DestoryEffect()
    //{
    //    Destroy(scanCompleteEffect);
    //}
}
