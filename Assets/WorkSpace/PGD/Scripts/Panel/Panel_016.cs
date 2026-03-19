using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_016 : MonoBehaviour
    {
        private CameraController cameraController;
        [SerializeField] private SCC scc;

        public string id;

        [SerializeField] private Image image_loadingRate;
        [SerializeField] private TMP_Text text_loadingRate;
        [SerializeField] private TMP_Text text_workingTime;
        [SerializeField] private TMP_Text text_Weight;
        [SerializeField] private Image[] image_sccIcon;
        [SerializeField] private Sprite sprite_sccBG;

        private string[] sccArray = { };

        private int MAX_ICON_COUNT = 3;

        void Awake()
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        private void OnEnable()
        {
            cameraController.MoveToTarget("ULD_" + id);
        }

        private void OnDisable()
        {
            cameraController.cancelTarget();
        }

        public void UpdateData(float loadingRate, string workingTime, float weight, string[] sccArray)
        {
            image_loadingRate.fillAmount = loadingRate / 100f;
            text_loadingRate.text = loadingRate.ToString() + "%";
            text_workingTime.text = workingTime;
            text_Weight.text = weight.ToString() + "kg";

            this.sccArray = sccArray;

            for (int i = 0; i < sccArray.Length; i++)
            {
                if (i >= MAX_ICON_COUNT)
                    break;

                if (scc.SCCMap.ContainsKey(sccArray[i]))
                {
                    image_sccIcon[i].sprite = scc.SCCMap[sccArray[i]];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    image_sccIcon[i].sprite = sprite_sccBG;
                    image_sccIcon[i].transform.GetChild(0).GetComponent<TMP_Text>().text = sccArray[i];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(true);
                }

                image_sccIcon[i].gameObject.SetActive(true);
            }
        }
        
        public void OnClickMoreBtn()
        {
            for (int i = 0; i < sccArray.Length; i++)
            {
                if (scc.SCCMap.ContainsKey(sccArray[i]))
                {
                    image_sccIcon[i].sprite = scc.SCCMap[sccArray[i]];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    image_sccIcon[i].sprite = sprite_sccBG;
                    image_sccIcon[i].transform.GetChild(0).GetComponent<TMP_Text>().text = sccArray[i];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(true);
                }

                image_sccIcon[i].gameObject.SetActive(true);
            }
        }
    }
}

