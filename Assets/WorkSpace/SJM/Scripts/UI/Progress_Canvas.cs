using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Progress_Canvas : MonoBehaviour
{
    public Slider slider;
    public TMP_Text progress;   

    public void Initialize()
    {
        progress = transform.Find("Slider").gameObject.transform.Find("Progress_txt").gameObject.GetComponent<TMP_Text>();
        slider = transform.Find("Slider").gameObject.GetComponent<Slider>();
    }

    public void SetProgressBar(float progressData)
    {
        progress.text = (progressData * 100).ToString() + "%";
        slider.value = progressData;
    }
}
