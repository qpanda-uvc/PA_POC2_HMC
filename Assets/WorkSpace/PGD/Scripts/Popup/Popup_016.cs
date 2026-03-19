using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PGD
{
    public class Popup_016 : MonoBehaviour
    {
        public Transform targetULD;
        [SerializeField] private TMP_Text text_name;
        [SerializeField] private Image image_loadingRate;
        [SerializeField] private TMP_Text text_completionTimeAverage;
        [SerializeField] private TMP_Text text_averageLoadingRate;

        public void SetName(string id)
        {
            text_name.text = "ULD_" + id;
        }

        public void UpdateData(float loadingRate, int averageLoadingRate)
        {
            image_loadingRate.fillAmount = loadingRate / 100f;
            text_averageLoadingRate.text = averageLoadingRate.ToString();
        }

        public void UpdateCompletionTimeData(int minute, int second)
        {
            text_completionTimeAverage.text = minute.ToString() + "Ка" + second.ToString() + "УЪ";
        }
    }
}

