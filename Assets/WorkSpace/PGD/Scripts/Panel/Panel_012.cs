using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_012 : MonoBehaviour
    {
        private CameraController cameraController;

        public string id;
        private int number;

        [SerializeField] private TMP_Text text_name;
        [SerializeField] private TMP_Text text_workStatus;
        [SerializeField] private TMP_Text text_loadingCargo;
        [SerializeField] private TMP_Text text_start;
        [SerializeField] private TMP_Text text_end;
        [SerializeField] private Image image_remainingBattery;
        [SerializeField] private TMP_Text text_fieldCoordinates;
        [SerializeField] private TMP_Text text_setSpeed;
        [SerializeField] private TMP_Text text_communicationSpeed;

        void Awake()
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        private void OnEnable()
        {
            number = GameObject.Find("Button_012-" + id).transform.GetSiblingIndex();
            SetName();
            //cameraController.MoveToTarget("AMR_" + id);
        }

        private void OnDisable()
        {
            cameraController.cancelTarget();
            ResetPanel();
        }

        void Update()
        {
            cameraController.MoveToTarget("AMR_" + id);

            if (GameObject.Find("AMR_" + id).GetComponent<AMR>().cargo != null)
            {
                Cargo cargo = GameObject.Find("AMR_" + id).GetComponent<AMR>().cargo.GetComponent<Cargo>();
                UIManager.Instance.UpdateAMRLoadData(id, cargo.cargoName, "", cargo.waterVolume, cargo.weight, cargo.POU, cargo.SCCs, 1);
            }
        }

        private void SetName()
        {
            text_name.text = "юнют AMR #" + id;
        }

        private void ResetPanel()
        {
            text_workStatus.text = null;
            text_loadingCargo.text = null;
            text_start.text = null;
            text_end.text = null;
            image_remainingBattery.fillAmount = 1f;
            text_setSpeed.text = null;
            text_communicationSpeed.text = null;
        }

        public void UpdateData(string workStatus, string loadingCargo, string start, string end, float remainingBattery, float setSpeed, float communicationSpeed)
        {
            text_workStatus.text = workStatus;
            text_loadingCargo.text = loadingCargo;
            text_start.text = start;
            text_end.text = end;
            image_remainingBattery.fillAmount = remainingBattery / 100f;
            text_setSpeed.text = setSpeed.ToString();
            text_communicationSpeed.text = communicationSpeed.ToString();
        }

        public void UpdateRealTimeData(Vector3 fieldCoordinates)
        {
            text_fieldCoordinates.text = fieldCoordinates.x + "," + fieldCoordinates.z;
        }

        public void OnClickArrow(int value)
        {
            if (((number + value) < 0) || ((number + value) > UIManager.Instance.amrGroup.childCount - 1))
                return;

            number += value;

            SetName();

            UIManager.Instance.TurnOffBottomBtn(UIManager.Instance.amrGroup);
            UIManager.Instance.amrGroup.GetChild(number).gameObject.GetComponent<MainBottomBtnClickEvent>().TurnOn();
            UIManager.Instance.OnClickAMRBtn(UIManager.Instance.FindAmrDictionaryIndex(number));
        }

        public void OnClickLoadingCargoBtn()
        {
            if (GameObject.Find("AMR_" + id).GetComponent<AMR>().cargo != null)
            {
                UIManager.Instance.panel_amrLoad.GetComponent<Panel_015>().id = id;
                UIManager.Instance.panel_amrLoad.GetComponent<Panel_015>().isAMR = true;

                UIManager.Instance.panel_amrLoad.SetActive(true);
            }         
        }
    }
} 
