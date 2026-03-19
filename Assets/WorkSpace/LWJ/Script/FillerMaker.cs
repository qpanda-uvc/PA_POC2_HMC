using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LWJ
{
    public class FillerMaker : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField]
        GameObject filler;
        [SerializeField]
        GameObject parent;

        void Start()
        {
            GenerateFiller();   
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void GenerateFiller()
        {
            List<Cargo_Dummy> underList = new List<Cargo_Dummy>();
            List<Cargo_Dummy> cargo_Dummies = new List<Cargo_Dummy>();


            cargo_Dummies.AddRange(FindObjectsOfType<Cargo_Dummy>());

            foreach (var item in cargo_Dummies)
            {
                item.SetSpec();
            }

            foreach (var item in cargo_Dummies)
            {
                underList.Clear();

                foreach (var compareItem in cargo_Dummies)
                {

                    // is under 
                    if (item.bottom > compareItem.top)
                    {
                        // is under, but out of range
                        if (item.left > compareItem.right
                                || item.right < compareItem.left)
                        {
                            continue;
                        }
                        // is under, not out of range, but under cargo is bigger
                        if (item.left > compareItem.left
                                && item.right < compareItem.right)
                        {
                            continue;
                        }

                        underList.Add(compareItem);
                    }

                }

                foreach (var underCargo in underList)
                {
                    GameObject tmpFiller = Instantiate(filler, parent.transform);
                    tmpFiller.name = item.name + underCargo.name;

                    Vector3 fillerPosition = new Vector3();
                    Vector3 fillerSize = new Vector3();

                    //placed cargo is full cover under cargo
                    if (item.left < underCargo.left
                            && item.right > underCargo.right)
                    {
                        fillerPosition.x = underCargo.transform.position.x;
                        fillerSize.x = underCargo.right - underCargo.left;
                    }
                    //placed cargo is further right
                    else if (item.transform.position.x < underCargo.transform.position.x)
                    {
                        fillerPosition.x = underCargo.left + ((item.right - underCargo.left) / 2);
                        fillerSize.x = underCargo.right - item.left;
                    }
                    //placed cargo is further left 
                    else if (item.transform.position.x > underCargo.transform.position.x)
                    {
                        fillerPosition.x = item.left + ((underCargo.right - item.left) / 2);
                        fillerSize.x = item.left - underCargo.right;
                    }

                    fillerPosition.y = underCargo.top + ((item.bottom - underCargo.top) / 2);
                    fillerSize.y = item.bottom - underCargo.top;

                    if (item.front < underCargo.front
                            && item.rear > underCargo.rear)
                    {
                        fillerPosition.z = underCargo.transform.position.z;
                        fillerSize.z = underCargo.rear - underCargo.front;
                    }

                    fillerPosition.z = underCargo.transform.position.z;
                    fillerSize.z = 1f;


                    tmpFiller.transform.position = fillerPosition;
                    tmpFiller.transform.localScale = fillerSize;

                }

            }

        }

        bool IsDirectlyUnder()
        {
            return false;
        }
    }

}
