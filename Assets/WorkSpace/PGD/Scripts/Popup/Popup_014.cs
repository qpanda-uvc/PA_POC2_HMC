using UnityEngine;
using TMPro;

namespace PGD
{
    public class Popup_014 : MonoBehaviour
    {
        public Transform targetVMS;
        [SerializeField] private TMP_Text text_name;
        [SerializeField] private TMP_Text text_state;
        [SerializeField] private TMP_Text text_measurementCounting;

        public void SetName(string id)
        {
            text_name.text = "VMS_" + id;
        }

        public void UpdateData(string state, int measurementCounting)
        {
            text_state.text = state;
            text_measurementCounting.text = measurementCounting.ToString() + "°Ç";
        }
    }
}

