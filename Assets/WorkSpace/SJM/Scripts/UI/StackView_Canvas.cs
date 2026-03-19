using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StackView_Canvas : MonoBehaviour
{
    VirtualULDWork virtualULDWork;
    public SCCIcons sccIcons;

    public TMP_Text uld_id;
    public TMP_Text wVolume;
    public TMP_Text sVolume;
    public Image wVolumeImage;
    public TMP_Text workTimeM;
    public TMP_Text workTimeS;
    public Image[] scc;

    public VirtualULD toShowULD;
    public Camera virtualULDCamera;

    public RawImage stackViewScreen;
    RenderTexture renderTexture; // 카메라 뷰를 렌더링할 텍스처

    Toggle stackViewPlay_btn;
    Toggle stackViewX2_btn;
    Button stackViewReset_btn;

    GameObject playImage;
    GameObject pauseImage;
    GameObject x1_Image;
    GameObject x2_Image;

    public void Initialize()
    {
        sccIcons = FindObjectOfType<SCCIcons>();

        GameObject stackViewDisplay = transform.Find("Stack View Display").gameObject;
        stackViewPlay_btn = stackViewDisplay.transform.Find("Stack View Play_btn").gameObject.GetComponent<Toggle>();
        stackViewX2_btn = stackViewDisplay.transform.Find("Stack View X2_btn").gameObject.GetComponent<Toggle>();
        stackViewReset_btn = stackViewDisplay.transform.Find("Stack View Reset_btn").gameObject.GetComponent<Button>();
        stackViewPlay_btn.onValueChanged.AddListener(ViewPlay);
        stackViewX2_btn.onValueChanged.AddListener(View_X2_btn);
        stackViewReset_btn.onClick.AddListener(ViewReset);

        playImage = stackViewPlay_btn.gameObject.transform.Find("PlayImage").gameObject;
        pauseImage = stackViewPlay_btn.gameObject.transform.Find("PauseImage").gameObject;
        x1_Image = stackViewX2_btn.gameObject.transform.Find("X1").gameObject;
        x2_Image = stackViewX2_btn.gameObject.transform.Find("X2").gameObject;

        playImage.SetActive(true);
        pauseImage.SetActive(false);
        x2_Image.SetActive(false);
        x1_Image.SetActive(true);  
    }

    private void OnEnable()
    {
        virtualULDWork = FindObjectOfType<VirtualULDWork>();
        virtualULDCamera = virtualULDWork.stackViewULDCamera;
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        virtualULDCamera.targetTexture = renderTexture;

        RenderCameraViewToUI();
    }

    private void RenderCameraViewToUI()
    {
        virtualULDCamera.Render();
        stackViewScreen.texture = renderTexture;
    }


    public void ScreenDisplay(FlightInfo flight, string myIndexName)
    {
        ULDInfo tmpULDInfo = new ULDInfo();
        tmpULDInfo = flight.uldInfos[myIndexName];

        uld_id.text = tmpULDInfo.id;
        wVolume.text = tmpULDInfo.wVolume.ToString() + "%";
        wVolumeImage.fillAmount = tmpULDInfo.wVolume / 100;
        sVolume.text = "(" + tmpULDInfo.sVolume.ToString() + "%" + ")";
        workTimeM.text = Mathf.FloorToInt(tmpULDInfo.workTime / 60).ToString() + "m";
        workTimeS.text = Mathf.FloorToInt(tmpULDInfo.workTime % 60f).ToString() + "s";
        for (int i = 0; i < tmpULDInfo.scc.Count; i++)
        {
            if (sccIcons.SCCMap.ContainsKey(tmpULDInfo.scc[i]))
            {
                if (i >= 3)
                {
                    break;
                }
                scc[i].sprite = sccIcons.SCCMap[tmpULDInfo.scc[i]];
            }
        }

        if (stackViewX2_btn.GetComponent<Toggle>().isOn)
        {
            toShowULD.FallingSpeedUp(true);
        }
    }

    void ViewPlay(bool isPlay)
    {
        if(toShowULD != null)
        {
            if (isPlay)
            {
                toShowULD.StackStart();
                toShowULD.isPause = false;
                playImage.SetActive(false);
                pauseImage.SetActive(true);
            }
            else
            {
                toShowULD.isPause = true;
                playImage.SetActive(true);
                pauseImage.SetActive(false);
            }
        }
    }

    void View_X2_btn(bool isOn)
    {
        if (toShowULD != null)
        {
            toShowULD.FallingSpeedUp(isOn);

            if (isOn)
            {
                x1_Image.SetActive(false);
                x2_Image.SetActive(true);
            }
            else
            {
                x1_Image.SetActive(true);
                x2_Image.SetActive(false);
            }
        } 
    }

    void ViewReset()
    {
        if (toShowULD != null)
        {
            toShowULD.ULDReset();
            ViewPlay(false);
            stackViewPlay_btn.isOn = false;
        }
    }
}
