## gate_builder.gd
## 停机位建造器 - 两次点击确定位置和推出路线
## 迁移自: GateBuilder.cs (282 行)
extends BaseBuilder

@export_group("Materials")
@export var ghost_gate_material: StandardMaterial3D
@export var ghost_pushback_material: StandardMaterial3D
@export var placed_gate_material: StandardMaterial3D

@export_group("Gate Configuration")
@export var current_gate_size: int = NavigationEnums.GateSize.MEDIUM
## 推出路线终点自动吸附到周围滑行道节点的最大距离
@export var snap_radius: float = 30.0

var current_floor_layer: int = 0
const FLOOR_HEIGHT: float = 6.0

static var instance: BaseBuilder = null

var _ghost_gate: MeshInstance3D = null
var _ghost_pushback_line: MeshInstance3D = null
var _gate_node_pos: Vector3 = Vector3.ZERO
var _snapped_pushback_node: PathNode = null


func _ready() -> void:
	if instance == null:
		instance = self
	else:
		queue_free()


func start_building_gate(size: int) -> void:
	current_gate_size = size
	current_state = BuildState.PLACING_START
	_update_floor_tooltip()
	_clear_ghosts()
	_snapped_pushback_node = null


func _process(delta: float) -> void:
	super._process(delta)

	if current_state != BuildState.IDLE:
		if Input.is_action_just_pressed("floor_up"):
			current_floor_layer += 1
			_update_floor_tooltip()
		elif Input.is_action_just_pressed("floor_down"):
			if current_floor_layer > 0:
				current_floor_layer -= 1
			_update_floor_tooltip()


func _update_floor_tooltip() -> void:
	if current_state == BuildState.PLACING_START:
		tooltip = "建造停机位 (楼层: %dF) 高度 %dm：\n请紧贴航站楼层边缘点击放置廊桥。\n【PageUp/PageDown】切换接驳层。" % [current_floor_layer, current_floor_layer * FLOOR_HEIGHT]
	elif current_state == BuildState.PLACING_END:
		tooltip = "拖出飞机【推出路线】(Pushback Path)，靠近地表现有滑行道可自动垂向吸附。再次点击完成。"
	tooltip_changed.emit(tooltip)


func _handle_placing_start() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos == null:
		return

	if _ghost_gate == null:
		_ghost_gate = _create_box_mesh()
		_ghost_gate.name = "Ghost_Gate_Module"
		if ghost_gate_material:
			_ghost_gate.material_override = ghost_gate_material
		add_child(_ghost_gate)

	var gate_scale := Vector3(36.0, 0.2, 36.0)
	if current_gate_size == NavigationEnums.GateSize.LARGE or current_gate_size == NavigationEnums.GateSize.HEAVY:
		gate_scale = Vector3(60.0, 0.2, 60.0)

	var y_pos := 0.1 + (current_floor_layer * FLOOR_HEIGHT)
	_ghost_gate.global_position = Vector3(hit_pos.x, y_pos, hit_pos.z)
	_ghost_gate.scale = gate_scale

	if _is_left_just_clicked():
		_gate_node_pos = _ghost_gate.global_position
		start_point = _gate_node_pos
		current_state = BuildState.PLACING_END
		_update_floor_tooltip()

		_ghost_pushback_line = _create_box_mesh()
		_ghost_pushback_line.name = "Ghost_Pushback_Line"
		if ghost_pushback_material:
			_ghost_pushback_line.material_override = ghost_pushback_material
		add_child(_ghost_pushback_line)


func _handle_placing_end() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos == null:
		return

	var end_pos: Vector3 = hit_pos
	_snapped_pushback_node = null

	# ⚠️ BOTTLENECK: 吸附搜索 - 遍历所有已注册跑道/滑行道的所有节点。
	# 当网络节点数量超过 1000 个时，此 O(N) 搜索每帧执行可能导致帧率下降。
	# 👉 优化建议: 使用 KD-Tree 或 Godot 的 Area3D 感知区域替代暴力搜索。
	if RunwayNetworkManager and RunwayNetworkManager.all_runways.size() > 0:
		var min_distance := snap_radius
		var closest_node: PathNode = null

		for runway in RunwayNetworkManager.all_runways:
			for node in runway.centerline_nodes:
				if node.type == NavigationEnums.NodeType.TAXIWAY_POINT:
					var dist := end_pos.distance_to(node.position)
					if dist < min_distance:
						min_distance = dist
						closest_node = node

		if closest_node != null:
			end_pos = closest_node.position
			_snapped_pushback_node = closest_node

	# 更新推出路线预览
	_update_pushback_line(_ghost_pushback_line, _gate_node_pos, end_pos)

	# 朝向
	var push_dir := end_pos - _gate_node_pos
	if push_dir.length_squared() > 1.0 and _ghost_gate:
		_ghost_gate.look_at(_ghost_gate.global_position - push_dir, Vector3.UP)

	if _is_left_just_clicked():
		_finalize_gate(_gate_node_pos, end_pos, _snapped_pushback_node)


func _update_pushback_line(mesh: MeshInstance3D, p1: Vector3, p2: Vector3) -> void:
	if mesh == null:
		return
	var diff := p2 - p1
	var dist := diff.length()
	mesh.global_position = (p1 + p2) / 2.0 + Vector3(0, 0.15, 0)
	mesh.scale = Vector3(1.0, 0.1, dist)
	if dist > 0.01:
		var flat_dir := Vector3(diff.x, 0.0, diff.z).normalized()
		mesh.look_at(mesh.global_position + flat_dir, Vector3.UP)


func _finalize_gate(gate_pos: Vector3, pushback_end_pos: Vector3, snap_node: PathNode) -> void:
	# 视觉实体
	var final_gate := _create_box_mesh()
	final_gate.name = "Gate_%d_%s" % [current_gate_size, str(randi()).substr(0, 5)]
	final_gate.global_position = _ghost_gate.global_position
	final_gate.scale = _ghost_gate.scale
	final_gate.rotation = _ghost_gate.rotation
	if placed_gate_material:
		final_gate.material_override = placed_gate_material
	get_tree().current_scene.add_child(final_gate)

	# 数据结构
	var gate_name_str := "Gate %d" % randi_range(10, 99)
	var new_gate := GateData.new(gate_name_str, current_gate_size)
	var gate_node := PathNode.new(gate_name_str + "_Node", gate_pos, NavigationEnums.NodeType.GATE_NODE)
	new_gate.gate_node = gate_node

	if snap_node != null:
		new_gate.pushback_end_node = snap_node
	else:
		new_gate.pushback_end_node = PathNode.new(
			gate_name_str + "_PushEndpoint", pushback_end_pos,
			NavigationEnums.NodeType.TAXIWAY_POINT
		)

	var push_dist := gate_pos.distance_to(pushback_end_pos)
	new_gate.connect_pushback_route(push_dist)

	if RunwayNetworkManager:
		RunwayNetworkManager.register_gate(new_gate)

	_clear_ghosts()
	_exit_build_mode()
	var snap_info := "[已吸附并入滑路网]" if snap_node != null else "[未接入路网]"
	tooltip = "停机位 %s 建造完成！%s" % [gate_name_str, snap_info]
	tooltip_changed.emit(tooltip)
	build_completed.emit()


func cancel_build() -> void:
	_clear_ghosts()
	super.cancel_build()


func _clear_ghosts() -> void:
	if _ghost_gate:
		_ghost_gate.queue_free()
		_ghost_gate = null
	if _ghost_pushback_line:
		_ghost_pushback_line.queue_free()
		_ghost_pushback_line = null


func _create_box_mesh() -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.mesh = BoxMesh.new()
	return mi


var _last_left_frame: int = -1
func _is_left_just_clicked() -> bool:
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		var f := Engine.get_process_frames()
		if f != _last_left_frame:
			_last_left_frame = f
			return true
	return false
