using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGD
{
    public class SCC : MonoBehaviour
    {
        public Dictionary<string, Sprite> SCCMap;
        private Sprite[] sprite_scc;

        void Start()
        {
            SCCMap = new Dictionary<string, Sprite>();

            sprite_scc = Resources.LoadAll<Sprite>("SCCIcon");

            for (int i = 0; i < sprite_scc.Length; i++)
            {
                SCCMap.Add(sprite_scc[i].name, sprite_scc[i]);
            }    
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
