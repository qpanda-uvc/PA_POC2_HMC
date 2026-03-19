using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MeshColliderTest : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    GameObject tmp;

    public void OnPointerEnter(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.AddComponent<MeshCollider>().convex = true;
        this.gameObject.AddComponent<Rigidbody>().useGravity = true;
        
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }


    
}
