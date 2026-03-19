using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGD
{
    public class Panel_008 : MonoBehaviour
    {
        public void OnClickOkBtn()
        {
            // 서버 재연결 시도
            UIManager.Instance.ShowBottomUi();
            gameObject.SetActive(false);
        }
    }

}
