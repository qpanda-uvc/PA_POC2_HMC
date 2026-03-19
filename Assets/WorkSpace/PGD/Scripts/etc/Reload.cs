using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGD
{
    public class Reload : MonoBehaviour
    {
        private static Reload instance = null;

        public static Reload Instance
        {
            get
            {
                if (instance == null)
                {
                    return null;
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

        }

        void Start()
        {
            UIManager.Instance.panel_main.SetActive(true);
        }
    }
}


