using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DeckManager : MonoBehaviour
{
    public FlightStatus flightStatus;
    public Progress_Canvas progress_Canvas;
    public DeckULD_btn deckULD_btn_prefab;

    public GameObject mainDeckBoxGroup;
    public GameObject lowerDeckBoxGroup;
    public List<GameObject> mainDeckBoxs = new List<GameObject>();
    public List<GameObject> lowerDeckBoxs = new List<GameObject>();

    public List<DeckULD_btn> mainDeckULD_Btns = new List<DeckULD_btn>();
    public List<DeckULD_btn> lowerDeckULD_Btns = new List<DeckULD_btn>();

    // IDX, ULD, SCC
    public List<GameObject> deckULD_IDX = new List<GameObject>();
    public List<GameObject> deckULD_ULD = new List<GameObject>();
    public List<GameObject> deckULD_SCC = new List<GameObject>();

    public DeckULD_btn clickedDeckULD_btn;
    

    public void Initialize()
    {
        
    }

    public void DestroyPreviousButtons()
    {
        foreach (DeckULD_btn previousBtn in mainDeckULD_Btns)
        {
            Destroy(previousBtn.gameObject);
        }
        foreach (DeckULD_btn previousBtn in lowerDeckULD_Btns)
        {
            Destroy(previousBtn.gameObject);
        }
        mainDeckULD_Btns.Clear();
        lowerDeckULD_Btns.Clear();
        deckULD_IDX.Clear();
        deckULD_ULD.Clear();
        deckULD_SCC.Clear();
    }

    public void GenerateDeckButtons(FlightInfo flightInfo)
    {
        DestroyPreviousButtons();

        // 이 Flight에 맞는 Deck구조 새로 생성 필요 (flightStatus.mainDeckBoxGroup에 생성)

        mainDeckBoxs.Clear();
        lowerDeckBoxs.Clear();

        // 그 후 DeckBoxs에 추가
        foreach (Transform boxs in mainDeckBoxGroup.transform)
        {
            mainDeckBoxs.Add(boxs.gameObject);
        }
        foreach (Transform boxs in lowerDeckBoxGroup.transform)
        {
            lowerDeckBoxs.Add(boxs.gameObject);
        }

        // 이 항공편이 갖고 있는 DeckULD_Btn들 생성
        foreach (ULDInfo indexNameValue in flightInfo.uldInfos.Values)
        {
            GenerateDeckULD_btn(indexNameValue);
        }

        float boxCount = mainDeckBoxs.Count + lowerDeckBoxs.Count;
        float uldCount = mainDeckULD_Btns.Count + lowerDeckULD_Btns.Count;

        if (progress_Canvas == null) 
        {
            progress_Canvas = FindObjectOfType<Progress_Canvas>();
        }
        progress_Canvas.SetProgressBar(uldCount / boxCount);
    }

    public void GenerateDeckULD_btn(ULDInfo uldInfo)
    {
        GameObject deckBox = null;

        if (uldInfo.main)
        {
            for (int i = 0; i < mainDeckBoxGroup.transform.childCount; i++)
            {
                GameObject tmpDeckBox = mainDeckBoxGroup.transform.GetChild(i).gameObject.transform.GetComponentInChildren<DeckButtonBox>().gameObject;
                if (tmpDeckBox.GetComponent<DeckButtonBox>().posIndex == uldInfo.posIndex)
                {
                    deckBox = tmpDeckBox;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < lowerDeckBoxGroup.transform.childCount; i++)
            {
                GameObject tmpDeckBox = lowerDeckBoxGroup.transform.GetChild(i).gameObject.transform.GetComponentInChildren<DeckButtonBox>().gameObject;
                if (tmpDeckBox.GetComponent<DeckButtonBox>().posIndex == uldInfo.posIndex)
                {
                    deckBox = tmpDeckBox;
                    break;
                }
            }
        }
        DeckULD_btn newDeckULD_btn = Instantiate(deckULD_btn_prefab, deckBox.transform);
        newDeckULD_btn.Initialize();
        newDeckULD_btn.myULDIndex = uldInfo.uldIndex;
        if (uldInfo.main)
        {
            mainDeckULD_Btns.Add(newDeckULD_btn);
            newDeckULD_btn.isMain = true;
        }
        else
        {
            lowerDeckULD_Btns.Add(newDeckULD_btn);
            newDeckULD_btn.isMain = false;
        }

        newDeckULD_btn.ShowDataThis();
        newDeckULD_btn.transform.localPosition = new Vector2(0, 0);
        deckULD_IDX.Add(newDeckULD_btn.gameObject.transform.Find("IDX_Image").gameObject);
        deckULD_ULD.Add(newDeckULD_btn.gameObject.transform.Find("ULD_Image").gameObject);
        deckULD_SCC.Add(newDeckULD_btn.gameObject.transform.Find("SCC_Image").gameObject);
        flightStatus.DeckButtonChange(flightStatus.deckButtonState); // 현재 IDX, ULD, SCC 버튼 상태에 따라 이미지 변경
    }

}
