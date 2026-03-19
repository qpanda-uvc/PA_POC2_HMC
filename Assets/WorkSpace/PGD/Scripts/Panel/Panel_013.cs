using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_013 : MonoBehaviour
    {
        private CameraController cameraController;
        [SerializeField] private SCC scc;

        public string id;
        private int number;

        [SerializeField] private TMP_Text text_name;
        [SerializeField] private Image image_warehousCAPA;
        [SerializeField] private TMP_Text text_warehousCAPA;
        [SerializeField] private Image image_flightInputRate;
        [SerializeField] private TMP_Text text_flightInputRate;
        [SerializeField] private TMP_Text[] text_destinationFiltering;
        [SerializeField] private TMP_Text[] text_conditionFiltering;
        [SerializeField] private TMP_InputField inputField_sccFiltering;
        [SerializeField] private Image[] image_sccIcon;

        [SerializeField] private GameObject canvas_panel013;
        [SerializeField] private Sprite sprite_sccBG;

        [SerializeField] private Transform destinationFilteringGroup;
        [SerializeField] private Transform stateFilteringGroup;

        // 다른 스크립트에서 가져오기
        string[] destinationFiltering = { "SIN", "ORD", "PEN" };
        string[] stateFiltering = { "대기상태", "불출예정" }; 
        string[] bookmark = { "ART", "AAA", "VIP" }; 

        int childNum;
        int prevNum = -1;
        bool? prevBool = null;

        void Awake()
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }


        void OnEnable()
        {
            number = GameObject.Find("Button_013-" + id).transform.GetSiblingIndex();
            SetName();
            cameraController.MoveToTarget("Storage_" + id);     
        }

        void Start()
        {
            for (int i = 0; i < destinationFiltering.Length; i++)
            {
                text_destinationFiltering[i].text = destinationFiltering[i];
            }

            for (int i = 0; i < stateFiltering.Length; i++)
            {
                text_conditionFiltering[i].text = stateFiltering[i];
            }

            for (int i = 0; i < bookmark.Length; i++)
            {
                if (scc.SCCMap.ContainsKey(bookmark[i]))
                {
                    image_sccIcon[i].sprite = scc.SCCMap[bookmark[i]];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    image_sccIcon[i].sprite = sprite_sccBG;
                    image_sccIcon[i].transform.GetChild(0).GetComponent<TMP_Text>().text = bookmark[i];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(true);
                }
                image_sccIcon[i].gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            cameraController.cancelTarget();
            ResetCell();
        }

        private void SetName()
        {
            text_name.text = "자동창고 #" + id;

        }
        public void UpdateData(float warehousCAPA, float flightInputRate)
        {
            image_warehousCAPA.fillAmount = warehousCAPA / 100f;
            text_warehousCAPA.text = warehousCAPA.ToString() + "%";
            image_flightInputRate.fillAmount = flightInputRate / 100f;
            text_flightInputRate.text = flightInputRate.ToString() + "%";
        }

        public void OnClickDestinationBtn(int num)
        {
            childNum = id == UIManager.Instance.FindAsrsDictionaryIndex(0) ? 0 : 1;

            ResetCell();

            if (num == prevNum)
            {
                prevNum = -1;
                return;
            }
   
            for (int i = 0; i < canvas_panel013.transform.GetChild(childNum).childCount; i++)
            {
                if (canvas_panel013.transform.GetChild(childNum).GetChild(i).GetComponent<Cell_013>().text_destination.text == destinationFilteringGroup.GetChild(num).GetChild(0).GetComponent<TMP_Text>().text)
                    canvas_panel013.transform.GetChild(childNum).GetChild(i).GetComponent<Cell_013>().transform.GetChild(0).GetComponent<Image>().color = new Color(1f, 0f, 0f, 1f);
            }

            prevNum = num;
        }

        private void ResetCell()
        {
            for (int i = 0; i < canvas_panel013.transform.GetChild(childNum).childCount; i++)
            {
                canvas_panel013.transform.GetChild(childNum).GetChild(i).GetComponent<Cell_013>().transform.GetChild(0).GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            }
        }


        public void OnClickStateBtn(bool state)
        {
            childNum = id == UIManager.Instance.FindAsrsDictionaryIndex(0) ? 0 : 1;

            ResetCell();

            if (state == prevBool)
            {
                prevBool = null;
                return;
            }

            for (int i = 0; i < canvas_panel013.transform.GetChild(childNum).childCount; i++)
            {
                Cargo cargo = canvas_panel013.transform.GetChild(childNum).GetChild(i).GetComponent<Cell_013>().cell.cargo;
               
                if (cargo != null && cargo.isBookedForPull == state)
                    canvas_panel013.transform.GetChild(childNum).GetChild(i).GetComponent<Cell_013>().transform.GetChild(0).GetComponent<Image>().color = new Color(1f, 0f, 0f, 1f);
            }

            prevBool = state;
        }

        public void OnClickSCCEnterBtn()
        {
            ResetCell();

            for (int i = 0; i < canvas_panel013.transform.GetChild(childNum).childCount; i++)
            {
                if (canvas_panel013.transform.GetChild(childNum).GetChild(i).GetComponent<Cell_013>().text_destination.text == inputField_sccFiltering.text)
                    canvas_panel013.transform.GetChild(childNum).GetChild(i).GetComponent<Cell_013>().transform.GetChild(0).GetComponent<Image>().color = new Color(1f, 0f, 0f, 1f);
            }     
        }

        private void UpdateWorldData()
        {

        }

        public void OnClickArrow(int value)
        {
            if (((number + value) < 0) || ((number + value) > UIManager.Instance.asrsGroup.childCount - 1))
                return;

            number += value;

            SetName();

            UIManager.Instance.TurnOffBottomBtn(UIManager.Instance.asrsGroup);
            UIManager.Instance.asrsGroup.GetChild(number).gameObject.GetComponent<MainBottomBtnClickEvent>().TurnOn();
            UIManager.Instance.OnClickASRSBtn(UIManager.Instance.FindAsrsDictionaryIndex(number));
        }
    }
}
