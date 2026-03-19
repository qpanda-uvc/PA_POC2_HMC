using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

enum Days
{
    year,
    month,
    day,
}

public class Day_Canvas : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown_Year;
    [SerializeField] TMP_Dropdown dropdown_Month;
    [SerializeField] TMP_Dropdown dropdown_Day;

    FlightCanvas flightCanvas;

    int selectedYear;
    int selectedMonth ;
    int selectedDay;
    public string selectedDate;
    TMP_Text selectedDate_txt;

    Button flightUp_btn;
    Button flightDown_btn;

    public void Initialize()
    {
        flightCanvas = GetComponentInChildren<FlightCanvas>();

        Transform dropdownTransform = transform.Find("Dropdown");
        if (dropdownTransform == null) { Debug.LogWarning("[Day_Canvas] 'Dropdown' 오브젝트를 찾을 수 없음, 초기화 건너뜀"); return; }
        GameObject dropdown = dropdownTransform.gameObject;
        dropdown_Year = dropdown.transform.Find("Year").GetComponent<TMP_Dropdown>();
        dropdown_Month = dropdown.transform.Find("Month").GetComponent<TMP_Dropdown>();
        dropdown_Day = dropdown.transform.Find("Day").GetComponent<TMP_Dropdown>();

        GameObject selectedDate = transform.Find("Selected Date").gameObject;
        selectedDate_txt = selectedDate.transform.Find("Selected Date_txt").gameObject.GetComponent<TMP_Text>();

        flightUp_btn = transform.Find("FlightUp_btn").gameObject.GetComponent<Button>();
        flightDown_btn = transform.Find("FlightDown_btn").gameObject.GetComponent<Button>();

        flightUp_btn.onClick.AddListener(() => FlightSelectChange(true));
        flightDown_btn.onClick.AddListener(() => FlightSelectChange(false));

        SetDate();
    }

    private void OnEnable()
    {
        dropdown_Year.ClearOptions();
        dropdown_Month.ClearOptions();
        dropdown_Day.ClearOptions();

        int currentYear = int.Parse(DateTime.Now.ToString("yyyy"));
        int currentMonth = int.Parse(DateTime.Now.ToString("MM"));
        int currnetDay = int.Parse(DateTime.Now.ToString("dd"));

        List<string> yearList = new List<string>();
        for (int year = currentYear - 2; year <= currentYear; year++) 
        {
            yearList.Add(" " + year.ToString());
        }
        dropdown_Year.AddOptions(yearList);

        List<string> monthList = new List<string>();
        for (int month = 1; month <= 12; month++)
        {
            monthList.Add(" " + month.ToString());
        }
        dropdown_Month.AddOptions(monthList);

        selectedYear = currentYear;
        selectedMonth = currentMonth;
        SetDays();

        dropdown_Year.value = dropdown_Year.options.Count;
        dropdown_Month.value = currentMonth - 1;
        dropdown_Day.value = currnetDay - 1;
    }

    public void OnYearDropdownValueChanged()
    {
        selectedYear = int.Parse(dropdown_Year.options[dropdown_Year.value].text);
        SetDays();
        SetDate();
    }
        
    public void OnMonthDropdownValueChanged()
    {
        selectedMonth = dropdown_Month.value + 1;
        SetDays();
        SetDate();
    }

    public void OnDayDropdownValueChanged()
    {
        selectedDay = dropdown_Day.value + 1;
        SetDate();
    }

    void SetDays()
    {
        dropdown_Day.ClearOptions();

        List<string> dayList = new List<string>();
        for (int day = 1; day <= DateTime.DaysInMonth(selectedYear, selectedMonth); day++)
        {
            dayList.Add(" " + day.ToString());
        }
        dropdown_Day.AddOptions(dayList);

        selectedDay = dropdown_Day.value + 1;
    }

    void FlightSelectChange(bool next)
    {
        flightCanvas.FlightChange(next);
    }

    void SetDate()
    {
        selectedDate_txt.text = selectedYear.ToString() + "/" + selectedMonth.ToString() + "/" + selectedDay.ToString();
        selectedDate = selectedDate_txt.text;
    }
}
