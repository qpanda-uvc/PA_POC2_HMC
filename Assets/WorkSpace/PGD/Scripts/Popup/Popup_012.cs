using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PGD
{
    public class Popup_012 : MonoBehaviour
    {
        public Transform targetAMR;
        [SerializeField] private TMP_Text text_name;
        [SerializeField] private TMP_Text text_destination;
        [SerializeField] private Image image_remainingBattery;
        [SerializeField] private TMP_Text text_remainingBattery;

        public void SetName(string id)
        {
            text_name.text = "AMR_" + id;
        }

        public void UpdateData(string destination, float remainingBattery)
        {
            text_destination.text = destination;
            image_remainingBattery.fillAmount = remainingBattery / 100f;
            text_remainingBattery.text = remainingBattery.ToString() + "%";
        }
    }
}

