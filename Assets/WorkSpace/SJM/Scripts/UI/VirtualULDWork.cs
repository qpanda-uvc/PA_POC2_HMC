using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualULDWork : MonoBehaviour
{
    public FlightStatus flightStatus;

    [SerializeField]
    VirtualULD[] ulds;

    [SerializeField]
    GameObject[] cargoObjects;

    public VirtualULD currentULD;
    public Vector3 virtualULDPos;
    public Vector3 virtualULDRot;
    public Camera stackViewULDCamera;
    public Camera loadTableULDCamera;


    public void Initialize()
    {
        stackViewULDCamera = transform.Find("Stack View ULD Camera").gameObject.GetComponent<Camera>();
        loadTableULDCamera = transform.Find("Load Table ULD Camera").gameObject.GetComponent<Camera>();

        cargoObjects = Resources.LoadAll<GameObject>("Cargos");
        ulds = Resources.LoadAll<VirtualULD>("ULDs");
    }

    public void GenerateNewULD(FlightInfo flight, string myIndexName)
    {
        if (currentULD != null)
        {
            Destroy(currentULD.gameObject);
        }

        ULDInfo tmpULDInfo = new ULDInfo();
        tmpULDInfo = flight.uldInfos[myIndexName];

        // Flight가 가진 uldType의 이름과 같은 ULD 생성
        for (int i = 0; i < ulds.Length; i++)
        {
            if (ulds[i].gameObject.name == tmpULDInfo.uldType)
            {
                currentULD = Instantiate(ulds[i]);
            }
        }
        currentULD.transform.localScale = new Vector3(1, 1, 1);
        currentULD.gameObject.transform.position = virtualULDPos;
        currentULD.gameObject.transform.localEulerAngles = virtualULDRot;
        stackViewULDCamera.transform.position = new Vector3(-999, -980, -1020);
        loadTableULDCamera.transform.position = new Vector3(-999, -980, -1020);

        // 새 Cargos 생성
        List<GameObject> newCargoObjects = new List<GameObject>();
        for (int i = 0; i < tmpULDInfo.cargos.Count; i++)
        {
            for (int j = 0; j < cargoObjects.Length; j++)
            {
                if (tmpULDInfo.cargos[i] == cargoObjects[j].name)
                {
                    newCargoObjects.Add(cargoObjects[j]);
                }
            }
        }
        currentULD.GenerateCargos(newCargoObjects, tmpULDInfo.cargoPos);

        flightStatus.stackView_Canvas.GetComponent<StackView_Canvas>().toShowULD = currentULD;
        flightStatus.loadTable_Canvas.GetComponent<LoadTable_Canvas>().toShowULD = currentULD;
    }
}
