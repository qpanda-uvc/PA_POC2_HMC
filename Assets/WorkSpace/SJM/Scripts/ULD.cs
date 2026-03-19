using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ULD : MonoBehaviour
{
    public List<GameObject> cargos = new List<GameObject>();
    public List<Vector3> cargoPos = new List<Vector3>();
    public GameObject pivot; 
    // 현재 작업중인 ULD이기 때문에 실시간으로 Cargo Add
    public void AddNewCargo(GameObject addCargo, Vector3 addCargoPos)
    {
        // 이 ULD에 추가
        cargos.Add(addCargo.gameObject);
        //cargoPos.Add(addCargoPos);
        //addCargo.transform.parent = pivot.transform;
        //addCargo.transform.position = addCargoPos;  
    }
    
    public void WorkEnd()
    {
        Destroy(this.gameObject);
    }

}
