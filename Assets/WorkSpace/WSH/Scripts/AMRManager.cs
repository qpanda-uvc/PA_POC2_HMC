using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace WSH
{
    public enum TaskType
    {
        MoveToVMS,
        MoveToASRSIn,
        MoveToASRSOut,
        LoadVMS,
        UnloadASRS,
        LoadASRS,
        MoveToSkid,
        MoveToCharge,
        UnloadSkid,
    }
    [Serializable]
    public class AMRTaskInfo
    {
        public string workerID;
        public TaskType taskType;
        public List<WayPoint> path = new();
        public AMRTaskInfo(string id, TaskType t)
        {
            workerID = id;
            taskType = t;
        }
    }

    public class AMRManager : MonoBehaviour
    {
        public List<AMR> amrList = new List<AMR>();
        Dictionary<string, AMR> workerTable = new();
        public int amrCount;
        int amrIndex;
        public Map map;
        public AMR prf_AMR;
        public AMR testAMR;
        public WayPoint startPoint;

        private void Awake()
        {
            map = FindObjectOfType<Map>();
            map.PointLoad();
            workerTable.Clear();
            amrIndex = 0;
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                if (amrIndex == amrCount)
                    amrIndex = 0;
                var id = amrIndex.ToString();
                RegistTask(new AMRTaskInfo(id, TaskType.MoveToVMS));
                RegistTask(new AMRTaskInfo(id, TaskType.LoadVMS));
                RegistTask(new AMRTaskInfo(id, TaskType.MoveToASRSIn));
                RegistTask(new AMRTaskInfo(id, TaskType.UnloadASRS));
                RegistTask(new AMRTaskInfo(id, TaskType.MoveToASRSOut));
                RegistTask(new AMRTaskInfo(id, TaskType.LoadASRS));
                RegistTask(new AMRTaskInfo(id, TaskType.MoveToSkid));
                RegistTask(new AMRTaskInfo(id, TaskType.UnloadSkid));
                RegistTask(new AMRTaskInfo(id, TaskType.MoveToCharge));
                amrIndex++;
            }
        }

        public void SpawnAMR(string id, WayPoint spawnPoint)
        {
            var amr = Instantiate(prf_AMR, spawnPoint.transform.position, spawnPoint.transform.rotation);
            amr.id = id;
            //amr.map = map;
            amr.transform.SetParent(transform);
            amrList.Add(amr);
            workerTable.Add(id, amr);
        }

        public void RegistTask(AMRTaskInfo taskInfo)
        {
            if (!FindWorker(taskInfo.workerID, out var worker))
            {
                return;
            }
            worker.AddTask(taskInfo);
        }

        bool FindWorker(string id, out AMR worker)
        {
            if (workerTable.TryGetValue(id, out worker))
                return true;

            SpawnAMR(id, startPoint);
            return workerTable.TryGetValue(id, out worker);
            Debug.Log($"Not Found Worker! {id}");
            return false;
        }
    }
}