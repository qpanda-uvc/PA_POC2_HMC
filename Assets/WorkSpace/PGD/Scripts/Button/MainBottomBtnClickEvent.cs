using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class MainBottomBtnClickEvent : MonoBehaviour
    {
        public bool isClick = false;

        [SerializeField] private GameObject image_clickBg;
        [SerializeField] private Image image_icon;
        [SerializeField] private TMP_Text text_name;

        [SerializeField] private Color selectedTextColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color unselectedTextColor = new Color(177f / 255f, 177f / 255f, 177f / 255f, 1f);


        void Start()
        {
            gameObject.GetComponent<Button>().onClick.AddListener(Toggle);
        }

        private void OnEnable()
        {
            TurnOff();
        }

        private void Toggle()
        {
            if (isClick)
                TurnOff();
            else
                TurnOn();  
        }

        public void TurnOn()
        {
            if (gameObject.name != "Toggle_UI")
            {
                UIManager.Instance.TurnOffBottomBtn(transform.parent);   
            }
              
            image_clickBg.SetActive(true);

            isClick = true;

            image_icon.color = selectedTextColor;
            if (text_name != null)
                text_name.color = selectedTextColor;
        }

        public void TurnOff()
        {           
            image_clickBg.SetActive(false);


            isClick = false;

            image_icon.color = unselectedTextColor;
            if (text_name != null)
                text_name.color = unselectedTextColor;
        }
    }
}

