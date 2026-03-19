using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class PanelUI : UIColor
    {
        void Start()
        {
            FindCloseBtn();
        }

        public void FindCloseBtn()
        {
            Transform button = transform.GetChild(0).Find("Button_Close");
            if (button != null)
            {
                button.GetComponent<Button>().onClick.AddListener(ClosePanel);

            }
            else
            {
                Debug.LogError(gameObject.name + "  Not find close");
            }
        }

        public void ClosePanel()
        {
            if (StateManager.Instance.isShowUI)
                UIManager.Instance.canvas_world.gameObject.SetActive(true);

            UIManager.Instance.InitializePanel();
            UIManager.Instance.ShowBottomUi();
            gameObject.SetActive(false);
        }

        public void CloseAllPanel()
        {
            gameObject.SetActive(false);
        }
    }
}

