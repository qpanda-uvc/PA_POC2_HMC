using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TesterClass : MonoBehaviour
{
    public FlightStatus flightStatus;
    public FlightCanvas flightCanvas;
    public VirtualULDWork virtualULDWork;
    public FlightManager flightManager;
    public DeckManager deckULDManager;
    public StackView_Canvas stackView_Canvas;
    public LoadTable_Canvas loadTable_Canvas;
    public TimeTable_Canvas timeTable_Canvas;
    public Day_Canvas day_Canvas;
    public Progress_Canvas progress_Canvas;
    public ActiveCheck activeCheck;
    public SCCIcons sccIcons;

    public TestManager testManager;

    public GameObject FlightDataCanvas;

    void Start()
    {
        if (flightStatus != null) flightStatus.Initialize();
        if (flightCanvas != null) flightCanvas.Initialize();
        if (virtualULDWork != null) virtualULDWork.Initialize();
        if (flightManager != null) flightManager.Initialize();
        if (deckULDManager != null) deckULDManager.Initialize();
        if (stackView_Canvas != null) stackView_Canvas.Initialize();
        if (loadTable_Canvas != null) loadTable_Canvas.Initialize();
        if (timeTable_Canvas != null) timeTable_Canvas.Initialize();
        if (day_Canvas != null) day_Canvas.Initialize();
        if (progress_Canvas != null) progress_Canvas.Initialize();
        if (activeCheck != null) activeCheck.Initialize();
        if (flightStatus != null) flightStatus.LateInitialize();
        if (sccIcons != null) sccIcons.Initialize();

        if (testManager != null) testManager.Initialize();
    }

}
