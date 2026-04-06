## gate_data.gd
## 停机位/登机口数据结构
## 迁移自: GateData.cs
class_name GateData

var gate_name: String                      ## 例如 "Gate A1", "Stand 201"
var supported_size: int                    ## GateSize 枚举值

var gate_node: PathNode                    ## 机位中心点
var pushback_end_node: PathNode            ## 推出终点 (通常在滑行道上)


func _init(name: String, size: int) -> void:
	gate_name = name
	supported_size = size


## 在机位和推出终点之间建立双向连线
## 进港：滑行道 -> 机位 (StandardTaxiway)
## 出港：机位 -> 滑行道 (PushbackPath, 需要牵引车)
func connect_pushback_route(distance_cost: float) -> void:
	if gate_node != null and pushback_end_node != null:
		# 1. 进港路线
		pushback_end_node.add_edge(
			gate_node, distance_cost,
			NavigationEnums.EdgeType.STANDARD_TAXIWAY, true
		)
		# 2. 出港路线
		gate_node.add_edge(
			pushback_end_node, distance_cost,
			NavigationEnums.EdgeType.PUSHBACK_PATH, true
		)
