using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class TimeTable_Canvas : MonoBehaviour
{
    ScrollRect scrollRect;

    public GameObject scrollContent;
    public GameObject contentList_prefab;
    float originContentListLength;
    float originContentListPos_X;

    public GameObject bar_Connected;
    public GameObject bar_Simulation;
    List<GameObject> contentList = new List<GameObject>();
    List<GameObject> workTimeBars = new List<GameObject>();
    public GameObject workTimeBar;
    float workBarScale;
    float resizeWorkBarPos_X;

    public float workBarSize_origin;
    public float workBarSize_resize;
    float workBarHeight = 68;
    float barInterval = 9f;
    float barHeight = 12f;

    Color buttonGray;


    public void Initialize()
    {
        scrollRect = GetComponentInChildren<ScrollRect>();

        originContentListLength = contentList_prefab.gameObject.GetComponent<RectTransform>().sizeDelta.x;
        originContentListPos_X = contentList_prefab.gameObject.GetComponent<RectTransform>().anchoredPosition.x;
        workBarSize_origin = contentList_prefab.gameObject.transform.GetChild(1).gameObject.GetComponent<RectTransform>().sizeDelta.x;

        buttonGray = new Color(0xEF / 255.0f, 0xEF / 255.0f, 0xEF / 255.0f, 1.0f);
    }

    private void Update()
    {
        WorkBarResize();
    }

    public void ListGenerate(FlightInfo flight, string indexName)
    {
        StopAllCoroutines();
        foreach (Transform child in scrollContent.transform)
        {
            Destroy(child.gameObject);
        }
        contentList.Clear();
        workTimeBars.Clear();
        workBarScale = 1.0f;

        ULDInfo tmpULDInfo = new ULDInfo();
        tmpULDInfo = flight.uldInfos[indexName];

        List<string> listWorkObjects = new List<string>();
        foreach (string listNames in tmpULDInfo.timeTableList_Name)
        {
            listWorkObjects.Add(listNames);
        }

        // workTimeBar 최대 크기 설정
        float maxEndPos = 0;
        float maxConEndPos = 0;
        float maxSimEndPos = 0;
        foreach (List<float> conEndValues in tmpULDInfo.conEndTime.Values)
        {
            foreach (float conEndTimes in conEndValues)
            {
                if (conEndTimes > maxConEndPos)
                {
                    maxConEndPos = conEndTimes;
                }
            }
        }
        foreach (List<float> simEndValues in tmpULDInfo.simEndTime.Values)
        {
            foreach (float simEndTimes in simEndValues)
            {
                if (simEndTimes > maxSimEndPos)
                {
                    maxSimEndPos = simEndTimes;
                }
            }
        }
        if (maxConEndPos >= maxSimEndPos)
        {
            maxEndPos = maxConEndPos;
        }
        else
        {
            maxEndPos = maxSimEndPos;
        }
        float increaseBarSize = maxEndPos - workBarSize_origin; // 늘어나야할 길이
        workBarSize_resize = workBarSize_origin + increaseBarSize; // 조정된 WorkBar크기


        for (int i = 0; i < tmpULDInfo.timeTableList_Name.Count; i++)
        {
            // 리스트 목록 생성
            contentList.Add(Instantiate(contentList_prefab, scrollContent.transform));
            GameObject listName = contentList[i].transform.GetChild(0).gameObject;
            listName.transform.GetChild(0).GetComponent<TMP_Text>().text = "  " + (i + 1).ToString() + ". " + tmpULDInfo.timeTableList_Name[i];

            contentList[i].GetComponent<RectTransform>().sizeDelta = new Vector2(originContentListLength + increaseBarSize, workBarHeight);
            contentList[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(originContentListPos_X + (increaseBarSize / 2), 0);

            workTimeBar = contentList[i].transform.GetChild(1).gameObject;
            workTimeBars.Add(workTimeBar);
            float originWorkBarPos_X = workTimeBar.GetComponent<RectTransform>().anchoredPosition.x;
            resizeWorkBarPos_X = originWorkBarPos_X + (increaseBarSize / 2);
            workTimeBar.GetComponent<RectTransform>().sizeDelta = new Vector2(workBarSize_resize, workBarHeight);
            workTimeBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(resizeWorkBarPos_X, 0);

            if (i % 2 == 1)
            {
                contentList[i].GetComponent<Image>().color = buttonGray;
            }


            // 이전 작업 bar 보여주기
            for (int j = 0; j < tmpULDInfo.conEndTime[listWorkObjects[i]].Count; j++)
            {
                float barStartPos = tmpULDInfo.conStartTime[listWorkObjects[i]][j];
                float workTime = tmpULDInfo.conEndTime[listWorkObjects[i]][j] - tmpULDInfo.conStartTime[listWorkObjects[i]][j];
                float barEndPos = -(workBarSize_resize - barStartPos) + (workTime);

                GameObject newBar = Instantiate(bar_Connected, workTimeBar.transform);
                newBar.GetComponent<RectTransform>().offsetMin = new Vector2(barStartPos, barInterval);
                newBar.GetComponent<RectTransform>().offsetMax = new Vector2(barEndPos, barInterval + barHeight);

            }
            for (int j = 0; j < tmpULDInfo.simEndTime[listWorkObjects[i]].Count; j++)
            {
                float barStartPos = tmpULDInfo.simStartTime[listWorkObjects[i]][j];
                float workTime = tmpULDInfo.simEndTime[listWorkObjects[i]][j] - tmpULDInfo.simStartTime[listWorkObjects[i]][j];
                float barEndPos = -(workBarSize_resize - barStartPos) + (workTime);

                GameObject newBar = Instantiate(bar_Simulation, workTimeBar.transform);
                newBar.GetComponent<RectTransform>().offsetMin = new Vector2(barStartPos, -(barInterval + barHeight));
                newBar.GetComponent<RectTransform>().offsetMax = new Vector2(barEndPos, -barInterval);
            }
        }
    }

    // ctrl Z로 좌우 확대 축소
    public void WorkBarResize()
    {
        if (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))
        {
            scrollRect.scrollSensitivity = 0;
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            float moveUnit = workBarSize_resize * 0.1f;

            if (scrollDelta > 0f)
            {
                workBarScale += 0.1f;
                if (workBarScale > 1)
                {
                    workBarScale = 1;
                }
            }
            else if (scrollDelta < 0f)
            {
                workBarScale -= 0.1f;
                if (workBarScale < 0.1)
                {
                    workBarScale = 0.1f;
                }
            }
            foreach (GameObject workBars in workTimeBars)
            {
                workBars.GetComponent<RectTransform>().localScale = new Vector3(workBarScale, 1, 1);
                workBars.GetComponent<RectTransform>().anchoredPosition = new Vector2(resizeWorkBarPos_X - (moveUnit / 2f) * ((1 - workBarScale) * 10f), 0);
            }
        }
        else
        {
            //scrollRect.scrollSensitivity = originScrollSensitivity;
        }

        /*
        Vector3 mousePos = Camera.main.WorldToScreenPoint(Input.mousePosition);

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = mousePos;

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(eventData, results);

        // 레이캐스트 결과 있을때
        if (results.Count > 0)
        {
            GameObject hitObject = results[0].gameObject;
            if (hitObject.CompareTag("Time Table"))
            {
                if (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))
                {
                    scrollRect.scrollSensitivity = 0;
                    float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
                    float moveUnit = workBarSize_resize * 0.1f;

                    if (scrollDelta > 0f)
                    {
                        workBarScale += 0.1f;
                        if (workBarScale > 1)
                        {
                            workBarScale = 1;
                        }
                    }
                    else if (scrollDelta < 0f)
                    {
                        workBarScale -= 0.1f;
                        if (workBarScale < 0.1)
                        {
                            workBarScale = 0.1f;
                        }
                    }
                    foreach (GameObject workBars in workTimeBars)
                    {
                        workBars.GetComponent<RectTransform>().localScale = new Vector3(workBarScale, 1, 1);
                        workBars.GetComponent<RectTransform>().anchoredPosition = new Vector2(resizeWorkBarPos_X - (moveUnit / 2f) * ((1 - workBarScale) * 10f), 0);
                    }
                }
                else
                {
                    scrollRect.scrollSensitivity = originScrollSensitivity;
                }
            }
        }
        */
    }

}
