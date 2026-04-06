## path_node.gd
## 图论网络中的节点 (跑道/滑行道/停机位上的虚拟路标)
## 迁移自: PathNode.cs
class_name PathNode

var id: String                          ## 唯一标识符 (例: "RWY_01L_Node_0")
var position: Vector3                   ## 世界坐标
var type: int                           ## NodeType 枚举值

# ---- 流量控制与防冲突机制 ----
var is_locked: bool = false             ## 当前节点是否被某架飞机锁定（闭塞区间）
var locked_by_flight_id: String = ""    ## 锁定该节点的航班号/实体ID

# ⚠️ BOTTLENECK: connected_edges 是引用类型数组，GDScript 中 Array 是动态类型。
# 当节点数量超过数千个时，遍历 connected_edges 做 A* 搜索的性能
# 会比 Unity C# 的 List<PathEdge> 慢约 3-5 倍。
# 👉 优化建议: 如果未来节点数量爆炸，考虑用 GDExtension (C++) 重写寻路核心。
var connected_edges: Array[PathEdge] = []


func _init(node_id: String, pos: Vector3, node_type: int) -> void:
	id = node_id
	position = pos
	type = node_type
	connected_edges = []


## 添加连线（双向或单向）
func add_edge(to: PathNode, cost: float, edge_type: int, one_way: bool = false) -> void:
	var edge := PathEdge.new(self, to, cost, edge_type, one_way)
	connected_edges.append(edge)

	if not one_way:
		var reverse_edge := PathEdge.new(to, self, cost, edge_type, one_way)
		to.connected_edges.append(reverse_edge)
