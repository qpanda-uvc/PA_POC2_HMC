using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cargo_Dummy : MonoBehaviour
{
    // Start is called before the first frame update

    public float top;
    public float bottom;
    public float front;
    public float rear;
    public float left;
    public float right; 

    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Generate Square Volume base Wall Coordinate 
    /// </summary>
    public void SetSpec()
    {
        Vector3 extents = this.GetComponent<MeshRenderer>().bounds.extents;

        top = this.transform.position.y + extents.y;
        bottom = this.transform.position.y - extents.y;
        front = this.transform.position.z - extents.z;
        rear = this.transform.position.z + extents.z;
        left = this.transform.position.x - extents.x;
        right = this.transform.position.x + extents.x;

    }

}
