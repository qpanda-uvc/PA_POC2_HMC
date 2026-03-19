using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_014 : MonoBehaviour
    {
        private CameraController cameraController;
        [SerializeField] private SCC scc;

        public string id;
        private bool isClickImage;

        [SerializeField] private Camera camera_load;
        [SerializeField] private RawImage image_load;
        [SerializeField] private RenderTexture texture_minCamera;
        [SerializeField] private RenderTexture texture_maxCamera;
        [SerializeField] private TMP_Text text_generatedCargoID;
        [SerializeField] private TMP_Text text_type;
        [SerializeField] private TMP_Text text_volume;
        [SerializeField] private TMP_Text text_weight;
        [SerializeField] private TMP_Text text_destination;
        [SerializeField] private Image[] image_sccIcon;

        [SerializeField] private Sprite sprite_sccBG;

        public int MAX_ICON_COUNT = 3;

        void Awake()
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        private void OnEnable()
        {
            camera_load.gameObject.SetActive(true);
            Vector3 objectPos = GameObject.Find("VMS_" + id).transform.position;
            Vector3 targetPos = new Vector3(objectPos.x + 4f, objectPos.y, objectPos.z - 2f);
            
            camera_load.transform.position = targetPos;
            camera_load.transform.eulerAngles = new Vector3(0f, -55f, 0f);
            cameraController.MoveToTarget("VMS_" + id);
        }

        private void OnDisable()
        {
            SetDefaultLoadCamera();
            cameraController.cancelTarget();

            for (int i = 0; i < image_sccIcon.Length; i++)
            {
                image_sccIcon[i].gameObject.SetActive(false);
            }
        }

        void SetDefaultLoadCamera()
        {
            isClickImage = false;
            image_load.GetComponent<RectTransform>().sizeDelta = new Vector2(960f, 360f);
            image_load.texture = texture_minCamera;
            camera_load.targetTexture = texture_minCamera;
        }

        public void UpdateData(string generatedCargoID, string type, float volume, float weight, string destination, string[] cargoProperties)
        {
            text_generatedCargoID.text = generatedCargoID;
            text_type.text = type;
            text_volume.text = volume.ToString() + "L";
            text_weight.text = weight.ToString() + "kg";
            text_destination.text = destination;

            for (int i = 0; i < image_sccIcon.Length; i++)
            {
                image_sccIcon[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < cargoProperties.Length; i++)
            {
                if (i >= MAX_ICON_COUNT)
                    break;

                if (scc.SCCMap.ContainsKey(cargoProperties[i]))
                {
                    image_sccIcon[i].sprite = scc.SCCMap[cargoProperties[i]];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    image_sccIcon[i].sprite = sprite_sccBG;
                    image_sccIcon[i].transform.GetChild(0).GetComponent<TMP_Text>().text = cargoProperties[i];
                    image_sccIcon[i].transform.GetChild(0).gameObject.SetActive(true);
                }
                image_sccIcon[i].gameObject.SetActive(true);
            }
        }

        public void OnClickSizeUpCameraImage()
        {
            isClickImage = !isClickImage;
            image_load.GetComponent<RectTransform>().sizeDelta = isClickImage ? new Vector2(960f, 800f) : new Vector2(960f, 360f);
            image_load.texture = isClickImage ? texture_maxCamera : texture_minCamera;
            camera_load.targetTexture = texture_maxCamera;
        }
    }
}

