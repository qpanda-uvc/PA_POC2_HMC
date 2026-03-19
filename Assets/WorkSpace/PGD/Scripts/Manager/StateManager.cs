using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGD
{
    public class StateManager : MonoBehaviour
    {
        private static StateManager instance = null;
        private CameraController cameraController;

        public bool isSimulationMode = true;
        public bool isShowUI = true;
        public bool isAbleInputKeyboard = true;

        private bool is3D = true;

        public bool Is3D
        {
            get
            {
                return is3D;
            }
            set
            {
                is3D = value;
                if (cameraController != null)
                    cameraController.SetCameraTransform();
            }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
               // DontDestroyOnLoad(gameObject);
            }
            else
            {
                //Destroy(gameObject);
            }

            if (Camera.main != null)
                cameraController = Camera.main.GetComponent<CameraController>();
        }

        public static StateManager Instance
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
    }
}

