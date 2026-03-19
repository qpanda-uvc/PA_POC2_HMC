using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlightCanvas : MonoBehaviour
{
    FlightManager flightManager;
    DeckManager deckManager;

    public Flight_btn flight_prefab;

    public Flight_btn selectedBtn;
    public int flightsNum;

    public void Initialize()
    {
        flightManager = FindObjectOfType<FlightManager>();
        flightManager.flightCanvas = this;
        deckManager = FindObjectOfType<DeckManager>();
    }

    public void GenerateFlight_btn(FlightInfo flightInfo)
    {
        if(selectedBtn != null)
        {
            Destroy(selectedBtn.gameObject);
        }

        Flight_btn flight_btn = Instantiate(flight_prefab, transform);
        selectedBtn = flight_btn;
        flightManager.selectedFlight = flightInfo;
        flight_btn.ShowThis(flightInfo);
        flight_btn.transform.localPosition = new Vector2(0, 0);
        deckManager.GenerateDeckButtons(flightInfo);
    }

    public void FlightChange(bool next)
    {
        if (next)
        {
            if (flightManager.flightInfos.Count - 1 >= flightsNum + 1) 
            {
                flightsNum++;
                GenerateFlight_btn(flightManager.flightInfos[flightsNum]);
            }
        }
        else
        {
            if (flightsNum - 1 >= 0) 
            {
                flightsNum--;
                GenerateFlight_btn(flightManager.flightInfos[flightsNum]);
            }
        }
    }
}
