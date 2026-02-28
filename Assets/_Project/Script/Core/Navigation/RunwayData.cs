using System.Collections.Generic;

namespace PP_RY.Core.Navigation
{
    [System.Serializable]
    public class RunwayData
    {
        public string runwayName;      // 例如 "01L" 或 "19R"
        public float length;           // 整条跑道的真实长度 (m)
        public float width;            // 宽度 (m)

        public PathNode thresholdNode; // 起点接地点
        public PathNode endNode;       // 跑道尽头

        // 包含所有 100m 间隔生成的核心节点，按照从起飞到滑跑的顺序排列
        public List<PathNode> centerlineNodes = new List<PathNode>();

        public RunwayData(string name, float length, float width)
        {
            this.runwayName = name;
            this.length = length;
            this.width = width;
            this.centerlineNodes = new List<PathNode>();
        }
    }
}
