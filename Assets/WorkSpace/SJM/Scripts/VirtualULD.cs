using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualULD : MonoBehaviour
{
    VirtualULDWork virtualULDWork;

    public List<GameObject> cargos = new List<GameObject>();
    public List<Vector3> cargoPos = new List<Vector3>();
    List<GameObject> visibleCargos = new List<GameObject>();

    Coroutine activeCoroutine;

    public float uldHeight = 18.5f;
    public float fallingSpeed;
    float originSpeed;
    public bool isPause;
    public bool isReset;

    public void Awake()
    {
        virtualULDWork = FindObjectOfType<VirtualULDWork>();
        uldHeight = 18.5f;
        originSpeed = 10.0f;
        fallingSpeed = originSpeed;
    }

    public void GenerateCargos(List<GameObject> btnCargos, List<Vector3> btnPos)
    {
        cargos.Clear();
        cargoPos.Clear();

        for (int i = 0; i < btnCargos.Count; i++)
        {
            GameObject newCargo = Instantiate(btnCargos[i]);
            cargos.Add(newCargo);
            cargoPos.Add(btnPos[i] + virtualULDWork.virtualULDPos);
            newCargo.transform.parent = transform;
            newCargo.transform.position = cargoPos[i];
            newCargo.SetActive(false);
        }
    }

    public void StackStart()
    {
        if (activeCoroutine == null)
        {
            activeCoroutine = StartCoroutine(CargoStackStart());
        }
    }

    public IEnumerator CargoStackStart()
    {
        int cargoNum = 0;
        bool setStartPos = false;
        isReset = false;

        for (int i = 0; i < cargos.Count; i++)
        {
            cargos[i].SetActive(false);
        }

        while (cargos.Count >= 1)
        {
            if (!isPause)
            {
                if (!setStartPos)
                {
                    cargos[cargoNum].SetActive(true);
                    float cargoHeight = cargos[cargoNum].GetComponent<BoxCollider>().bounds.size.y;
                    float fallingStartPos = uldHeight + virtualULDWork.virtualULDPos.y;
                    cargos[cargoNum].transform.position = new Vector3(cargoPos[cargoNum].x, fallingStartPos + cargoHeight / 2, cargoPos[cargoNum].z);
                    visibleCargos.Add(cargos[cargoNum]);
                }
                setStartPos = true;
                cargos[cargoNum].transform.position = Vector3.MoveTowards(cargos[cargoNum].transform.position, cargoPos[cargoNum], fallingSpeed * Time.deltaTime);
                if (Vector3.Distance(cargos[cargoNum].transform.position, cargoPos[cargoNum]) < 0.01)
                {
                    cargos[cargoNum].transform.position = cargoPos[cargoNum];
                    setStartPos = false;
                    cargoNum++;
                }

                if (cargoNum >= cargos.Count)
                {
                    activeCoroutine = null;
                    yield break;
                }

                if (isReset)
                {
                    activeCoroutine = null;
                    yield break;
                }
                yield return null;
            }
            else
            {
                if (isReset)
                {
                    activeCoroutine = null;
                    yield break;
                }
                yield return null;
            }
        }
    }

    public void FallingSpeedUp(bool isOn)
    {
        if (isOn)
        {
            fallingSpeed = originSpeed * 2;
        }
        else
        {
            fallingSpeed = originSpeed;
        }
    }

    public void ULDReset()
    {
        isReset = true;
        for (int i = 0; i < cargos.Count; i++)
        {
            cargos[i].SetActive(false);
        }
    }

    public void ULDLoadTableState()
    {
        for (int i = 0; i < cargos.Count; i++)
        {
            cargos[i].transform.position = cargoPos[i];
            cargos[i].SetActive(true);
        }
        WorkEnd();
    }

    public void WorkEnd()
    {
        StopAllCoroutines();
        activeCoroutine = null;
    }

}
