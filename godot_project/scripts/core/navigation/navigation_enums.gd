## navigation_enums.gd
## 导航系统的所有枚举定义
## 迁移自: NavigationEnums.cs
class_name NavigationEnums

enum NodeType {
	RUNWAY_CENTERLINE, ## 跑道中轴线节点
	RUNWAY_THRESHOLD,  ## 跑道端点 (起降打卡点)
	RUNWAY_END,        ## 跑道尽头
	TAXIWAY_POINT,     ## 普通滑行道节点
	HOLD_SHORT_NODE,   ## 跑道外等待点 (Hold Short Line)
	GATE_NODE          ## 登机口/停机位节点
}

enum EdgeType {
	RUNWAY_SEGMENT,    ## 位于跑道上的直行路段
	HIGH_SPEED_EXIT,   ## 快速脱离道 (High Speed Taxiway)
	STANDARD_TAXIWAY,  ## 标准滑行道路段
	PUSHBACK_PATH      ## 推出路段 (需要牵引车倒车)
}

enum RunwayStatus {
	FREE,              ## 跑道空闲，允许穿越或起降
	LANDING,           ## 跑道正被用于降落，禁止进入
	TAKING_OFF,        ## 跑道正被用于起飞，禁止进入
	CLOSED             ## 跑道关闭（维修等例外情况）
}

enum GateSize {
	SMALL,  ## 适合 A, B 类 (通用航空或小型支线客机)
	MEDIUM, ## 适合 C 类 (A320, B737)
	LARGE,  ## 适合 D, E 类 (A330, B777, B787)
	HEAVY   ## 适合 F 类 (A380, B747-8)
}

enum ICAORunwayCategory {
	NONE, A, B, C, D, E, F
}

enum ICAOTaxiwayCategory {
	A, B, C, D, E, F
}
