using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCCIcons : MonoBehaviour
{
    public Dictionary<string, Sprite> SCCMap;
    private Sprite[] sprite_scc;

    public void Initialize()
    {
        SCCMap = new Dictionary<string, Sprite>();

        sprite_scc = Resources.LoadAll<Sprite>("SCCIcon");

        for (int i = 0; i < sprite_scc.Length; i++)
        {
            SCCMap.Add(sprite_scc[i].name, sprite_scc[i]);
        }
    }
}
