using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WSH
{
    public class Map : MonoBehaviour
    {
        public WayPoint[] wayPoints;
        public WayPoint VMSOut;
        public WayPoint ASRSIn;
        public WayPoint ASRSOut;
        public List<WayPoint> skidPoints = new();
        public List<WayPoint> watingPoints = new();
        public List<WayPoint> chargePoints = new();
        public List<WayPoint> startPoints = new List<WayPoint>();
        public HashSet<WayPoint> entryPoints = new();
        public HashSet<WayPoint> portPoints = new();
        public void PointLoad()
        {
            wayPoints = GetComponentsInChildren<WayPoint>();
            VMSOut = null;
            ASRSIn = null;
            ASRSOut = null;
            skidPoints.Clear();
            portPoints.Clear();
            entryPoints.Clear();
            startPoints.Clear();
            watingPoints.Clear();
            chargePoints.Clear();
            foreach (var w in wayPoints)
            {
                if(w.TryGetComponent(out Tag_StartPoint sp))
                {
                    startPoints.Add(w);
                }
                
                if(w.TryGetComponent(out Tag_Charging cp))
                {
                    chargePoints.Add(w);
                }

                if(w.TryGetComponent(out Tag_Waiting wp))
                {
                    watingPoints.Add(w);
                }

                if (w.TryGetComponent(out Tag_SKID sk))
                {

                    portPoints.Add(w);
                    skidPoints.Add(w);
                }

                if(w.TryGetComponent(out Tag_EntryPoint ep))
                {
                    entryPoints.Add(w);
                }

                if (VMSOut==null && w.TryGetComponent(out Tag_VMSOut vo))
                {
                    portPoints.Add(w);
                    VMSOut = w;
                }

                if (ASRSIn == null && w.TryGetComponent(out Tag_ASRSIn ai))
                {
                    portPoints.Add(w);
                    ASRSIn = w;
                }

                if (ASRSOut ==null && w.TryGetComponent(out Tag_ASRSOut ao))
                {
                    portPoints.Add(w);
                    ASRSOut = w;
                }

            }
        }
        internal List<WayPoint> FindPath(WayPoint currentPoint, WayPoint targetPoint)
        {
            List<WayPoint> result = new();
            searchPoint.Clear();
            FindPath(currentPoint, targetPoint, ref result);
            result.Reverse();
            result.Insert(0,currentPoint);
            result.Add(targetPoint);
            return result;
        }

        HashSet<WayPoint> searchPoint = new();

        bool FindPath(WayPoint cp, WayPoint tp, ref List<WayPoint> path)
        {
            searchPoint.Add(cp);
            
            foreach (var p in cp.connectedPoints)
            {
                if (searchPoint.Contains(p))
                    continue;
                if (FindPath(p, tp, ref path))
                {
                    path.Add(p);
                    return true;
                }
                if (p == tp)
                {
                    return true;
                }
                    
                
            }
            return false;
        }

        public WayPoint GetEmptyChargePoint()
        {
            foreach (var cp in chargePoints)
            {
                if (!cp.isFull)
                    return cp;
            }
            return null;
        }
        public bool isSkidPoint(WayPoint wp)
        {
            if (skidPoints.Contains(wp))
                return true;
            return false;
        }

        public bool isChargePoint(WayPoint wp)
        {
            if (chargePoints.Contains(wp))
                return true;
            return false;
        }

        internal bool isEntryPoint(WayPoint targetPoint)
        {
            return entryPoints.Contains(targetPoint);
        }
        public bool isPortPoint(WayPoint p)
        {
            return portPoints.Contains(p);
        }
    }
}