using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PGD
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance = null;

        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    return null;
                }
                return instance;
            }

        }
        private List<GameObject> panelList = new List<GameObject>();

        [SerializeField] public GameObject panel_main;
        [SerializeField] private GameObject panel_simulation;
        [SerializeField] private GameObject panel_connectionError;

        [SerializeField] private Image slider_flightProgress;
        [SerializeField] private TMP_Text text_precent;

        [SerializeField] public GameObject canvas_world;


        // MeasuringInstrument
        [SerializeField] private TMP_Text text_workingHoursMI;
        [SerializeField] private TMP_Text text_detectionError;


        // Container
        [SerializeField] private TMP_Text text_completionAverage;
        [SerializeField] private TMP_Text text_averageLoadingRate;

        // AutomaticWarehouse
        [SerializeField] private TMP_Text text_completionAverage2;
        [SerializeField] private TMP_Text text_averageLoadingRate2;

        // AMR
        [SerializeField] private TMP_Text text_workingHoursAMR;


        // camera toggle
        [SerializeField] private TMP_Text text_3d;
        [SerializeField] private TMP_Text text_2d;
        [SerializeField] private Image image_3dBg;
        [SerializeField] private Image image_2dBg;

        [SerializeField] private Image image_notice;

        // bottom ui
        [SerializeField] private Transform canvas_bottom;
        public Transform bottomGroup;
        public Transform amrGroup;
        public Transform asrsGroup;
        [SerializeField] private GameObject panel_amr;
        [SerializeField] private GameObject panel_asrs;
        [SerializeField] private GameObject panel_vms;
        [SerializeField] private GameObject panel_uld;
        [SerializeField] public GameObject panel_amrLoad;
        [SerializeField] private GameObject panel_operationStatus;

        public GameObject canvas_013_1, canvas_013_2;


        // new Version
        [SerializeField] private GameObject prefab_amrButton;
        [SerializeField] private GameObject prefab_asrsButton;
        [SerializeField] private GameObject prefab_vmsButton;
        [SerializeField] private GameObject prefab_uldButton;

        public Dictionary<string, GameObject> spawnedAMRMap = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> spawnedASRSMap = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> spawnedVMSMap = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> spawnedULDMap = new Dictionary<string, GameObject>();

        int dictionaryCount;


        // ASRS Cell
        [SerializeField] private GameObject prefab_cellPopup;
        [SerializeField] private GameObject cell1Group, cell2Group;

        public Coroutine noticeCoroutine;
        private bool isStartCoroutin;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
               // DontDestroyOnLoad(gameObject);
            }
            else
            {
              //  Destroy(gameObject);
            }
            if (panel_main != null) panel_main.SetActive(false);
            if (panel_simulation != null) panel_simulation.SetActive(false);
            FindAllButtonAndPanelObjects();
        }
    
        // �� �� ��ư, �г� ã�� 
        private void FindAllButtonAndPanelObjects()
        {
            Transform[] allObjects = Resources.FindObjectsOfTypeAll<Transform>();

            for (int i = 0; i < allObjects.Length; i++)
            {
                string[] split = allObjects[i].name.Split('_');

                if (split[0].Equals("Button"))
                {
                    var button = allObjects[i].gameObject;

                    if (button.GetComponent<Button>() == null)
                    {
                        button.AddComponent<Button>();
                        button.AddComponent<ButtonUI>();
                        button.GetComponent<ButtonUI>().FindPanel();
                    }
                }
                else if (split[0].Equals("Panel"))
                {
                    var panel = allObjects[i].gameObject;
                    panelList.Add(panel);

                    if (panel.GetComponent<PanelUI>() == null)
                    {
                        panel.AddComponent<PanelUI>();
                        //panel.GetComponent<PanelUI>().FindCloseBtn();
                    }
                }
            }
        }

        public void CreateBottomAMRButton(string id)
        {
            if (prefab_amrButton == null || amrGroup == null) return;
            GameObject amrButton = Instantiate(prefab_amrButton, amrGroup.transform);
            amrButton.GetComponent<Button>().onClick.AddListener(() => OnClickAMRBtn(id));
            amrButton.name = "Button_012-" + id;
            amrButton.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = "ARM_" + id;
        }

        public void CreateBottomASRSButton(string id)
        {
            if (prefab_asrsButton == null || asrsGroup == null) return;
            GameObject asrsButton = Instantiate(prefab_asrsButton, asrsGroup.transform);
            asrsButton.GetComponent<Button>().onClick.AddListener(() => OnClickASRSBtn(id));
            asrsButton.name = "Button_013-" + id;
            asrsButton.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = "ASRS_" + id;
        }

        public void CreateBottomVMSButton(string id)
        {
            if (prefab_vmsButton == null) return;
            prefab_vmsButton.GetComponent<Button>().onClick.AddListener(() => OnClickVMSBtn(id));
            prefab_vmsButton.name = "Button_014-" + id;       
        }

        public void CreateBottomULDButton(string id)
        {
            if (prefab_uldButton == null) return;
            prefab_uldButton.GetComponent<Button>().onClick.AddListener(() => OnClickULDBtn(id));
            prefab_uldButton.name = "Button_016-" + id;           
        }

        public void CreateCellPopup(List<Cell> cellList)
        {
            if (cell1Group == null || cell2Group == null) return;
            int dev = cellList.Count / 2;

            for (int i = 0; i < cellList.Count; i++)
            {
                GameObject cellPopup;
                if (dev > i)
                {
                    cellPopup = Instantiate(prefab_cellPopup, cell1Group.transform);
                    cellPopup.transform.position = new Vector3(cellList[i].gameObject.transform.position.x, cellList[i].gameObject.transform.position.y, cellList[i].gameObject.transform.position.z + 4f);
                    cellPopup.transform.eulerAngles = new Vector3(0f, 180f, 0f);
                }
                else
                {
                    cellPopup = Instantiate(prefab_cellPopup, cell2Group.transform);
                    cellPopup.transform.position = new Vector3(cellList[i].gameObject.transform.position.x, cellList[i].gameObject.transform.position.y, cellList[i].gameObject.transform.position.z - 2f);
                    cellPopup.transform.eulerAngles = Vector3.zero;
                }

                cellPopup.GetComponent<Cell_013>().cell = cellList[i];
                cellPopup.GetComponent<Cell_013>().id = cellList[i].cellIndex;    
                cellPopup.GetComponent<Cell_013>().SetName();
            }
        }

        public string FindAmrDictionaryIndex(int number)
        {
            dictionaryCount = 0;
            foreach (var item in spawnedAMRMap)
            {
                if (dictionaryCount == number)
                {
                    return item.Key;
                }
                dictionaryCount++;
            }
            return null;
        }

        public string FindAsrsDictionaryIndex(int number)
        {
            dictionaryCount = 0;
            foreach (var item in spawnedASRSMap)
            {
                if (dictionaryCount == number)
                {
                    return item.Key;
                }
                dictionaryCount++;
            }
            return null;
        }

        public void UpdateAMRPanelData(string id, string workStatus, string loadingCargo, string start, string end, float remainingBattery, float setSpeed, float communicationSpeed)
        {
            if (panel_amr == null) return;
            if (panel_amr.GetComponent<Panel_012>().id == id)
            {
                panel_amr.GetComponent<Panel_012>().UpdateData(workStatus, loadingCargo, start, end, remainingBattery, setSpeed, communicationSpeed);
            }
        }

        public void UpdateAMRPanelRealTimeData(string id, Vector3 fieldCoordinates)
        {
            if (panel_amr == null) return;
            if (panel_amr.GetComponent<Panel_012>().id == id)
            {
                panel_amr.GetComponent<Panel_012>().UpdateRealTimeData(fieldCoordinates);
            }
        }

        public void UpdateAMRLoadData(string id, string dbName, string type, float volume, float weight, string destination, List<string> sccArray, int workStep)
        {
            if (panel_amr == null || panel_amrLoad == null) return;
            if (panel_amr.GetComponent<Panel_012>().id == id)
            {
                panel_amrLoad.GetComponent<Panel_015>().UpdateData(dbName, type, volume, weight, destination, sccArray, workStep);
            }
        }

        public void UpdateASRSLoadCellData(string dbName, string type, float volume, float weight, string destination, List<string> sccArray, int workStep)
        {
            if (panel_amrLoad == null) return;
            panel_amrLoad.GetComponent<Panel_015>().UpdateData(dbName, type, volume, weight, destination, sccArray, workStep);
        }

        public void UpdataAsrsPanelData(string id, float warehousCAPA, float flightInputRate)
        {
            if (panel_asrs == null) return;
            if (panel_asrs.GetComponent<Panel_013>().id == id)
            {
                panel_asrs.GetComponent<Panel_013>().UpdateData(warehousCAPA, flightInputRate);
            }
        }

        public void UpdateVMSPanelData(string generatedCargoID, string type, float volume, float weight, string destination, string[] cargoProperties)
        {
            if (panel_vms == null) return;
            panel_vms.GetComponent<Panel_014>().UpdateData(generatedCargoID, type, volume, weight, destination, cargoProperties);
        }

        public void UpdateASRSCellData(string id, bool isPlaced, string cargoID, string destination)
        {
            if (canvas_013_1 == null || canvas_013_2 == null) return;
            if (canvas_013_1.transform.Find("Cell_" + id))
                canvas_013_1.transform.Find("Cell_"+id).GetComponent<Cell_013>().UpdateData(isPlaced, cargoID, destination);
            else if (canvas_013_2.transform.Find("Cell_" + id))
                canvas_013_2.transform.Find("Cell_" + id).GetComponent<Cell_013>().UpdateData(isPlaced, cargoID, destination);
        }

        public void UpdateULDPanelData(float loadingRate, string workingTime, float weight, string[] sccArray)
        {
            if (panel_uld == null) return;
            panel_uld.GetComponent<Panel_016>().UpdateData(loadingRate, workingTime, weight, sccArray);
        }


        // ���� ȭ�� ����
        public void UpdateMainUI(float progressedRatio)
        {
            if (slider_flightProgress == null) return;
            slider_flightProgress.fillAmount = progressedRatio;
            text_precent.text = (slider_flightProgress.fillAmount* 100).ToString();

        }


        // ī�޶� ��ȯ
        public void OnClickCameraToggle()
        {
            StateManager.Instance.Is3D = !StateManager.Instance.Is3D;

            if (StateManager.Instance.Is3D)
            {
                image_3dBg.gameObject.SetActive(true);
                image_2dBg.gameObject.SetActive(false);
                text_3d.color = new Color(1f, 1f, 1f, 1f);
                text_2d.color = new Color(0 / 255f, 44 / 255f, 95 / 255f, 1f);
            }
            else
            {
                image_3dBg.gameObject.SetActive(false);
                image_2dBg.gameObject.SetActive(true);
                text_3d.color = new Color(0 / 255f, 44 / 255f, 95 / 255f, 1f);
                text_2d.color = new Color(1f, 1f, 1f, 1f);
            }
        }

        // ���ε�
        public void OnClickReloadBtn()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        //// ����
        //public void OnClickNoticeBtn()
        //{

        //}

        // �ùķ��̼�
        public void OnClickSimulationBtn()
        {
            if (CheckServerConnection())
            {
                if (panel_main != null) panel_main.SetActive(false);
                if (panel_simulation != null) panel_simulation.SetActive(true);
            }
            else
            {
                panel_connectionError.SetActive(true);
            }
        }

        // Ŀ��Ƽ��
        public void OnClickConnectedBtn()
        {
            if (CheckServerConnection())
            {
                if (panel_main != null) panel_main.SetActive(false);
                StateManager.Instance.isSimulationMode = false;
            }
            else
            {
                panel_connectionError.SetActive(true);
            }
        }

        // UI toggle
        public void OnClickUIToggleBtn()
        {
            StateManager.Instance.isShowUI = !StateManager.Instance.isShowUI;

            if (canvas_world != null) canvas_world.SetActive(StateManager.Instance.isShowUI);
        }

        public void OnClickBottomRootBtn()
        {
            InitializePanel();
        }

        public void OnClickAMRBtn(string name)
        {
            if (panel_amr == null) return;
            InitializePanel();

            panel_amr.GetComponent<Panel_012>().id = name;
            panel_amr.SetActive(true);
        }

        public void OnClickASRSBtn(string name)
        {
            if (panel_asrs == null) return;
            InitializePanel();
            panel_asrs.GetComponent<Panel_013>().id = name;
            panel_asrs.SetActive(true);
        }

        public void OnClickVMSBtn(string name)
        {
            if (panel_vms == null) return;
            InitializePanel();
            panel_vms.GetComponent<Panel_014>().id = name;
            panel_vms.SetActive(true);
        }

        public void OnClickULDBtn(string name)
        {
            if (panel_uld == null) return;
            InitializePanel();
            panel_uld.GetComponent<Panel_016>().id = name;
            panel_uld.SetActive(true);
        }

        public void OnClickFlightOperationStatus()
        {
            if (panel_operationStatus == null) return;
            panel_operationStatus.SetActive(true);
        }

        public void HideBottomUi()
        {
            //canvas_bottom.DOMoveY(-400f, 1f);
        }

        public void TurnOffBottomBtn(Transform group)
        {
            for (int i = 0; i < group.childCount; i++)
            {
                if (group.GetChild(i).gameObject.name.Split('_')[0] == "Button")
                {
                    group.GetChild(i).gameObject.GetComponent<MainBottomBtnClickEvent>().TurnOff();     
                }                
            }
        }

        public void ShowBottomUi()
        {
            //canvas_bottom.DOMoveY(0f, 1f);
            InitializeBottomBtn();
        }

        private void InitializeBottomBtn()
        {
            if (bottomGroup == null || amrGroup == null || asrsGroup == null) return;
            for (int i = 0; i < bottomGroup.childCount; i++)
            {
                if (bottomGroup.GetChild(i).gameObject.name.Split('_')[0] == "Button")
                {
                    bottomGroup.GetChild(i).gameObject.GetComponent<MainBottomBtnClickEvent>().isClick = false;
                    bottomGroup.GetChild(i).gameObject.GetComponent<MainBottomBtnClickEvent>().TurnOff();
                }
            }
            for (int i = 0; i < amrGroup.childCount; i++)
            {
                amrGroup.GetChild(i).gameObject.GetComponent<MainBottomBtnClickEvent>().isClick = false;
                amrGroup.GetChild(i).gameObject.GetComponent<MainBottomBtnClickEvent>().TurnOff();
            }
            for (int i = 0; i < asrsGroup.childCount; i++)
            {
                asrsGroup.GetChild(i).gameObject.GetComponent<MainBottomBtnClickEvent>().isClick = false;
                asrsGroup.GetChild(i).gameObject.GetComponent<MainBottomBtnClickEvent>().TurnOff();
            }
        }

        public void InitializePanel()
        {
            Transform parent = GameObject.Find("Popups").transform;
            for (int i = 0; i < parent.childCount; i++)
            { 
                if (parent.transform.GetChild(i).gameObject.GetComponent<PanelUI>() != null)
                {
                    parent.transform.GetChild(i).gameObject.GetComponent<PanelUI>().CloseAllPanel();
                }
            }
        }

        public void ChangeNoticeColor()
        {
            if (image_notice == null) return;
            isStartCoroutin = true;
            noticeCoroutine = StartCoroutine(ChangeColor());
        }

        public IEnumerator ChangeColor()
        {
            while(true)
            {   
                image_notice.color = Color.yellow;
                yield return new WaitForSeconds(0.5f);
                image_notice.color = Color.white;
                yield return new WaitForSeconds(0.5f);
            }       
        }

        public void SetDefaultNoticeColor()
        {
            if (image_notice == null) return;
            if(isStartCoroutin)
                StopCoroutine(noticeCoroutine);
            
            isStartCoroutin = false;
            image_notice.color = Color.white;
        }


        public bool CheckServerConnection()
        {
            // ���� ���� üũ

            return true;
        }
    }
}

