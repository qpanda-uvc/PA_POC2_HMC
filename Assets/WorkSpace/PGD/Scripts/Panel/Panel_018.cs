using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGD
{
    public class Panel_018 : MonoBehaviour
    {
        public void OnClickOkBtn()
        {
            StateManager.Instance.isSimulationMode = false;
            gameObject.SetActive(false);
        }
    }
}

