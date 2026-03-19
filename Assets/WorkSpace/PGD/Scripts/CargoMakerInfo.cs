using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoMakerInfo : MonoBehaviour
{
    [SerializeField]
    public string ID;
    public string Flight;
    public string POU;
    public string SCC;
    public string WV;
    public string x;
    public string y;
    public string z;
    public string W;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //public void SetInfo(Dictionary<string, object> datas)
    //{
    //    ID = datas[nameof(ID)].ToString();
    //    prefabName = datas[nameof(prefabName)].ToString();
    //    width = datas[nameof(width)].ToString();
    //    length =  datas[nameof(length)].ToString();
    //    height = datas[nameof(height)].ToString();
    //    volume_water = datas[nameof(volume_water)].ToString();
    //    volume_square = datas[nameof(volume_square)].ToString();
    //    weight = datas[nameof(weight)].ToString();
    //    isStructed = datas[nameof(isStructed)].ToString();
    //    barcode = datas[nameof(barcode)].ToString();  
    //    destination = datas[nameof(destination)].ToString();
    //    source = datas[nameof(source)].ToString();
    //    SCC = datas[nameof(SCC)].ToString();
    //    Breakable = datas[nameof(Breakable)].ToString();
    //    pieceCount = datas[nameof(pieceCount)].ToString();
    //    pieceID = datas[nameof(pieceID)].ToString();
    //    spawnRate = datas[nameof(spawnRate)].ToString();
    //}

    public string GetInfo()
    {
        string result;

        result = ID + "," + Flight + "," + POU + "," + SCC + "," + WV + "," + x + ","+ y + "," + z +"," + W;
        return result;
    }

}



