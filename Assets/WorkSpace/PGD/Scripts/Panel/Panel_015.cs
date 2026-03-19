using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_015 : MonoBehaviour
    {
        [SerializeField] private SCC scc;

        public string id;
        private bool isClickImage;
        public bool isAMR;

        [SerializeField] private Camera camera_load;  

        [SerializeField] private TMP_Text text_name;
        [SerializeField] private RawImage image_load;
        [SerializeField] private RenderTexture texture_minCamera;
        [SerializeField] private RenderTexture texture_maxCamera;
        [SerializeField] private TMP_Text text_generatedCargoID;
        [SerializeField] private TMP_Text text_type;
        [SerializeField] private TMP_Text text_volume;
        [SerializeField] private TMP_Text text_weight;
        [SerializeField] private TMP_Text text_destination;
        [SerializeField] private Image[] image_sccIcon;
        [SerializeField] private Image image_remainingBattery;

        [SerializeField] private Sprite sprite_sccBG;

        public GameObject target;
        Vector3 targetPos;

        private int MAX_ICON_COUNT = 3;

        private void OnEnable()
        {
            camera_load.gameObject.SetActive(true);
            text_name.text = id;

            if(isAMR)
            {
                target = GameObject.Find("AMR_" + id);
            }
            else
            {
                targetPos = new Vector3(target.transform.position.x, target.transform.position.y + 1.8f, target.transform.position.z - 0.5f);
            }

            camera_load.transform.position = targetPos;
            camera_load.transform.eulerAngles = new Vector3(70f, 0f, 0f);
        }

        private void OnDisable()
        {
            SetDefaultLoadCamera();
            camera_load.gameObject.SetActive(false);
        }

        void Update()
        {
            if (isAMR)
            {
                targetPos = new Vector3(target.transform.position.x, target.transform.position.y + 1.2f, target.transform.position.z - 0.5f);
                camera_load.transform.position = targetPos; 
            }
        }

        void SetDefaultLoadCamera()
        {
            isClickImage = false;
            image_load.GetComponent<RectTransform>().sizeDelta = new Vector2(960f, 360f);
            image_load.texture = texture_minCamera;
            camera_load.targetTexture = texture_minCamera;
        }


        public void UpdateData(string dbName, string type, float volume, float weight, string destination, List<string> sccArray, int workStep)
        {
            text_generatedCargoID.text = dbName;
            text_type.text = type;
            text_volume.text = volume.ToString();
            text_weight.text = weight.ToString();
            text_destination.text = destination;

            for (int i = 0; i < sccArray.Count; i++)
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

            image_remainingBattery.fillAmount = 100f * 1/6 * workStep / 100f;
        }

        public void OnClickSizeUpCameraImage()
        {
            isClickImage = !isClickImage;
            image_load.GetComponent<RectTransform>().sizeDelta = isClickImage ? new Vector2(960f, 800f) : new Vector2(960f, 360f);
            image_load.texture = isClickImage ? texture_maxCamera : texture_minCamera;
            camera_load.targetTexture = texture_maxCamera;
        }

        public void OnClickCloseBtn()
        {
            gameObject.SetActive(false);
        }
    }
}

