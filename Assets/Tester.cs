using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PGD;

public class Tester : MonoBehaviour
{
    FlightManager flightManager;
    public GameObject testCargo;
    public FlightInfo flightInfo;
    public List<string> tmpCargoName;
    public List<Dictionary<string, object>> dataParsing = new List<Dictionary<string, object>>();
    public int test;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //flightManager.AddNewFlight(flightInfo);
            SimulationModeTaskManager.Instance.Igniter();
        }
        if (Input.GetKeyDown(KeyCode.N))
        {

            List<Storage> storages = SimulationModeTaskManager.Instance.asrs.storages;

            int cellIndex = int.MaxValue;
            Cell findCell = null;
            bool findEmptyCell;

            findEmptyCell = false;
            Cargo tmpCargo = SimulationModeTaskManager.Instance.cargoSpawnQueue.Dequeue();

            foreach (var item in storages)
            {
                if (item.FindEmptyCell(out var cell))
                {
                    if (cell.cellIndex < cellIndex)
                    {
                        cellIndex = cell.cellIndex;
                        findCell = cell;
                        findEmptyCell = true;
                    }
                }
            }

            if ( findEmptyCell)
           
            SimulationModeTaskManager.Instance.asrs.capacity++;
            findCell.PutCargo(tmpCargo);

        }


        if (Input.GetKeyDown(KeyCode.M))
        {
            SimulationModeTaskManager.Instance.CallReqeustPSResult();
        }



        if (Input.GetKeyDown(KeyCode.Q))
        {
            GameObject tmp = SimulationModeTaskManager.Instance.instantQueue.Dequeue().gameObject;
            Cell tmpcell = SimulationModeTaskManager.Instance.asrs.FindCargo(tmp.GetComponent<Cargo>().cargoID);

            SimulationModeTaskManager.Instance.networkManager.Post_CellCargoUpdate(tmpcell.cargo, false);
            tmpcell.PullCargo(); 

            SimulationModeTaskManager.Instance.asrs.capacity--;
            SimulationModeTaskManager.Instance.ForkLiftJobreport(null, tmp);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Cargo tmpCargo = SimulationModeTaskManager.Instance.cargoSpawnQueue.Dequeue();
            VMSAwbInfo vmsAwb = new VMSAwbInfo(
                tmpCargo.cargoID,
                "3d Model Name",
                tmpCargo.waterVolume,
                1,
                tmpCargo.width,
                tmpCargo.length,
                tmpCargo.depth,
                tmpCargo.weight,
                1,
                "saved",
                0,
                " ",
                true,
                "/c/file/xxx",
                1,
                "배송설명",
                tmpCargo.SCCs.ToArray(),
                21);

            string json = JsonUtility.ToJson(vmsAwb);
            SimulationModeTaskManager.Instance.networkManager.PostVMSAwb(json);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            SimulationModeTaskManager.Instance.ULDInputOrder(0, null);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            SimulationModeTaskManager.Instance.CallNetwork();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
        }



    }

}
