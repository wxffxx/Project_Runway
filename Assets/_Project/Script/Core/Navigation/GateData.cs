using UnityEngine;

namespace PP_RY.Core.Navigation
{
    public enum GateSize
    {
        Small,  // 适合 A, B 类 (通用航空或小型支线客机)
        Medium, // 适合 C 类 (A320, B737)
        Large,  // 适合 D, E 类 (A330, B777, B787)
        Heavy   // 适合 F 类 (A380, B747-8)
    }

    [System.Serializable]
    public class GateData
    {
        public string gateName;            // 例如 "Gate A1", "Stand 201"
        public GateSize supportedSize;     // 能够停靠的最大机型限制

        // 机位中心点，飞机最终停靠和初始生成的物理位置
        public PathNode gateNode;          
        
        // 推出线路的终点，通常位于滑行道上。推车会将飞机从 gateNode 反向推到 pushbackEndNode，然后转为正常滑行。
        public PathNode pushbackEndNode;   

        public GateData(string name, GateSize size)
        {
            this.gateName = name;
            this.supportedSize = size;
        }

        /// <summary>
        /// 在机位和推出终点之间建立一条双向的连线。
        /// 进港（滑行道 -> 机位）：作为普通 StandardTaxiway，正向滑入机位。
        /// 出港（机位 -> 滑行道）：作为 PushbackPath，需要牵引车反向推出。
        /// </summary>
        public void ConnectPushbackRoute(float distanceCost)
        {
            if (gateNode != null && pushbackEndNode != null)
            {
                // 1. 进港路线：从滑行道 (pushbackEndNode) 到机位 (gateNode)，正常滑行进站
                pushbackEndNode.AddEdge(gateNode, distanceCost, EdgeType.StandardTaxiway, isOneWay: true);
                
                // 2. 出港路线：从机位 (gateNode) 到滑行道 (pushbackEndNode)，必须使用牵引车推出
                gateNode.AddEdge(pushbackEndNode, distanceCost, EdgeType.PushbackPath, isOneWay: true);
            }
        }
    }
}
