using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_010 : MonoBehaviour
    {
        [SerializeField] TMP_Text text_currentTime;
        [SerializeField] TMP_Text text_workingFlight;
        [SerializeField] TMP_Text text_remainingTime;
        [SerializeField] Image slider_flightProgress;
        [SerializeField] Image slider_currentULDPackingFactor;

        private DateTime currentTime;
        private DateTime remainingTime;

        private TimeSpan timeDiff;

        private void Start()
        {
            remainingTime = DateTime.Now.AddHours(2);
        }
        void OnEnable()
        {

        }

        void Update()
        {
            currentTime = DateTime.Now;
            timeDiff = remainingTime - currentTime;

            text_currentTime.text = currentTime.ToString("yyyy.MM.dd HH:mm:ss");

            if (timeDiff.TotalSeconds <= 0)
            {
                text_remainingTime.text = "¿Ï·á";
            }
            else
            {
                text_remainingTime.text = timeDiff.Hours + ":" + timeDiff.Minutes + ":" + timeDiff.Seconds;
            }
        }

        public void UpdateData(string workingFlight, DateTime time)
        {
            DateTime currentTime = DateTime.Now;
         
            text_workingFlight.text = workingFlight;
            text_remainingTime.text = (currentTime - time).ToString();        
        }

        public void UpdateSlider(float flightProgress, float currentULDPackingFactor)
        {
            slider_flightProgress.fillAmount = flightProgress / 100f;
            slider_currentULDPackingFactor.fillAmount = currentULDPackingFactor / 100f;
        }
    }
}

