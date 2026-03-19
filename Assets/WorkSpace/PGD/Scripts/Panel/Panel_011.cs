using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_011 : MonoBehaviour
    {
        [Header("012")]
        [SerializeField] private GameObject prefab_amr;
        [SerializeField] private Transform amrSpawnPos;
        
        [Header("013")]
        [SerializeField] private GameObject prefab_asrs;
        [SerializeField] private Transform asrsSpawnPos;

        [Header("014")]
        [SerializeField] private GameObject prefab_vms;
        [SerializeField] private Transform vmsSpawnPos;

        [Header("016")]
        [SerializeField] private GameObject prefab_uld;
        [SerializeField] private Transform uldSpawnPos;

        private int completionCount;

        public void CreateAMRPopup(List<string> id)
        {
            for (int i = 0; i < id.Count; i++)
            {
                GameObject amrPopup = Instantiate(prefab_amr, gameObject.transform);
                UIManager.Instance.CreateBottomAMRButton(id[i]);
                amrPopup.name = "AMRPopup_" + id[i];

                Popup_012 amrScript = amrPopup.GetComponent<Popup_012>();
                amrScript.SetName(id[i]);

                PopupTargetObject targetObject = GameObject.Find("AMR_" + id[i]).GetComponent<PopupTargetObject>();
                targetObject.GetComponent<AMR>().floatingUI = this;
                targetObject.targetPopup = amrScript.gameObject;

                UIManager.Instance.spawnedAMRMap.Add(id[i], amrPopup);
            }
        }

        public void UpdateAMRData(string id, string destination, float remainingBattery)
        {
            if (UIManager.Instance.spawnedAMRMap.ContainsKey(id))
            {
                Popup_012 amrPopup = UIManager.Instance.spawnedAMRMap[id].GetComponent<Popup_012>();
                amrPopup.UpdateData(destination, remainingBattery);
            }
            else
            {
                Debug.LogError("AMR id not found");
            }
        }

        public void CreateASRSPopup(List<string> id)
        {
            for (int i = 0; i < id.Count; i++)
            {
                GameObject asrsPopup = Instantiate(prefab_asrs, gameObject.transform);
                UIManager.Instance.CreateBottomASRSButton(id[i]);
                asrsPopup.name = "ASRSPopup_" + id[i];

                Popup_013 asrsScript = asrsPopup.GetComponent<Popup_013>();
                asrsScript.SetName(id[i]);

                PopupTargetObject targetObject = GameObject.Find("Storage_" + id[i]).GetComponent<PopupTargetObject>();
                targetObject.targetPopup = asrsScript.gameObject;

                UIManager.Instance.spawnedASRSMap.Add(id[i], asrsPopup);
            }
        }

        public void UpdateASRRData(string id, float storageKappa, int storage, int outputWaiting)
        {
            if (UIManager.Instance.spawnedASRSMap.ContainsKey(id))
            {
                Popup_013 asrsPopup = UIManager.Instance.spawnedASRSMap[id].GetComponent<Popup_013>();
                asrsPopup.UpdateData(storageKappa, storage, outputWaiting);
            }
            else
            {
                Debug.LogError("ASRS id not found");
            }
        }

        public void CreateVMSPopup(string id)
        {
            GameObject vmsPopup = Instantiate(prefab_vms, gameObject.transform);
            UIManager.Instance.CreateBottomVMSButton(id);
            vmsPopup.name = "VMSPopup_" + id;

            Popup_014 vmsScript = vmsPopup.GetComponent<Popup_014>();
            vmsScript.SetName(id);
            print("VMS_" + id);
            PopupTargetObject targetObject = GameObject.Find("VMS_" + id).GetComponent<PopupTargetObject>();
            targetObject.transform.parent.GetComponent<VMSBeam>().floatingUI = this;
            targetObject.targetPopup = vmsScript.gameObject;

            UIManager.Instance.spawnedVMSMap.Add(id, vmsPopup);
        }

        public void UpdateVMSData(string id, string state, int measurementCounting)
        {
            Popup_014 vmsPopup = UIManager.Instance.spawnedVMSMap[id].GetComponent<Popup_014>();
            vmsPopup.UpdateData(state, measurementCounting);
        }

        public void CreateULDPopup(string id)
        {
            GameObject uldPopup = Instantiate(prefab_uld, gameObject.transform);
            UIManager.Instance.CreateBottomULDButton(id);
            uldPopup.name = "ULDPopup_" + id;

            Popup_016 uldScript = uldPopup.GetComponent<Popup_016>();
            uldScript.SetName(id);

            PopupTargetObject targetObject = GameObject.Find("ULD_" + id).GetComponent<PopupTargetObject>();
            targetObject.targetPopup = uldScript.gameObject;

            UIManager.Instance.spawnedULDMap.Add(id, uldPopup);
        }

        public void UpdateULDData(string id, float loadingRate, int averageLoadingRate)
        {        
            Popup_016 uldPopup = UIManager.Instance.spawnedULDMap[id].GetComponent<Popup_016>();
            uldPopup.UpdateData(loadingRate, averageLoadingRate);
        }

        public void UpdateULDCompletionTime(string id, float completionTimeAverage)
        {
            completionCount++;
            Popup_016 uldPopup = UIManager.Instance.spawnedULDMap[id].GetComponent<Popup_016>();

            int average = (int)completionTimeAverage / completionCount;
            int minute = average / 60;
            int second = average % 60;

            uldPopup.UpdateCompletionTimeData(minute, second);
        }
    }
}

