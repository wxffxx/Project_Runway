namespace PP_RY.Core.Navigation
{
    public enum NodeType
    {
        RunwayCenterline, // 跑道中轴线节点
        RunwayThreshold,  // 跑道端点 (起降打卡点)
        RunwayEnd,        // 跑道尽头
        TaxiwayPoint,     // 普通滑行道节点
        HoldShortNode     // 跑道外等待点 (Hold Short Line)
    }

    public enum EdgeType
    {
        RunwaySegment,    // 位于跑道上的直行路段
        HighSpeedExit,    // 快速脱离道 (High Speed Taxiway)
        StandardTaxiway   // 标准滑行道路段
    }
}
