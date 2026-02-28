namespace PP_RY.Core.Navigation
{
    [System.Serializable]
    public class PathEdge
    {
        public PathNode fromNode;      // 起点
        public PathNode toNode;        // 终点
        public float distance;         // 距离 (寻路权重 Cost)
        public EdgeType type;          // 道路类型
        public bool isOneWay;          // 是否为单向通道

        public PathEdge(PathNode from, PathNode to, float distance, EdgeType type, bool isOneWay)
        {
            this.fromNode = from;
            this.toNode = to;
            this.distance = distance;
            this.type = type;
            this.isOneWay = isOneWay;
        }
    }
}
