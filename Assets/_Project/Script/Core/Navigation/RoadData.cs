using System.Collections.Generic;
using UnityEngine;

namespace PP_RY.Core.Navigation
{
    /// <summary>
    /// 陆侧车辆半寻路体系 (The Half-Pathfinding System for Landside)
    /// 纯粹的样条线/航点数据结构，完全没有复杂的 A* Graph逻辑。
    /// </summary>
    [System.Serializable]
    public class RoadData
    {
        public string id;
        
        // 车辆将在这群路标中穿梭
        public List<Vector3> waypoints = new List<Vector3>();

        // 道路的总长度（只为 UI 和概览使用）
        public float totalLength;

        public RoadData(string id)
        {
            this.id = id;
            this.waypoints = new List<Vector3>();
        }

        public void AddWaypoint(Vector3 wp)
        {
            if (waypoints.Count > 0)
            {
                totalLength += Vector3.Distance(waypoints[waypoints.Count - 1], wp);
            }
            waypoints.Add(wp);
        }
    }
}
