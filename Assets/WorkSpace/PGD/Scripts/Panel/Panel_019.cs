using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Newtonsoft.Json;

namespace PGD
{
    public class Panel_019 : MonoBehaviour
    {
        [SerializeField] NetworkManager networkManager;
        public Alert alert = new Alert();
        List<GameObject> alert_List = new List<GameObject>();

        [SerializeField] private TMP_Dropdown dropdown_year;
        [SerializeField] private TMP_Dropdown dropdown_month;

        private int selectedYear;
        private int selectedMonth;

        [SerializeField] GameObject prefab_alert;
        [SerializeField] Transform alertGroup;

        private bool isArrowClick;

        void OnEnable()
        {
            SetDropdown();
            UIManager.Instance.SetDefaultNoticeColor();
        }

        private void OnDisable()
        {
            dropdown_year.onValueChanged.RemoveAllListeners();
            dropdown_month.onValueChanged.RemoveAllListeners();
        }

        private void SetDropdown()
        {
            dropdown_year.ClearOptions();
            dropdown_month.ClearOptions();

            int currentYear = int.Parse(DateTime.Now.ToString("yyyy"));
            int currentMonth = int.Parse(DateTime.Now.ToString("MM"));


            List<string> yearList = new List<string>();
            for (int year = 2021; year <= currentYear; year++)
            {
                yearList.Add(year.ToString());
            }
            dropdown_year.AddOptions(yearList);

            List<string> monthList = new List<string>();
            for (int month = 1; month <= 12; month++)
            {
                monthList.Add(month.ToString());
            }
            dropdown_month.AddOptions(monthList);

            dropdown_year.value = dropdown_year.options.Count;
            dropdown_month.value = currentMonth - 1;

            selectedYear = currentYear;
            selectedMonth = currentMonth;

            dropdown_year.onValueChanged.AddListener(delegate { OnYearDropdownValueChanged(); });
            dropdown_month.onValueChanged.AddListener(delegate { OnMonthDropdownValueChanged(); });

            SetAlertDate();
        }

        private void OnYearDropdownValueChanged()
        {
            selectedYear = int.Parse(dropdown_year.options[dropdown_year.value].text);
            if (!isArrowClick)
                SetAlertDate();

            isArrowClick = false;
        }

        private void OnMonthDropdownValueChanged()
        {
            selectedMonth = dropdown_month.value + 1;
            SetAlertDate();

            isArrowClick = false;
        }

        public void SetAlertDate()
        {
            int lastday = DateTime.DaysInMonth(selectedYear, selectedMonth);
            string LastDayofMonth = lastday.ToString();
            GetAlertLog(selectedYear.ToString() + selectedMonth.ToString("D2") + "01", selectedYear.ToString() + selectedMonth.ToString("D2") + LastDayofMonth);
        }

        public void GetAlertLog(string createAtFrom, string createAtTo)
        {
            if (networkManager == null)
                return;

            networkManager.GetClass($"alarm?createdAtTo={createAtTo}&createdAtFrom={createAtFrom}", (result, text) =>
            {
                if (result)
                {
                    alert = JsonConvert.DeserializeObject<Alert>(text);
                    CreateAlertLog(alert);
                }
                else
                {
                    Debug.Log("알람 데이터를 정상적으로 받아오지 못하였습니다. 다시 시도해주세요");
                }
            });
        }

        public void CreateAlertLog(Alert alert)
        {
            foreach (var a in alert_List)
            {
                Destroy(a);
            }

            foreach (var a in alert.data)
            {
                GameObject log = Instantiate(prefab_alert, alertGroup);
                log.GetComponent<Cell_019>().SetData(a);
                alert_List.Add(log);
            }
        }

        public void OnClickArrowBtn(int num)
        {
            int value = num + dropdown_month.value;

            if (value < 0 && dropdown_year.value != 0)
            {
                selectedYear = dropdown_year.value - 1;
                selectedMonth = dropdown_month.options.Count - 1;
            }
            else if (value  > dropdown_month.options.Count - 1 && dropdown_year.value != dropdown_year.options.Count - 1)
            {
                selectedYear = dropdown_year.value + 1;
                selectedMonth = 0;
            }
            else
            {
                selectedMonth = dropdown_month.value + num;
            }

            isArrowClick = true;

            dropdown_year.value = selectedYear;
            dropdown_month.value = selectedMonth;          
        }
    }
}
