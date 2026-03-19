using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PGD
{
    public class Cell_013 : MonoBehaviour
    {
        public int id;

        private bool isPlaced;
        private float second;
        private int minute;

        [SerializeField] private GameObject placedUI;
        [SerializeField] private GameObject notPlacedUI;

        [SerializeField] private TMP_Text text_name;
        [SerializeField] private TMP_Text text_cargoID;
        [SerializeField] public TMP_Text text_destination;
        [SerializeField] private TMP_Text text_standBy;
        [SerializeField] private TMP_Text text_stanByName;

        public Cell cell;
        private Button button;
   
        void Start()
        {
            button = transform.GetChild(3).GetComponent<Button>();
        }

        private void Update()
        {
            if (isPlaced)
            {
                second += Time.deltaTime;

                text_standBy.text = string.Format("{0:D2}:{1:D2}", minute, (int)second);
                if ((int)second > 59)
                {
                    second = 0;
                    minute++;
                }
            }
            else
            {
                second = 0;
                minute = 0;
                text_standBy.text = null;
            }

            button.interactable = transform.parent.GetComponent<CanvasGroup>().alpha == 1;
        }

        public void SetName()
        {     
            gameObject.name = "Cell_" + id;
            text_name.text = "Cell_" + id;
            text_stanByName.text = "Cell_" + id;          
        }

        public void UpdateData(bool isPlaced, string cargoID, string destination)
        {
            this.isPlaced = isPlaced;

            if (isPlaced)
            {
                placedUI.SetActive(true);
                notPlacedUI.SetActive(false);     
                text_cargoID.text = cargoID;
                text_destination.text = destination;
            }
            else
            {
                placedUI.SetActive(false);
                notPlacedUI.SetActive(true);
            }
        }


        public void OnClickPopup()
        {
            UIManager.Instance.panel_amrLoad.SetActive(false);

            UIManager.Instance.panel_amrLoad.GetComponent<Panel_015>().id = id.ToString();
            UIManager.Instance.panel_amrLoad.GetComponent<Panel_015>().isAMR = false;

            Cargo cargo = cell.cargo;

            if(cargo != null)
            {
                UIManager.Instance.panel_amrLoad.GetComponent<Panel_015>().target = cargo.gameObject;
                UIManager.Instance.UpdateASRSLoadCellData(cargo.cargoName, "", cargo.waterVolume, cargo.weight, cargo.POU, cargo.SCCs, 1);
                UIManager.Instance.panel_amrLoad.SetActive(true);
            }
        }
    }
}

