using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Storage : MonoBehaviour
{
    public string id;
    public List<Cell> cells;
    public int cellCount;
    public int capacity; 

    // Start is called before the first frame update
    void Start()
    {
        //Initialize();   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize()
    {
        cells.AddRange(this.transform.GetComponentsInChildren<Cell>());
        cells = cells.OrderBy(obj => obj.cellIndex).ToList();

        foreach(var item in cells)
        {
            item.Initialize(this);
        }
    }

    public bool FindEmptyCell(out Cell emptyCell)
    {
        foreach (var cell in cells)
        {
            if (cell.isBooked == false)
            {
                emptyCell = cell;
                return true;
            }
        }

        emptyCell = null;
        return false;
    }

    public bool FindCellIDByCargoID(string cargoID, out Cell foundedSell)
    {
        foreach (var cell in cells)
        {
            if (cell.cargo == null)
            {
                continue;
            }
            if (cell.cargo.cargoID.Equals(cargoID))
            {

                foundedSell = cell;
                return true;
            }
        }

        foundedSell = null;
        return false;
    }

    public void UpdateCellData(bool isPushed)
    {
        capacity += isPushed ? 1 : -1;

        
    }

}
