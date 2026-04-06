## road_vehicle_spawner.gd
## 陆侧车辆管理器 - 基于路标序列的纯数学位移
## 迁移自: RoadVehicleSpawner.cs + RoadVehicle 内部类
extends Node3D

@export var car_scene: PackedScene  ## 预制体车辆场景
@export var spawn_interval: float = 3.0
@export var vehicle_speed: float = 15.0

static var instance: Node3D = null

# ⚠️ BOTTLENECK: 所有活跃车辆存在一个数组中。
# 未来实现防追尾检测时，需要对每辆车和其他车做距离比较 → O(N²)。
# 当车辆超过 100 辆时，这会成为严重性能瓶颈。
# 👉 优化建议: 使用 Area3D 物理感知区域检测前车，或用空间分区避免全量遍历。
var _active_vehicles: Array[Node3D] = []
var _spawn_timer: float = 0.0


func _ready() -> void:
	if instance == null:
		instance = self
	else:
		queue_free()


func _process(delta: float) -> void:
	_spawn_timer += delta
	if _spawn_timer >= spawn_interval:
		_spawn_timer = 0.0
		_try_spawn_vehicle()


func _try_spawn_vehicle() -> void:
	# 获取 RoadBuilder 的已建造道路
	# 注意: 在 Godot 中需要通过 get_node 或信号获取 RoadBuilder
	var road_builder = _find_road_builder()
	if road_builder == null or road_builder.all_built_roads.size() == 0:
		return

	var random_road: RoadData = road_builder.all_built_roads[randi() % road_builder.all_built_roads.size()]
	if random_road.waypoints.size() >= 2:
		_spawn_vehicle(random_road)


func _spawn_vehicle(target_road: RoadData) -> void:
	var car_obj: Node3D
	if car_scene:
		car_obj = car_scene.instantiate()
	else:
		# 临时方块代替
		car_obj = MeshInstance3D.new()
		var box := BoxMesh.new()
		box.size = Vector3(2.0, 1.5, 4.0)
		(car_obj as MeshInstance3D).mesh = box
		var mat := StandardMaterial3D.new()
		mat.albedo_color = Color(randf(), randf(), randf())
		(car_obj as MeshInstance3D).material_override = mat

	car_obj.global_position = target_road.waypoints[0]
	car_obj.name = "RoadVehicle_%s" % str(randi()).substr(0, 4)
	get_tree().current_scene.add_child(car_obj)

	# 附加车辆逻辑脚本
	var vehicle_script := RoadVehicle.new()
	vehicle_script.initialize(target_road, vehicle_speed, self)
	car_obj.add_child(vehicle_script)
	_active_vehicles.append(car_obj)


func remove_vehicle(vehicle_node: Node3D) -> void:
	_active_vehicles.erase(vehicle_node)
	vehicle_node.queue_free()


func _find_road_builder():
	# 在场景树中查找 RoadBuilder 实例
	var nodes := get_tree().get_nodes_in_group("road_builder")
	if nodes.size() > 0:
		return nodes[0]
	return null
