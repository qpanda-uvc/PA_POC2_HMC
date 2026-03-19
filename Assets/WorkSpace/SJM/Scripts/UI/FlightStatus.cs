using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum DeckButtonType
{
    IDX,
    ULD,
    SCC
}

public class FlightStatus : MonoBehaviour
{
    DeckManager deckManager;
    public VirtualULDWork virtualULDWork;

    public Button stackView_btn;
    public Button loadTable_btn;
    public Button timeTable_btn;
    public GameObject stackView_Canvas;
    public GameObject loadTable_Canvas;
    public GameObject timeTable_Canvas;

    // Deck
    Button mainDeck_btn;
    Button lowerDeck_btn;
    Button IDX_btn;
    Button ULD_btn;
    Button SCC_btn;
    public GameObject mainDeck_Canvas;
    public GameObject lowerDeck_Canvas;

    public  TMP_Text stackView_txt;
    public TMP_Text loadTable_txt;
    public TMP_Text timeTable_txt;
    public TMP_Text mainDeck_txt;
    public TMP_Text lowerDeck_txt;

    public bool isOn_StackView;
    public bool isOn_LoadTable;
    public bool isOn_TimeTable;
    public bool isOn_MainDeck;
    public bool isOn_LowerDeck;


    Color colorWhite;
    Color colorBlack;
    Color colorGray;
    Color colorSky;

    public DeckButtonType deckButtonState;

    public void Initialize()
    {
        deckManager = FindObjectOfType<DeckManager>();
        deckManager.flightStatus = this;

        stackView_btn = transform.Find("Stack View_btn").gameObject.GetComponent<Button>();
        loadTable_btn = transform.Find("Load Table_btn").gameObject.GetComponent<Button>();
        timeTable_btn = transform.Find("Time Table_btn").gameObject.GetComponent<Button>();
        stackView_btn.onClick.AddListener(StackView_btn);
        loadTable_btn.onClick.AddListener(LoadTable_btn);
        timeTable_btn.onClick.AddListener(TimeTable_btn);
        stackView_Canvas = transform.Find("Stack View_Canvas").gameObject;
        loadTable_Canvas = transform.Find("Load Table_Canvas").gameObject;
        timeTable_Canvas = transform.Find("Time Table_Canvas").gameObject;

        mainDeck_btn = transform.Find("Main Deck_btn").gameObject.GetComponent<Button>();
        lowerDeck_btn = transform.Find("Lower Deck_btn").gameObject.GetComponent<Button>();
        mainDeck_btn.onClick.AddListener(Click_MainDeck_btn);
        lowerDeck_btn.onClick.AddListener(Click_LowerDeck_btn);
        mainDeck_Canvas = transform.Find("Main Deck_Canvas").gameObject;
        lowerDeck_Canvas = transform.Find("Lower Deck_Canvas").gameObject;

        deckManager.mainDeckBoxGroup = mainDeck_Canvas.transform.Find("DeckBoxGroup").gameObject;
        deckManager.lowerDeckBoxGroup = lowerDeck_Canvas.transform.Find("DeckBoxGroup").gameObject;

        IDX_btn = transform.Find("IDX_btn").gameObject.GetComponent<Button>();
        ULD_btn = transform.Find("ULD_btn").gameObject.GetComponent<Button>();
        SCC_btn = transform.Find("SCC_btn").gameObject.GetComponent<Button>();
        IDX_btn.onClick.AddListener(Click_IDX_btn);
        ULD_btn.onClick.AddListener(Click_ULD_btn);
        SCC_btn.onClick.AddListener(Click_SCC_btn);

        stackView_txt = stackView_btn.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        loadTable_txt = loadTable_btn.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        timeTable_txt = timeTable_btn.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        mainDeck_txt = mainDeck_btn.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        lowerDeck_txt = lowerDeck_btn.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();

        colorWhite = Color.white;
        colorBlack = Color.black;
        colorGray = new Color(0xC9 / 255.0f, 0xC9 / 255.0f, 0xC9 / 255.0f, 1.0f);
        colorSky = new Color(0x00 / 255.0f, 0xAA / 255.0f, 0xD2 / 255.0f, 1.0f);
    }

    public void LateInitialize()
    {
        Click_MainDeck_btn(); 
        StackView_btn();
        Click_IDX_btn();
    }

    void StackView_btn()
    {
        stackView_Canvas.SetActive(true);
        loadTable_Canvas.SetActive(false);
        timeTable_Canvas.SetActive(false);
        isOn_StackView = true;
        isOn_LoadTable = false;
        isOn_TimeTable = false;
        stackView_btn.gameObject.GetComponent<Image>().color = colorWhite;
        loadTable_btn.gameObject.GetComponent<Image>().color = colorGray;
        timeTable_btn.gameObject.GetComponent<Image>().color = colorGray;
        stackView_txt.color = colorBlack;
        loadTable_txt.color = colorWhite;
        timeTable_txt.color = colorWhite;

        if(deckManager.clickedDeckULD_btn != null)
        {
            deckManager.clickedDeckULD_btn.ShowDataDisplay();
        }
    }
    void LoadTable_btn()
    {
        stackView_Canvas.SetActive(false);
        loadTable_Canvas.SetActive(true);
        timeTable_Canvas.SetActive(false);
        isOn_StackView = false;
        isOn_LoadTable = true;
        isOn_TimeTable = false;
        stackView_btn.gameObject.GetComponent<Image>().color = colorGray;
        loadTable_btn.gameObject.GetComponent<Image>().color = colorWhite;
        timeTable_btn.gameObject.GetComponent<Image>().color = colorGray;
        stackView_txt.color = colorWhite;
        loadTable_txt.color = colorBlack;
        timeTable_txt.color = colorWhite;

        if (deckManager.clickedDeckULD_btn != null)
        {
            deckManager.clickedDeckULD_btn.ShowDataDisplay();
        }
    }
    void TimeTable_btn()
    {
        stackView_Canvas.SetActive(false);
        loadTable_Canvas.SetActive(false);
        timeTable_Canvas.SetActive(true);
        isOn_StackView = false;
        isOn_LoadTable = false;
        isOn_TimeTable = true;
        stackView_btn.gameObject.GetComponent<Image>().color = colorGray;
        loadTable_btn.gameObject.GetComponent<Image>().color = colorGray;
        timeTable_btn.gameObject.GetComponent<Image>().color = colorWhite;
        stackView_txt.color = colorWhite;
        loadTable_txt.color = colorWhite;
        timeTable_txt.color = colorBlack;

        if (deckManager.clickedDeckULD_btn != null) 
        {
            deckManager.clickedDeckULD_btn.ShowDataDisplay();
        }
    }

    #region
    void Click_MainDeck_btn()
    {
        mainDeck_Canvas.SetActive(true);
        lowerDeck_Canvas.SetActive(false);
        mainDeck_btn.gameObject.GetComponent<Image>().color = colorWhite;
        lowerDeck_btn.gameObject.GetComponent<Image>().color = colorGray;
        mainDeck_txt.color = colorBlack;
        lowerDeck_txt.color = colorWhite;
        isOn_MainDeck = true;
        isOn_LowerDeck = false;

        if (deckManager.clickedDeckULD_btn != null)
        {
            deckManager.clickedDeckULD_btn.ShowDataThis();
        }
    }
    void Click_LowerDeck_btn()
    {
        mainDeck_Canvas.SetActive(false);
        lowerDeck_Canvas.SetActive(true);
        mainDeck_btn.gameObject.GetComponent<Image>().color = colorGray;
        lowerDeck_btn.gameObject.GetComponent<Image>().color = colorWhite;
        mainDeck_txt.color = colorWhite;
        lowerDeck_txt.color = colorBlack;
        isOn_MainDeck = false;
        isOn_LowerDeck = true;

        if (deckManager.clickedDeckULD_btn != null)
        {
            deckManager.clickedDeckULD_btn.ShowDataThis();
        }
    }

    void Click_IDX_btn()
    {
        deckButtonState = DeckButtonType.IDX;
        DeckButtonChange(deckButtonState);
        IDX_btn.gameObject.GetComponent<Image>().color = colorSky;
        ULD_btn.gameObject.GetComponent<Image>().color = colorGray;
        SCC_btn.gameObject.GetComponent<Image>().color = colorGray;
    }

    void Click_ULD_btn()
    {
        deckButtonState = DeckButtonType.ULD;
        DeckButtonChange(deckButtonState);
        IDX_btn.gameObject.GetComponent<Image>().color = colorGray;
        ULD_btn.gameObject.GetComponent<Image>().color = colorSky;
        SCC_btn.gameObject.GetComponent<Image>().color = colorGray;
    }

    void Click_SCC_btn()
    {
        deckButtonState = DeckButtonType.SCC;
        DeckButtonChange(deckButtonState);
        IDX_btn.gameObject.GetComponent<Image>().color = colorGray;
        ULD_btn.gameObject.GetComponent<Image>().color = colorGray;
        SCC_btn.gameObject.GetComponent<Image>().color = colorSky;
    }

    public void DeckButtonChange(DeckButtonType duttonState)
    {
        switch (duttonState)
        {
            case DeckButtonType.IDX:
                for (int i = 0; i < deckManager.deckULD_IDX.Count; i++)
                {
                    deckManager.deckULD_IDX[i].SetActive(true);
                    deckManager.deckULD_ULD[i].SetActive(false);
                    deckManager.deckULD_SCC[i].SetActive(false);
                }
                break;

            case DeckButtonType.ULD:
                for (int i = 0; i < deckManager.deckULD_IDX.Count; i++)
                {
                    deckManager.deckULD_IDX[i].SetActive(false);
                    deckManager.deckULD_ULD[i].SetActive(true);
                    deckManager.deckULD_SCC[i].SetActive(false);
                }
                break;

            case DeckButtonType.SCC:
                for (int i = 0; i < deckManager.deckULD_IDX.Count; i++)
                {
                    deckManager.deckULD_IDX[i].SetActive(false);
                    deckManager.deckULD_ULD[i].SetActive(false);
                    deckManager.deckULD_SCC[i].SetActive(true);
                }
                break;
        }
    }

    #endregion

    public void OnClickCloseBtn()
    {
        gameObject.SetActive(false);
    }
}
