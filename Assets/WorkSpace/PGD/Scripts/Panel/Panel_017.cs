using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace PGD
{
    public class Panel_017 : MonoBehaviour
    {
        // First row
        [SerializeField] private TMP_Dropdown dropdown_fromYear;
        [SerializeField] private TMP_Dropdown dropdown_fromMonth;
        [SerializeField] private TMP_Dropdown dropdown_fromDay;

        [SerializeField] private TMP_Dropdown dropdown_toYear;
        [SerializeField] private TMP_Dropdown dropdown_toMonth;
        [SerializeField] private TMP_Dropdown dropdown_toDay;

        private int fromSelectedYear = 2000;
        private int fromSelectedMonth = 1;
        private int fromSelectedDay = 1;

        private int toSelectedYear = 2000;
        private int toSelectedMonth = 1;
        private int toSelectedDay = 1;

        [SerializeField] private TMP_Text text_fromDate;
        [SerializeField] private TMP_Text text_toDate;
        [SerializeField] private TMP_Text text_dayDiff;

        private int timeDiff;

        // Second row
        [SerializeField] private Image[] slider_awbKpi;
        [SerializeField] private Image[] slider_pouTop3;
        [SerializeField] private Image[] slider_sccTop3;

        // Thrid row
        [SerializeField] private TMP_Text text_averageLoadingRate;
        [SerializeField] private TMP_Text text_averageWorkingHours;
        [SerializeField] private TMP_Text text_recommendation;
        [SerializeField] private TMP_Text text_uld;

        // Fourth row
        [SerializeField] private Image slider_automationFacilitiesKpi;
        [SerializeField] private TMP_Text text_automationFacilitieskpi;
        [SerializeField] private Image slider_vms;
        [SerializeField] private TMP_Text text_vms;

        [SerializeField] private Image slider_amr;
        [SerializeField] private TMP_Text text_amr;
        [SerializeField] private Image slider_asrs;
        [SerializeField] private TMP_Text text_asrs;

        void OnEnable()
        {
            SetDropdown();
        }


        private void SetDropdown()
        {
            dropdown_fromYear.ClearOptions();
            dropdown_fromMonth.ClearOptions();
            dropdown_fromDay.ClearOptions();
            dropdown_toYear.ClearOptions();
            dropdown_toMonth.ClearOptions();
            dropdown_toDay.ClearOptions();


            int currentYear = int.Parse(DateTime.Now.ToString("yyyy"));
            int currentMonth = int.Parse(DateTime.Now.ToString("MM"));
            int currnetDay = int.Parse(DateTime.Now.ToString("dd"));

            List<string> yearList = new List<string>();
            for (int year = (currentYear - 2); year <= currentYear; year++)
            {
                yearList.Add(year.ToString());
            }
            dropdown_fromYear.AddOptions(yearList);
            dropdown_toYear.AddOptions(yearList);

            List<string> monthList = new List<string>();
            for (int month = 1; month <= 12; month++)
            {
                monthList.Add(month.ToString());
            }   
            dropdown_fromMonth.AddOptions(monthList);
            dropdown_toMonth.AddOptions(monthList);

            int lastday = DateTime.DaysInMonth(currentYear, currentMonth);    
            
            dropdown_fromYear.value = dropdown_fromYear.options.Count;
            dropdown_fromMonth.value = currentMonth - 1;
            dropdown_fromDay.value = currnetDay - 1;

            dropdown_toYear.value = dropdown_toYear.options.Count;
            dropdown_toMonth.value = currentMonth - 1;
            dropdown_toDay.value = currnetDay - 1;

            fromSelectedYear = currentYear;
            fromSelectedMonth = currentMonth;
            fromSelectedDay = currnetDay;

            toSelectedYear = currentYear;
            toSelectedMonth = currentMonth;
            toSelectedDay = currnetDay;

            SetDropDownDay();

            UpdateFromSelectedDate();   
            UpdateToSelectedDate();
            
        }

        private void SetDropDownDay()
        {
            dropdown_fromDay.ClearOptions();
            dropdown_toDay.ClearOptions();

            int fromLastday = DateTime.DaysInMonth(fromSelectedYear, fromSelectedMonth);
            int toLastday = DateTime.DaysInMonth(toSelectedYear, toSelectedMonth);

            List<string> fromDayList = new List<string>();
            List<string> toDayList = new List<string>();

            for (int day = 1; day <= fromLastday; day++)
            {
                fromDayList.Add(day.ToString());
            }

            for (int day = 1; day <= toLastday; day++)
            {
                toDayList.Add(day.ToString());
            }

            dropdown_fromDay.AddOptions(fromDayList);
            dropdown_toDay.AddOptions(toDayList);
        }

        public void OnFromYearDropdownValueChanged()
        {
            fromSelectedYear = int.Parse(dropdown_fromYear.options[dropdown_fromYear.value].text);
            SetDropDownDay();
        }

        public void OnFromMonthDropdownValueChanged()
        {
            fromSelectedMonth = dropdown_fromMonth.value + 1;
            SetDropDownDay();
        } 

        public void OnFromDayDropdownValueChanged() => fromSelectedDay = dropdown_fromDay.value + 1;

        public void OnToYearDropdownValueChanged()
        {
            toSelectedYear = int.Parse(dropdown_toYear.options[dropdown_toYear.value].text);
            SetDropDownDay();
        }

        public void OnToMonthDropdownValueChanged()
        {
            toSelectedMonth = dropdown_toMonth.value + 1;
            SetDropDownDay();
        } 

        public void OnToDayDropdownValueChanged() => toSelectedDay = dropdown_toDay.value + 1;

        public void UpdateFromSelectedDate()
        {
            text_fromDate.text = fromSelectedYear.ToString() + "/" + fromSelectedMonth.ToString() + "/" + fromSelectedDay.ToString();
            text_dayDiff.text = CompareDay().ToString();
        }
            
        public void UpdateToSelectedDate()
        {
            text_toDate.text = toSelectedYear.ToString() + "/" + toSelectedMonth.ToString() + "/" + toSelectedDay.ToString();
            text_dayDiff.text = CompareDay().ToString();
        }

        private int CompareDay()
        {
            DateTime fromDateTime = new DateTime(fromSelectedYear, fromSelectedMonth, fromSelectedDay);
            DateTime toDateTime = new DateTime(toSelectedYear, toSelectedMonth, toSelectedDay);

            timeDiff = (toDateTime - fromDateTime).Days;
            return timeDiff;
        }

        public void OnClickConfirmBtn()
        {
            if (timeDiff < 0)
                return;          
        }

        private void UpdateSecondRowData(float[] awbKpiArray, float[] pouTop3Array, float[] sccTop3Array)
        {
            for (int i = 0; i < slider_awbKpi.Length; i++)
            {
                slider_awbKpi[i].fillAmount = awbKpiArray[i] / 100f;
            }
            for (int i = 0; i < slider_pouTop3.Length; i++)
            {
                slider_pouTop3[i].fillAmount = pouTop3Array[i] / 100f;
                slider_sccTop3[i].fillAmount = sccTop3Array[i] / 100f;
            }
        }

        private void UpdateThirdRowData(string averageLoadingRate, string averageWorkingHours, string recommendation, string uld)
        {

            text_averageLoadingRate.text = averageLoadingRate;
            text_averageWorkingHours.text = averageWorkingHours;
            text_recommendation.text = recommendation;
            text_uld.text = uld;
        }

        private void UpdateFourthRowData(float automationFacilitiesKpi, float vms, float amr, float asrs)
        {
            slider_automationFacilitiesKpi.fillAmount = automationFacilitiesKpi / 100f;
            text_automationFacilitieskpi.text = automationFacilitiesKpi.ToString() + "%";
            slider_vms.fillAmount = vms / 100f;
            text_vms.text = vms.ToString() + "%";
            slider_amr.fillAmount = amr / 100f;
            text_amr.text = amr.ToString() + "%";
            slider_asrs.fillAmount = asrs / 100f;
            text_asrs.text = asrs.ToString() + "%";
        }

        public void OnClickBG()
        {
            gameObject.SetActive(false);
        }
    }
}

    