using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGD
{
    public class PopupTargetObject : MonoBehaviour
    {
        public GameObject targetPopup;

        void Update()
        {
            if (Camera.main == null || targetPopup == null) return;
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            screenPoint.y += 150f;

            if (targetPopup != null)
            {
                if (gameObject.name.Contains("Storage_"))
                {
                    float add = gameObject.name == "Storage_" + UIManager.Instance.FindAsrsDictionaryIndex(1) ? 200f : -200f;
                    targetPopup.transform.position = new Vector3(screenPoint.x + add, screenPoint.y, screenPoint.z);
                }
                else
                {
                    targetPopup.transform.position = screenPoint;
                }     
            }
        }

        private void OnBecameInvisible()
        {
            if (targetPopup != null)
            {
                targetPopup.GetComponent<CanvasGroup>().alpha = 0;
            }
        }

        private void OnBecameVisible()
        {
            if (targetPopup != null)
            {
                targetPopup.GetComponent<CanvasGroup>().alpha = 1;
            }
        }
    }
}

