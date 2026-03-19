using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PGD
{
    public class Popup_013 : MonoBehaviour
    {
        public Transform targetASRS;
        [SerializeField] private TMP_Text text_name;
        [SerializeField] private Image image_storageKappa;
        [SerializeField] private TMP_Text text_storage;
        [SerializeField] private TMP_Text text_outputWaiting;

        public void SetName(string id)
        {
            text_name.text = "ASRS_" + id;
        }

        public void UpdateData(float storageKappa, int storage, int outputWaiting)
        {
            image_storageKappa.fillAmount = storageKappa / 100f;
            text_storage.text = storage.ToString();
            text_outputWaiting.text = outputWaiting.ToString();
        }
    }
}

