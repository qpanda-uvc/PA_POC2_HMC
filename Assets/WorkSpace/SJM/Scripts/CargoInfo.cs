using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoInfo : MonoBehaviour
{
    public string cargoName;
    public string pou;
    public float wVolume;
    public float sVolume;
    public float volume;
    public float weight;
    public List<string> scc = new List<string>();

    public CargoInfo()
    {

    }

    public CargoInfo(string name, string pou, float wVolume, float sVolume, float volume, float weight)
    {
        this.cargoName = name;
        this.pou = pou;
        this.wVolume = wVolume;
        this.sVolume = sVolume;
        this.volume = volume;
        this.weight = weight;
    }
}
