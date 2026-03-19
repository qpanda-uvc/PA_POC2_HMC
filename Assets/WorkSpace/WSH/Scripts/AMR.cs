using System;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WSH
{
    public class AMR : MonoBehaviour
    {
        public AMRSpec specData;
        public string id;
        public float bc;
        bool batteryFullCallback;
        bool batteryHalfCallback;
        bool batteryLowCallback;
        public float batteryCapacity
        {
            get
            {
                return bc;
            }
            set
            {
                if (bc >= 100f)
                {
                    if (!batteryFullCallback)
                    {
                        batteryHalfCallback = false;
                        batteryLowCallback = false;
                        batteryFullCallback = true;
                        onBatteryFull?.Invoke(this);
                        Debug.Log($"Battery Full:{name}");
                    }
                    bc = 100f;
                    return;
                }
                else if (!batteryHalfCallback && bc >= 50f && bc <= 51f)
                {
                    batteryHalfCallback = true;
                    batteryLowCallback = false;
                    batteryFullCallback = false;
                    onBatteryHalf?.Invoke(this);
                    Debug.Log($"Battery half:{name}");
                }
                else if (!batteryLowCallback && bc >= 30f && bc <= 31f)
                {
                    batteryLowCallback = true;
                    batteryFullCallback = false;
                    batteryHalfCallback = false;
                    onBatteryLow?.Invoke(this);
                    Debug.Log($"Battery Low:{name}");
                }
                else if(bc<=0f)
                {
                    bc = 0f;
                    return;
                }
                bc = value;
            }
        }
        public float moveSpeed => specData.moveSpeed;
        public float rotateSpeed => specData.rotateSpeed;
        public float dockingSpeed => specData.dockingSpeed;
        public float bc_Idle => specData.bc_Idle;
        public float bc_Move => specData.bc_Move;
        public float bc_Lift => specData.bc_Lift;
        public float bc_Rotate => specData.bc_Rotate;
        public float bc_Docking => specData.bc_Docking;
        public float liftSpeed => specData.liftSpeed;
        public GameObject cargo;
        public WayPoint currentPoint;
        public WayPoint targetPoint;

        Vector3 prevLocate;
        Vector3 nextLocate;

        public State _state;
        int pathIndex;
        float process;
        float moveLength;
        
        public AMRTaskInfo currentTask;
        Queue<AMRTaskInfo> tasks = new Queue<AMRTaskInfo>();
        
        public event Action onStateChanged;
        public event Action<AMR> onTaskEnd;
        public event Action<AMR> onTaskStart;
        public event Action<AMR> onLoadStartVMS;
        public event Action<AMR> onLoadEndVMS;
        public event Action<AMR> onUnloadStartASRS;
        public event Action<AMR> onUnloadEndASRS;
        public event Action<AMR> onLoadStartASRS;
        public event Action<AMR> onLoadEndASRS;
        public event Action<AMR> onBatteryFull;
        public event Action<AMR> onBatteryHalf;
        public event Action<AMR> onBatteryLow;
        public event Action<AMR> onUnloadSkid;
        public event Action<AMR> onUnloadEndSkid;

        event Action stateLoop;
        event Action<AMR> onPointExit;
        event Action<AMR> onPointEnter;

        Map map;
        float liftEnd;
        float liftStart;
        float liftHeight;
        Tag_Lift lift;
        public bool isLoaded;

        public enum State
        {
            Idle,
            TaskReady,
            MoveReady,
            Move,
            MoveEnd,
            Charging,
            Error,
            TaskEnd,
            WaitingVMS,
            LoadVMS,
            LoadEndVMS,
            UnloadASRS,
            UnloadEndASRS,
            Docking,
            Rotate,
            WaitingASRS,
            LoadASRS,
            LoadEndASRS,
            UnloadSkid,
            UnloadEndSkid,
            UnloadStartASRS,
            UnloadStartSkid,
        }
        public State state
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state == value)
                    return;
                
                _state = value;
                onStateChanged?.Invoke();
            }
        }

        void Awake()
        {
            cargo = null;
            map = FindObjectOfType<Map>();
            specData = Resources.Load<AMRSpec>("AMRSpecData");
            SetLift();
            bc = 100f;
            StateChange(State.Idle);
        }
        
        /// <summary>
        /// AMR 프리팹 내의 Lift 를 찾아 초기 설정 하는 함수
        /// </summary>
        void SetLift()
        {
            lift = GetComponentInChildren<Tag_Lift>();
            liftStart = lift.transform.parent.position.y;
            liftHeight = lift.GetComponent<CapsuleCollider>().height * lift.transform.localScale.y;
            liftEnd = liftStart + liftHeight;
        }

        void Update()
        {
            stateLoop();
        }
        /// <summary>
        /// 상태 전환용 함수. State 변경시 반드시 이 함수로 변경.
        /// </summary>
        /// <param name="nextState"></param>
        void StateChange(State nextState)
        {
            Debug.Log($"StateChange : {nextState}");
            switch (nextState)
            {
                case State.Idle:
                    stateLoop = Idle;
                    break;
                case State.TaskReady:
                    stateLoop = TaskReady;
                    break;
                case State.TaskEnd:
                    stateLoop = TaskEnd;
                    break;
                case State.MoveReady:
                    stateLoop = MoveReady;
                    break;
                case State.Docking:
                    stateLoop = Docking;
                    break;
                case State.Move:
                    stateLoop = Move;
                    break;
                case State.Rotate:
                    stateLoop = Rotate;
                    break;
                case State.MoveEnd:
                    stateLoop= MoveEnd;
                    break;
                case State.WaitingVMS:
                    stateLoop = WaitingVMS;
                    break;
                case State.LoadVMS:
                    stateLoop = LoadVMS;
                    break;
                case State.LoadEndVMS:
                    stateLoop = LoadEndVMS;
                    break;

                case State.UnloadStartASRS:
                    stateLoop = UnloadReadyASRS;
                    break;

                case State.UnloadASRS:
                    stateLoop = UnloadASRS;
                    break;
                case State.UnloadEndASRS:
                    stateLoop = UnloadEndASRS;
                    break;

                case State.WaitingASRS:
                    stateLoop = WaitingASRS;
                    break;
                case State.LoadASRS:
                    stateLoop = LoadASRS;
                    break;
                case State.LoadEndASRS:
                    stateLoop = LoadEndASRS;
                    break;

                case State.UnloadStartSkid:
                    stateLoop = UnloadReadySkid;
                    break;
                case State.UnloadSkid:
                    stateLoop = UnloadSkid;
                    break;

                case State.UnloadEndSkid:
                    stateLoop = UnloadEndSkid;
                    break;

                case State.Charging:
                    break;
                case State.Error:
                    break;
            }
            state = nextState;
        }
        void UnloadReadySkid()
        {
            if (!map.isSkidPoint(currentPoint))
            {
                Debug.Log($"{currentPoint} is Not Skid Point!");
                StateChange(State.TaskEnd);
                return;
            }
            process = 0f;
            onUnloadSkid?.Invoke(this);
            StateChange(State.UnloadSkid);
        }
        void UnloadSkid()
        {
            LiftDown();
            if(process>=1f)
            {
                StateChange(State.UnloadEndSkid);
            }
        }

        void UnloadEndSkid()
        {
            onUnloadEndSkid?.Invoke(this);
            StateChange(State.TaskEnd);
        }

        void TaskReady()
        {
            if(tasks.Count==0)
            {
                StateChange(State.Idle);
                return;
            }

            onTaskStart?.Invoke(this);
            currentTask = tasks.Dequeue();
            switch (currentTask.taskType)
            {
                case TaskType.MoveToVMS:
                    MoveTo(map.VMSOut);
                    break;
                
                case TaskType.MoveToASRSIn:
                    MoveTo(map.ASRSIn);
                    break;
                
                case TaskType.MoveToASRSOut:
                    MoveTo(map.ASRSOut);
                    break;
                
                case TaskType.LoadVMS:
                    StateChange(State.WaitingVMS);
                    break;

                case TaskType.UnloadASRS:
                    onUnloadStartASRS?.Invoke(this);
                    StateChange(State.UnloadStartASRS);
                    break;

                case TaskType.LoadASRS:
                    StateChange(State.WaitingASRS);
                    break;

                case TaskType.MoveToSkid:
                    MoveTo(map.skidPoints[Random.Range(0, map.skidPoints.Count)]);
                    break;

                case TaskType.MoveToCharge:
                    var chargePoint = map.GetEmptyChargePoint();
                    MoveTo(chargePoint);
                    break;

                case TaskType.UnloadSkid:
                    StateChange(State.UnloadStartSkid);
                    break;
            }
        }


        void WaitingASRS()
        {
            batteryCapacity -= bc_Idle * Time.deltaTime;
            if (!isLoaded)
                return;
            process = 0f;
            onLoadStartASRS?.Invoke(this);
            StateChange(State.LoadASRS);
        }
        void LoadASRS()
        {
            LiftUp();
            if (process >= 1f)
            {
                StateChange(State.LoadEndASRS);
            }
        }
        void LiftUp()
        {
            process += (Time.deltaTime * liftSpeed) / liftHeight;
            batteryCapacity -= (Time.deltaTime * bc_Lift);
            var liftPos = Mathf.Lerp(liftStart, liftEnd, process);
            lift.transform.parent.position = new Vector3(lift.transform.parent.position.x, liftPos, lift.transform.parent.position.z);
        }

        void LiftDown()
        {
            process += liftSpeed * Time.deltaTime;
            batteryCapacity -= bc_Lift * Time.deltaTime;
            var liftPos = Mathf.Lerp(liftEnd, liftStart, process);
            lift.transform.parent.position = new Vector3(lift.transform.parent.position.x, liftPos, lift.transform.parent.position.z);

        }
        void LoadEndASRS()
        {
            onLoadEndASRS?.Invoke(this);
            StateChange(State.TaskEnd);
        }

        void WaitingVMS()
        {
            batteryCapacity -= bc_Idle * Time.deltaTime;
            if (!isLoaded)
                return;

            process = 0f;
            onLoadStartVMS?.Invoke(this);
            StateChange(State.LoadVMS);                
        }

        void LoadVMS()
        {
            LiftUp();
            if(process>=1f)
            {
                StateChange(State.LoadEndVMS);
            }
        }

        void LoadEndVMS()
        {
            onLoadEndVMS?.Invoke(this);
            StateChange(State.TaskEnd);
        }

        void UnloadReadyASRS()
        {
            if (!currentPoint.TryGetComponent(out Tag_ASRSIn ai))
            {
                Debug.Log($"Wrong Task! This is Not ASRS In Point! {currentPoint}");
                StateChange(State.TaskEnd);
                return;
            }
            StateChange(State.UnloadASRS);
        }

        void UnloadASRS()
        {
            
            LiftDown();
            if (process >= 1f)
            {
                StateChange(State.UnloadEndASRS);
            }
        }

        void UnloadEndASRS()
        {
            onUnloadEndASRS?.Invoke(this);
            StateChange(State.TaskEnd);
        }
        void MoveTo(WayPoint targetPoint)
        {
            pathIndex = 0;
            currentTask.path = map.FindPath(currentPoint, targetPoint);
            StateChange(State.MoveReady);
        }

        void TaskEnd()
        {
            onTaskEnd?.Invoke(this);
            process = 0f;
            StateChange(State.TaskReady);
        }
        void Idle()
        {
            if (!map.isChargePoint(currentPoint))
                batteryCapacity -= Time.deltaTime * bc_Idle;
            if(tasks.Count>0)
            {
                StateChange(State.TaskReady);
            }
        }

        WayPoint prevPoint;
        float angle;
        Vector3 startRot;
        Vector3 endRot;
        void MoveReady()
        {
            prevPoint = targetPoint;
            targetPoint = currentTask.path[pathIndex++];
            process = 0f;
            prevLocate = transform.position;
            nextLocate = targetPoint.transform.position;
            moveLength = Vector3.Distance(prevLocate, nextLocate);

            if (prevPoint != null && map.isEntryPoint(prevPoint) && map.isPortPoint(targetPoint))
            {
                StateChange(State.Docking);
                return;
            }

            if (map.isPortPoint(currentPoint) && map.isEntryPoint(targetPoint))
            {
                StateChange(State.Docking);
                return;
            }

            if(currentPoint == targetPoint)
            {
                StateChange(State.MoveEnd);
                return;
            }

            Vector3 nextDir = targetPoint.transform.position - transform.position;
            angle = Vector3.Angle(transform.forward, nextDir);
            if (angle >= 91f)
                angle = 0f;
            if (angle >= 3f)
            {
                startRot = transform.rotation.eulerAngles;
                endRot = startRot;
                if (transform.forward.z <= 0f)
                    endRot.y -= angle;
                else
                    endRot.y += angle;
                StateChange(State.Rotate);
                return;
            }

            StateChange(State.Move);
        }
        private void OnDrawGizmos()
        {
            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(transform.position, transform.position +(transform.forward*5));
            //if(targetPoint!=null)
            //{
            //    Gizmos.color = Color.green;
            //    Gizmos.DrawLine(transform.position, targetPoint.transform.position);
            //    Gizmos.color = Color.cyan;
            //    GizmosExtensions.DrawWireArc(transform.position, transform.forward,, 5f, 5f);
            //    //Get angle for transform.position , targetPoint.transform.position;

            //}
        }

        void Rotate()
        {
            process += rotateSpeed * Time.deltaTime;
            batteryCapacity -= Time.deltaTime * bc_Rotate;
            transform.eulerAngles = Vector3.Lerp(startRot, endRot, process);
            if (process >= 1f)
            {
                process = 0f;
                StateChange(State.Move);
            }
        }

        void Docking()
        {
            process += (Time.deltaTime * dockingSpeed) / moveLength;
            batteryCapacity -= Time.deltaTime * bc_Docking;
            transform.position = Vector3.Lerp(prevLocate, nextLocate, process);
            if (process >= 1f)
            {
                StateChange(State.MoveEnd);
            }
        }

        void Move()
        {
            process += (Time.deltaTime * moveSpeed) / moveLength;
            batteryCapacity -= Time.deltaTime * bc_Move;
            transform.position = Vector3.Lerp(prevLocate, nextLocate, process);
            if(process>=1f)
            {
                StateChange(State.MoveEnd);
            }
        }

        void MoveEnd()
        {
            if (pathIndex < currentTask.path.Count)
                StateChange(State.MoveReady);
            else
                StateChange(State.TaskEnd);
        }

        internal void OnPointEnter(WayPoint wayPoint)
        {
            Debug.Log($"{name} In {wayPoint.name}");
            currentPoint = wayPoint;
            onPointEnter?.Invoke(this);
        }

        internal void OnPointExit(WayPoint wayPoint)
        {
            Debug.Log($"{name} Exit {wayPoint.name}");
            onPointExit?.Invoke(this);
        }

        internal void AddTask(AMRTaskInfo taskInfo)
        {
            tasks.Enqueue(taskInfo);
        }
    }
}