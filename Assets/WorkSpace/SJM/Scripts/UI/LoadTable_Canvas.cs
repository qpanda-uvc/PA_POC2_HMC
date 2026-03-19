using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadTable_Canvas : MonoBehaviour
{
    VirtualULDWork virtualULDWork;
    SCCIcons sccIcons;

    public TMP_Text load_id;
    public TMP_Text id;
    public TMP_Text volume;
    public TMP_Text weight;
    public TMP_Text destination;
    public Image[] scc;
    public Image stageFill;
    public TMP_Text workStage;
    public float totalStage = 10;

    public VirtualULD toShowULD;
    public Camera virtualULDCamera;
    public RawImage stackViewScreen;
    RenderTexture renderTexture; // 카메라 뷰를 렌더링할 텍스처

    public void Initialize()
    {
        sccIcons = FindObjectOfType<SCCIcons>();
    }

    private void OnEnable()
    {
        virtualULDWork = FindObjectOfType<VirtualULDWork>();
        virtualULDCamera = virtualULDWork.loadTableULDCamera;
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        virtualULDCamera.targetTexture = renderTexture;

        RenderCameraViewToUI();

        if (toShowULD != null)
        {
            toShowULD.ULDLoadTableState();
        }
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

        load_id.text = tmpULDInfo.id;
        id.text = tmpULDInfo.id;
        volume.text = tmpULDInfo.volume.ToString() + "L";
        weight.text = tmpULDInfo.weight.ToString() + "Kg";
        destination.text = tmpULDInfo.destination;
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
        stageFill.fillAmount = tmpULDInfo.workStage / totalStage;
        workStage.text = "STEP" + tmpULDInfo.workStage.ToString();

        if (toShowULD != null)
        {
            toShowULD.ULDLoadTableState();
        }
    }
}
