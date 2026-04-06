## runway_data.gd
## 单条跑道的完整数据结构
## 迁移自: RunwayData.cs
class_name RunwayData

var runway_name: String           ## 例如 "01L" 或 "19R"
var length: float                 ## 整条跑道的真实长度 (m)
var width: float                  ## 宽度 (m)

var threshold_node: PathNode      ## 起点接地点
var end_node: PathNode            ## 跑道尽头

# ---- 流量控制 (Traffic Control) ----
var current_status: int = NavigationEnums.RunwayStatus.FREE
var active_flight_id: String = ""

# ⚠️ BOTTLENECK: 当跑道长度超过 3000m 时，centerline_nodes 将包含 30+ 个节点。
# 每次滑行道试图吸附到最近的跑道节点时需要遍历此数组。
# 👉 优化建议: 如果跑道数量超过 20 条，考虑构建空间索引 (如四叉树) 加速最近邻查找。
var centerline_nodes: Array[PathNode] = []


func _init(name: String, rwy_length: float, rwy_width: float) -> void:
	runway_name = name
	length = rwy_length
	width = rwy_width
	centerline_nodes = []
