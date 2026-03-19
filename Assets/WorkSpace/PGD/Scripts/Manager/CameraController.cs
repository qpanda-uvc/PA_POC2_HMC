 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGD
{
    public class CameraController : MonoBehaviour
    {
        [Header("3D")]
        [SerializeField] private float move3DSpeed = 25f;
        [SerializeField] private float rotate3DSpeed = 50f;
        [SerializeField] private float zoom3DSpeed = 500f;

        [SerializeField] private float camera3DYMin = 2f;
        [SerializeField] private float camera3DYMax = 40f;


        [SerializeField] private float camera3DZoomMin = 35f;
        [SerializeField] private float camera3DZoomMax = 60f;

        private Vector3 current3DRotation;
        private Vector3 current3DPosition;

        [Header("2D")]
        [SerializeField] private float move2DSpeed = 20f;
        [SerializeField] private float zoom2DSpeed = 40f;

        [SerializeField] private float camera2DZoomMin = 5f;
        [SerializeField] private float camera2DZoomMax = 10f;
        
        private Vector3 current2DPosition;
        private Vector3 current2DRotation;

        private Transform targetObject;

        private Vector3 CurrentMigrationPosition
        {
            get
            {
                return StateManager.Instance.Is3D ? new Vector3(Mathf.Floor(transform.position.x * 10f) / 10f,
                                                                Mathf.Floor(transform.position.y * 10f) / 10f,
                                                                Mathf.Floor(transform.position.z * 10f) / 10f) 
                                                    :
                                                     new Vector3(Mathf.Floor(transform.position.x * 10f) / 10f,
                                                                 Mathf.Floor(transform.position.y * 10f) / 10f,
                                                                 Mathf.Floor(transform.position.z * 10f) / 10f);
            }
        }

        private Vector3 targetMigrationPostion;

        public Vector3 TargetMigrationPostion
        {
            get
            {
                return StateManager.Instance.Is3D ? new Vector3(Mathf.Floor(targetMigrationPostion.x * 10f) / 10f,
                                                                Mathf.Floor(targetMigrationPostion.y * 10f) / 10f,
                                                                Mathf.Floor(targetMigrationPostion.z * 10f) / 10f)
                                                    :
                                                    new Vector3(Mathf.Floor(targetMigrationPostion.x * 10f) / 10f,
                                                                Mathf.Floor(camera2DZoomMax * 10f) / 10f,
                                                                Mathf.Floor(targetMigrationPostion.z * 10f) / 10f);
            }
            set
            {
                targetMigrationPostion = value;
            }
        }

        private void Start()
        {
            current3DPosition = transform.position;
            current3DRotation = transform.eulerAngles;

            current2DPosition = new Vector3(30f, camera2DZoomMax, -15f);
            current2DRotation = new Vector3(90f, 0f, 0f);
        }

        void Update()
        {
            if (StateManager.Instance.isAbleInputKeyboard)
            {
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, camera3DZoomMax, 0.05f);

                if (Input.GetMouseButton(1))
                {
                    MoveKeyBoard();
                }
            }           
            else if (!StateManager.Instance.isAbleInputKeyboard)
            {
                MoveToTarget();
            }          
        }

        private void MoveKeyBoard()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            if (StateManager.Instance.Is3D)
            {
                Move3DCamera(x, z);
                Rotate3DCamera();
            }
            else
            {
                Move2DCamera(x, z);
            }

            UpdateCurrentTransfrom();
        }

        private void MoveToTarget()
        {
            if (CurrentMigrationPosition != TargetMigrationPostion)
            {
                transform.position = Vector3.Lerp(transform.position, targetMigrationPostion, Time.deltaTime * 2f);
                Vector3 dir = (targetObject.position - transform.position).normalized;
             
                if (StateManager.Instance.Is3D)
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 2f);

            }
            else
            {
                if (StateManager.Instance.Is3D)
                    Zoom3DCamera();
                else
                    Zoom2DCamera();
            }
        }

        private void Zoom3DCamera()
        {
            float z = -Input.GetAxis("Mouse ScrollWheel") * zoom3DSpeed * Time.deltaTime;
            Camera.main.fieldOfView = Camera.main.fieldOfView + z;
            Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, camera3DZoomMin, camera3DZoomMax);
        }


        private void Zoom2DCamera()
        {
            float z = -Input.GetAxis("Mouse ScrollWheel") * zoom2DSpeed * Time.deltaTime;
            Camera.main.orthographicSize = Camera.main.orthographicSize + z;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, camera2DZoomMin, camera2DZoomMax);
        }

        public void SetCameraTransform()
        {
            if (StateManager.Instance.Is3D)
            {
                Camera.main.fieldOfView = camera3DZoomMax;
                Camera.main.orthographic = false;
                transform.position = current3DPosition;
                transform.eulerAngles = current3DRotation;
            }
            else
            {
                Camera.main.orthographicSize = camera2DZoomMax;
                Camera.main.orthographic = true;
                transform.position = current2DPosition;
                transform.eulerAngles = current2DRotation;
            }
        }

        private void Move3DCamera(float x, float z)
        {
            float y = Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f;

            Vector3 prevTransform = transform.position;

            transform.Translate(new Vector3(x, y, z) * move3DSpeed * Time.deltaTime);

            float currentY = transform.position.y;

            float clampY = Mathf.Clamp(transform.position.y, camera3DYMin, camera3DYMax);

            if (currentY != clampY)
            {
                transform.position = prevTransform; 
            }
        }

        void Rotate3DCamera()
        {
            float x = -Input.GetAxis("Mouse Y");
            float y = Input.GetAxis("Mouse X");

            transform.eulerAngles = current3DRotation + new Vector3(x, y, 0) * rotate3DSpeed * Time.deltaTime;
        }

        private void Move2DCamera(float x, float z)
        {
            Zoom2DCamera();
            transform.Translate(new Vector2(x, z) * move2DSpeed * Time.deltaTime);
            //transform.position = Clamp2DTransform();
        }

        //private Vector3 Clamp2DTransform()
        //{
        //    return new Vector3(
        //          transform.position.x,
        //          Mathf.Clamp(transform.position.y, camera2DZoomMin, camera2DZoomMax),
        //          transform.position.z
        //        );
        //}

        private void UpdateCurrentTransfrom()
        {
            if (StateManager.Instance.Is3D)
            {
                current3DPosition = transform.position;
                current3DRotation = transform.eulerAngles;
            }
            else
            {
                current2DPosition = transform.position;
            }                
        }

        public void MoveToTarget(string name)
        {
            StateManager.Instance.isAbleInputKeyboard = false;
            targetObject = GameObject.Find(name).transform;
            UIManager.Instance.canvas_world.gameObject.SetActive(false);

            if (StateManager.Instance.Is3D)
            {
                if(name.Contains("Storage_")) // 자동창고
                {
                    UIManager.Instance.canvas_013_1.transform.parent.gameObject.SetActive(true);
                    UIManager.Instance.canvas_013_1.GetComponent<CanvasGroup>().alpha = name == ("Storage_" + UIManager.Instance.FindAsrsDictionaryIndex(0)) ? 1 : 0;
                    UIManager.Instance.canvas_013_2.GetComponent<CanvasGroup>().alpha = name == ("Storage_" + UIManager.Instance.FindAsrsDictionaryIndex(1)) ? 1 : 0;

                    float add = name == "Storage_" + UIManager.Instance.FindAsrsDictionaryIndex(1) ? -14f : 17f;
                    TargetMigrationPostion = new Vector3(targetObject.transform.position.x, targetObject.transform.position.y, targetObject.transform.position.z + add);
                }
                else
                {
                    TargetMigrationPostion = new Vector3(targetObject.transform.position.x, targetObject.transform.position.y + 10f, targetObject.transform.position.z - 10f);
                }
            }
            else
            {
                TargetMigrationPostion = new Vector3(targetObject.transform.position.x, camera2DZoomMax, targetObject.transform.position.z);
            } 
        }

        public void cancelTarget()
        {
            UIManager.Instance.canvas_013_1.transform.parent.gameObject.SetActive(false);
            UpdateCurrentTransfrom();

            StateManager.Instance.isAbleInputKeyboard = true;
        }
        
    }
}

