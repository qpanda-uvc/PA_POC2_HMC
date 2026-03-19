using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PGD;

public class Cell : MonoBehaviour
{
    public int cellIndex;
    public Cargo cargo;
    public bool hasCargo;
    public bool isBooked;
    string cellID;
    public Storage parentStorage;
    // Start is called before the first frame update

    public void Initialize(Storage parentStorage)
    {
        this.parentStorage = parentStorage;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PutCargo(Cargo inputCargo)
    {
        

        cargo = inputCargo;
        hasCargo = true;
        isBooked = true;

        
        cargo.beltOccupy.Clear();
        cargo.currentLocation = this.cellIndex.ToString();
        parentStorage.UpdateCellData(true);
        if (SimulationModeTaskManager.Instance != null && SimulationModeTaskManager.Instance.networkManager != null)
            SimulationModeTaskManager.Instance.networkManager.Post_CellCargoUpdate(this.cargo, true);
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateASRSCellData(cellIndex.ToString(), true, inputCargo.cargoName, inputCargo.POU);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Cargo>(out var cargo))
        {
            //PullCargo();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Cargo>(out var cargo))
        {
            //PutCargo(cargo);
        }

        
    }

    public void PullCargo()
    {
        cargo = null;
        hasCargo = false;
        isBooked = false;

        parentStorage.UpdateCellData(false);
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateASRSCellData(cellID, false, null, null);

    }
}
