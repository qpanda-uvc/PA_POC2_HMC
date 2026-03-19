using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WSH
{
    public class WayPoint : MonoBehaviour
    {
        public List<WayPoint> connectedPoints = new List<WayPoint>();
        public bool isFull;
        public void CreateConnectPoint()
        {
            var newPoint = Instantiate(gameObject).GetComponent<WayPoint>();
            newPoint.name = "WayPoint";
            newPoint.connectedPoints = new List<WayPoint>();
            newPoint.connectedPoints.Add(this);
            newPoint.transform.SetParent(transform.parent);
            newPoint.transform.position = transform.position;
            //Select Object
            Selection.activeGameObject = newPoint.gameObject;
            connectedPoints.Add(newPoint);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out AMR amr))
                return;
            isFull = true;
            amr.OnPointEnter(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out AMR amr))
                return;
            isFull = false;
            amr.OnPointExit(this);
        }

        private void OnDrawGizmos()
        {
            foreach (var connectedPoint in connectedPoints)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, connectedPoint.transform.position);
            }
        }
    }
}