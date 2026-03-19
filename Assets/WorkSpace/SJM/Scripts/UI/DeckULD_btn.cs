using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckULD_btn : MonoBehaviour
{
    FlightManager flightManager;
    DeckManager deckULDManager;
    VirtualULDWork virtualULDWork;
    FlightStatus flightStatus;
    SCCIcons sccIcons;

    public bool isMain;
    public string myULDIndex;

    // 버튼 내 UI_IDX
    public TMP_Text posIndex_txt;
    public TMP_Text id_txt;
    public TMP_Text IDX_weight_txt;
    public TMP_Text IDX_volume_txt;
    public Image scc_Small;

    // 버튼 내 UI_ULD
    public TMP_Text ULD_weight_txt;
    public TMP_Text ULD_volume_txt;
    public Image[] scc;

    public DeckULD_btn()
    {

    }

    public DeckULD_btn(bool isMain, string myULDIndex)
    {
        this.isMain = isMain;
        this.myULDIndex = myULDIndex;
    }


    public void Initialize()
    {
        flightManager = FindObjectOfType<FlightManager>();
        deckULDManager = FindObjectOfType<DeckManager>();
        virtualULDWork = FindObjectOfType<VirtualULDWork>();
        flightStatus = FindObjectOfType<FlightStatus>();
        sccIcons = FindObjectOfType<SCCIcons>();

        GetComponent<Button>().onClick.AddListener(ClickDeckULD_btn);
    }

    public void ClickDeckULD_btn()
    {
        deckULDManager.clickedDeckULD_btn = this;

        // 버튼에 맞는 새 uld로 변경
        virtualULDWork.GenerateNewULD(flightManager.selectedFlight, myULDIndex);

        ShowData();
    }

    public void ShowData()
    {
        ShowDataThis();
        ShowDataDisplay();
    }

    public void ShowDataThis()
    {
        ULDInfo tmpULDInfo = new ULDInfo();
        tmpULDInfo = flightManager.selectedFlight.uldInfos[myULDIndex];

        posIndex_txt.text = tmpULDInfo.posIndex.ToString();
        id_txt.text = tmpULDInfo.id;
        IDX_weight_txt.text = tmpULDInfo.weight.ToString() + "Kg";
        IDX_volume_txt.text = tmpULDInfo.wVolume.ToString() + "%";
        ULD_weight_txt.text = tmpULDInfo.weight.ToString() + "Kg";
        ULD_volume_txt.text = tmpULDInfo.wVolume.ToString() + "%";

        if (tmpULDInfo.scc.Count > 0)
        {
            if (sccIcons.SCCMap.ContainsKey(tmpULDInfo.scc[0]))
            {
                scc_Small.sprite = sccIcons.SCCMap[tmpULDInfo.scc[0]];
            }
        }

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
    }

    public void ShowDataDisplay()
    {
        if (flightStatus.isOn_StackView)
        {
            flightStatus.stackView_Canvas.GetComponent<StackView_Canvas>().ScreenDisplay(flightManager.selectedFlight, myULDIndex);
        }
        else if (flightStatus.isOn_LoadTable)
        {
            flightStatus.loadTable_Canvas.GetComponent<LoadTable_Canvas>().ScreenDisplay(flightManager.selectedFlight, myULDIndex);
        }
        else if (flightStatus.isOn_TimeTable)
        {
            flightStatus.timeTable_Canvas.GetComponent<TimeTable_Canvas>().ListGenerate(flightManager.selectedFlight, myULDIndex);
        }
    }

    

}
