## runway_builder.gd
## 跑道建造器 - 两次点击确定起终点，生成跑道白模和图论节点网络
## 迁移自: RunwayBuilder.cs (356 行)
extends BaseBuilder

@export_group("Runway Settings")
@export var runway_width: float = 15.0
@export var runway_thickness: float = 0.5
@export var min_length: float = 5.0

@export_group("Visuals")
@export var ghost_material: StandardMaterial3D
@export var placed_material: StandardMaterial3D

var _current_ghost_runway: MeshInstance3D = null

static var instance: BaseBuilder = null


func _ready() -> void:
	if instance == null:
		instance = self
	else:
		queue_free()


## 给 UI 按钮调用的公开方法
func start_building_runway(width: float, category_name: String) -> void:
	runway_width = width
	current_category_name = category_name
	current_state = BuildState.PLACING_START
	tooltip = "建造跑道 (宽%dm)：请点击海平面锚定【起点】。按 ESC 取消。" % int(width)
	tooltip_changed.emit(tooltip)

	if _current_ghost_runway != null:
		_current_ghost_runway.queue_free()
		_current_ghost_runway = null


func _handle_placing_start() -> void:
	# 右键取消
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		cancel_build()
		return

	if Input.is_action_just_pressed("ui_accept") or \
	   (Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT) and _is_just_clicked()):
		var hit_pos = get_mouse_ground_position()
		if hit_pos != null:
			start_point = hit_pos
			current_state = BuildState.PLACING_END
			tooltip = "请移动鼠标拉伸跑道长度与角度，再次点击放置【终点】。"
			tooltip_changed.emit(tooltip)

			# 创建幽灵白模
			_current_ghost_runway = _create_box_mesh()
			_current_ghost_runway.name = "GhostRunway"
			if ghost_material:
				_current_ghost_runway.material_override = ghost_material
			add_child(_current_ghost_runway)


func _handle_placing_end() -> void:
	# 右键取消
	if _is_right_just_clicked():
		cancel_build()
		return

	var hit_pos = get_mouse_ground_position()
	if hit_pos == null:
		return

	var current_mouse_pos: Vector3 = hit_pos

	# Shift 键 45 度吸附
	if Input.is_key_pressed(KEY_SHIFT):
		current_mouse_pos = _snap_to_45_degrees(start_point, current_mouse_pos)

	# 实时更新幽灵白模
	var direction := current_mouse_pos - start_point
	var total_length := direction.length()
	var visual_total_length := total_length + runway_width
	_update_runway_transform(_current_ghost_runway, start_point, current_mouse_pos, visual_total_length)

	# 更新浮动提示
	_draw_floating_tooltip_runway(current_mouse_pos)

	# 确认建造
	if _is_left_just_clicked():
		var length := start_point.distance_to(current_mouse_pos)
		if length >= min_length:
			_finalize_runway(start_point, current_mouse_pos)
		else:
			tooltip = "跑道太短了，请拉长一点！"
			tooltip_changed.emit(tooltip)


func _snap_to_45_degrees(start: Vector3, end: Vector3) -> Vector3:
	var direction := end - start
	var distance := direction.length()
	if distance < 0.1:
		return end

	# 角度吸附
	var angle := atan2(direction.x, direction.z)
	angle = rad_to_deg(angle)
	var snapped_angle := roundf(angle / 45.0) * 45.0

	# ICAO 长度吸附
	var standard_lengths := [800.0, 1200.0, 1800.0, 2400.0]
	var snapped_distance := distance
	var min_diff := INF
	for std_len in standard_lengths:
		var diff := absf(distance - std_len)
		if diff < min_diff:
			min_diff = diff
			snapped_distance = std_len

	if distance > 3000.0:
		snapped_distance = roundf(distance / 500.0) * 500.0

	var snapped_dir := Vector3(0, 0, 1).rotated(Vector3.UP, deg_to_rad(snapped_angle))
	var new_end := start + snapped_dir * snapped_distance
	new_end.x = roundf(new_end.x)
	new_end.z = roundf(new_end.z)
	return new_end


func _update_runway_transform(mesh: MeshInstance3D, p1: Vector3, p2: Vector3, visual_length: float) -> void:
	if mesh == null:
		return
	var direction := p2 - p1
	var node_distance := direction.length()
	if node_distance < 0.1:
		return

	# 位置: 中心点
	var center := p1 + direction * 0.5
	center.y = runway_thickness * 0.5
	mesh.position = center

	# 直接设置 BoxMesh 尺寸，避免 scale + look_at 交互异常
	var box: BoxMesh = mesh.mesh as BoxMesh
	if box:
		box.size = Vector3(runway_width, runway_thickness, visual_length)
	else:
		mesh.scale = Vector3(runway_width, runway_thickness, visual_length)

	# 旋转: 用 atan2 精确计算 Y 轴旋转角，避免 look_at 的 -Z 约定问题
	var flat_dir := Vector3(direction.x, 0.0, direction.z)
	if flat_dir.length() > 0.01:
		mesh.rotation = Vector3(0.0, atan2(flat_dir.x, flat_dir.z), 0.0)


# ⚠️ BOTTLENECK: 每次放置跑道时会创建 (length/100) 个 PathNode 并互相连接。
# 对于超长跑道 (4000m+) 会产生 40+ 个节点和边的实例化。
# GDScript 中 new() 调用比 C# 慢。如果批量建造大量跑道，此处是热点。
# 👉 优化建议: 批处理创建，或考虑用 Resource 子类替代纯 RefCounted。
func _finalize_runway(p1: Vector3, p2: Vector3) -> void:
	# 创建实体跑道 — 先加入场景树再设置 transform
	var final_runway := _create_box_mesh()
	final_runway.name = "Runway_New"
	get_tree().current_scene.add_child(final_runway)

	var direction := p2 - p1
	var total_length := direction.length()
	var visual_total_length := total_length + runway_width
	_update_runway_transform(final_runway, p1, p2, visual_total_length)
	if placed_material:
		final_runway.material_override = placed_material

	# 生成图论网络
	_generate_runway_network(p1, p2, runway_width)

	# 销毁幽灵
	if _current_ghost_runway != null:
		_current_ghost_runway.queue_free()
		_current_ghost_runway = null

	_exit_build_mode()
	tooltip = "跑道建造完成！网络节点已生成。"
	tooltip_changed.emit(tooltip)
	build_completed.emit()


func _generate_runway_network(start: Vector3, end: Vector3, width: float) -> void:
	var direction := end - start
	var total_length := direction.length()
	var normalized_dir := direction.normalized()

	var rwy_data := RunwayData.new("RWY_TEMP", total_length, width)

	var num_intervals := int(total_length / 100.0)
	var previous_node: PathNode = null

	for i in range(num_intervals + 1):
		var current_dist := float(i) * 100.0
		var node_pos := start + normalized_dir * current_dist

		var type: int = NavigationEnums.NodeType.RUNWAY_CENTERLINE
		if i == 0:
			type = NavigationEnums.NodeType.RUNWAY_THRESHOLD

		var current_node := PathNode.new("Node_100m_%d" % i, node_pos, type)
		rwy_data.centerline_nodes.append(current_node)

		if i == 0:
			rwy_data.threshold_node = current_node

		if previous_node != null:
			var dist := previous_node.position.distance_to(current_node.position)
			previous_node.add_edge(current_node, dist, NavigationEnums.EdgeType.RUNWAY_SEGMENT, false)

		previous_node = current_node

	# 处理尾巴
	var remainder := total_length - (num_intervals * 100.0)
	if remainder > 1.0:
		var final_node := PathNode.new("Node_End", end, NavigationEnums.NodeType.RUNWAY_END)
		rwy_data.centerline_nodes.append(final_node)
		rwy_data.end_node = final_node
		var dist := previous_node.position.distance_to(final_node.position)
		previous_node.add_edge(final_node, dist, NavigationEnums.EdgeType.RUNWAY_SEGMENT, false)
	else:
		if previous_node != null:
			previous_node.type = NavigationEnums.NodeType.RUNWAY_END
			rwy_data.end_node = previous_node

	# 注册到全局管理器
	if RunwayNetworkManager:
		RunwayNetworkManager.register_runway(rwy_data)
	else:
		push_warning("未找到 RunwayNetworkManager Autoload！")


func cancel_build() -> void:
	if _current_ghost_runway != null:
		_current_ghost_runway.queue_free()
		_current_ghost_runway = null
	super.cancel_build()


func _draw_floating_tooltip_runway(end_pos: Vector3) -> void:
	if floating_label == null:
		return
	var length := start_point.distance_to(end_pos)
	var length_category := _get_icao_length_number(length)
	floating_label.text = "%d%s\n%dm" % [length_category, current_category_name, int(length)]
	floating_label.visible = true
	# 跟随鼠标
	floating_label.position = get_viewport().get_mouse_position() + Vector2(20, 20)


func _get_icao_length_number(length: float) -> int:
	if length < 800.0:
		return 1
	if length < 1200.0:
		return 2
	if length < 1800.0:
		return 3
	return 4


# ---- 工具函数 ----

func _create_box_mesh() -> MeshInstance3D:
	var mesh_instance := MeshInstance3D.new()
	mesh_instance.mesh = BoxMesh.new()
	return mesh_instance


var _last_left_click_frame: int = -1
var _last_right_click_frame: int = -1

func _is_left_just_clicked() -> bool:
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		var frame := Engine.get_process_frames()
		if frame != _last_left_click_frame:
			_last_left_click_frame = frame
			return true
	return false

func _is_right_just_clicked() -> bool:
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		var frame := Engine.get_process_frames()
		if frame != _last_right_click_frame:
			_last_right_click_frame = frame
			return true
	return false

func _is_just_clicked() -> bool:
	return _is_left_just_clicked()
