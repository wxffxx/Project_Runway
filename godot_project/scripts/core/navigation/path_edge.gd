## path_edge.gd
## 图论网络中的边 (连接两个 PathNode)
## 迁移自: PathEdge.cs
class_name PathEdge

var from_node: PathNode  ## 起点
var to_node: PathNode    ## 终点
var distance: float      ## 距离 (寻路权重 Cost)
var type: int            ## EdgeType 枚举值
var is_one_way: bool     ## 是否为单向通道


func _init(from: PathNode, to: PathNode, dist: float, edge_type: int, one_way: bool) -> void:
	from_node = from
	to_node = to
	distance = dist
	type = edge_type
	is_one_way = one_way
