## road_data.gd
## 陆侧车辆半寻路体系 - 纯样条线/航点数据结构
## 迁移自: RoadData.cs
class_name RoadData

var id: String
var waypoints: PackedVector3Array = PackedVector3Array()
var total_length: float = 0.0


func _init(road_id: String) -> void:
	id = road_id
	waypoints = PackedVector3Array()


## 添加路标点并自动累加总长度
func add_waypoint(wp: Vector3) -> void:
	if waypoints.size() > 0:
		total_length += waypoints[waypoints.size() - 1].distance_to(wp)
	waypoints.append(wp)
