## road_vehicle.gd
## 单辆车辆的纯插值运动逻辑 (作为子节点附加到车辆 Node3D 上)
## 迁移自: RoadVehicle (RoadVehicleSpawner.cs 内部类)
extends Node
class_name RoadVehicle

var _assigned_road: RoadData
var _current_waypoint_index: int = 0
var _speed: float = 10.0
var _spawner: Node3D = null


func initialize(road: RoadData, speed: float, spawner: Node3D) -> void:
	_assigned_road = road
	_speed = speed
	_spawner = spawner
	_current_waypoint_index = 0

	if road.waypoints.size() > 1:
		var parent := get_parent() as Node3D
		if parent:
			var dir := road.waypoints[1] - road.waypoints[0]
			if dir.length() > 0.01:
				parent.look_at(parent.global_position + dir, Vector3.UP)


func _process(delta: float) -> void:
	if _assigned_road == null or _current_waypoint_index >= _assigned_road.waypoints.size():
		return

	var parent := get_parent() as Node3D
	if parent == null:
		return

	var target_point: Vector3 = _assigned_road.waypoints[_current_waypoint_index]

	# 纯数学移动
	parent.global_position = parent.global_position.move_toward(target_point, _speed * delta)

	# 转向
	var direction := target_point - parent.global_position
	if direction.length_squared() > 0.05:
		var look_rot := Transform3D.IDENTITY.looking_at(direction, Vector3.UP)
		parent.global_transform.basis = parent.global_transform.basis.slerp(look_rot.basis, delta * 5.0)

	# 到达检测
	if parent.global_position.distance_to(target_point) < 0.5:
		_current_waypoint_index += 1

		if _current_waypoint_index >= _assigned_road.waypoints.size():
			print("[%s] 到达公路终点，车辆销毁。" % parent.name)
			if _spawner and _spawner.has_method("remove_vehicle"):
				_spawner.remove_vehicle(parent)
			else:
				parent.queue_free()
