using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PGD
{
    public class ButtonUI : UIColor
    {
        private GameObject panel;

        void Awake()
        {
            Navigation navigation = gameObject.GetComponent<Button>().navigation;
            navigation.mode = Navigation.Mode.None;
            gameObject.GetComponent<Button>().navigation = navigation;
            gameObject.GetComponent<Button>().colors = SetButtonColor();
        }

        public void FindPanel()
        {
            string[] split = gameObject.name.Split('_');
            GameObject parent = GameObject.Find("Popups");
            panel = parent.transform.Find("Panel_" + split[1]).gameObject;
            gameObject.GetComponent<Button>().onClick.AddListener(OnClickButton);
        }

        public void OnClickButton()
        {
           
            UIManager.Instance.InitializePanel();
            UIManager.Instance.HideBottomUi();
            if (gameObject.GetComponent<MainBottomBtnClickEvent>() != null)
            {
                panel.SetActive(!gameObject.GetComponent<MainBottomBtnClickEvent>().isClick);
            }
            else
            {
                panel.SetActive(true);
            }
            //EventSystem.current.SetSelectedGameObject(null);
        }
    }
}


