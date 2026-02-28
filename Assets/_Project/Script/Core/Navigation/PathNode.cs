using System.Collections.Generic;
using UnityEngine;

namespace PP_RY.Core.Navigation
{
    [System.Serializable]
    public class PathNode
    {
        public string id;              // 唯一标识符，便于调试 ("RWY_01L_Node_0")
        public Vector3 position;       // 存在于世界中的虚拟坐标
        public NodeType type;          // 节点类型
        
        // 我们不序列化到 Inspector 显示深度嵌套，避免死循环崩溃
        [System.NonSerialized]
        public List<PathEdge> connectedEdges = new List<PathEdge>();

        // 有参构造函数方便生成
        public PathNode(string id, Vector3 position, NodeType type)
        {
            this.id = id;
            this.position = position;
            this.type = type;
            this.connectedEdges = new List<PathEdge>();
        }

        // 添加连线的方法
        public void AddEdge(PathNode to, float cost, EdgeType edgeType, bool isOneWay = false)
        {
            PathEdge edge = new PathEdge(this, to, cost, edgeType, isOneWay);
            connectedEdges.Add(edge);

            if (!isOneWay)
            {
                PathEdge reverseEdge = new PathEdge(to, this, cost, edgeType, isOneWay);
                to.connectedEdges.Add(reverseEdge);
            }
        }
    }
}
