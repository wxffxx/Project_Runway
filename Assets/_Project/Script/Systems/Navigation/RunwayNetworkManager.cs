using System.Collections.Generic;
using UnityEngine;
using PP_RY.Core.Navigation;

namespace PP_RY.Systems.Navigation
{
    public class RunwayNetworkManager : MonoBehaviour
    {
        public static RunwayNetworkManager Instance;

        // 全局存储的所有跑道数据（纯数据图网络）
        [HideInInspector]
        public List<RunwayData> allRunways = new List<RunwayData>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // 把建造器生成的 RunwayData 注册保存到大管家里
        public void RegisterRunway(RunwayData newData)
        {
            allRunways.Add(newData);
            Debug.Log($"【寻路网络】 已注册跑道 {newData.runwayName}, 长度 {newData.length}m, 包含 {newData.centerlineNodes.Count} 个主线节点。");
        }

#if UNITY_EDITOR
        // 为了方便我们在 Scene 窗口中裸眼看到咱们的寻路节点和连线！
        private void OnDrawGizmos()
        {
            if (allRunways == null || allRunways.Count == 0) return;

            foreach (var runway in allRunways)
            {
                // 画线 (Edges)
                Gizmos.color = Color.yellow;
                foreach (var node in runway.centerlineNodes)
                {
                    if (node.connectedEdges != null)
                    {
                        foreach (var edge in node.connectedEdges)
                        {
                            // 画一条连接线
                            Gizmos.DrawLine(edge.fromNode.position, edge.toNode.position);
                        }
                    }
                }

                // 画圆点 (Nodes)
                foreach (var node in runway.centerlineNodes)
                {
                    if (node.type == NodeType.RunwayThreshold || node.type == NodeType.RunwayEnd)
                    {
                        // 首尾接地点用红色大圆圈
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(node.position, 2.5f);
                    }
                    else
                    {
                        // 普通的 100m 间隔点用绿色小圆圈
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(node.position, 1.0f);
                    }
                }
            }
        }
#endif
    }
}
