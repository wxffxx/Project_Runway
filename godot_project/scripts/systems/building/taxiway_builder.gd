## taxiway_builder.gd
## 滑行道建造器 - 支持双层模型 (核心道面 + 保护性道肩)
## 迁移自: TaxiwayBuilder.cs (276 行)
extends BaseBuilder

@export_group("Materials")
@export var ghost_core_material: StandardMaterial3D
@export var ghost_shoulder_material: StandardMaterial3D
@export var placed_core_material: StandardMaterial3D
@export var placed_shoulder_material: StandardMaterial3D

static var instance: BaseBuilder = null

var _current_core_width: float = 0.0
var _current_total_width: float = 0.0
var _ghost_core: MeshInstance3D = null
var _ghost_shoulder: MeshInstance3D = null


func _ready() -> void:
	if instance == null:
		instance = self
	else:
		queue_free()


func start_building_taxiway(core_width: float, total_width: float, category_name: String) -> void:
	_current_core_width = core_width
	_current_total_width = total_width
	current_category_name = category_name
	current_state = BuildState.PLACING_START
	tooltip = "建造滑行道 (Code %s)：请点击海平面锚定起点。按 ESC 取消。" % category_name
	tooltip_changed.emit(tooltip)
	_clear_ghosts()


func _handle_placing_start() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos != null and _is_left_just_clicked():
		start_point = hit_pos
		current_state = BuildState.PLACING_END
		tooltip = "请拖动鼠标确定长度与角度 (按住 Shift 吸附 45°)。再次点击左键完成。"
		tooltip_changed.emit(tooltip)

		_ghost_core = _create_box_mesh()
		_ghost_core.name = "Ghost_Taxiway_Core"
		if ghost_core_material:
			_ghost_core.material_override = ghost_core_material
		add_child(_ghost_core)

		if _current_total_width > _current_core_width:
			_ghost_shoulder = _create_box_mesh()
			_ghost_shoulder.name = "Ghost_Taxiway_Shoulder"
			if ghost_shoulder_material:
				_ghost_shoulder.material_override = ghost_shoulder_material
			add_child(_ghost_shoulder)


func _handle_placing_end() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos == null:
		return

	var end_pos: Vector3 = hit_pos

	if Input.is_key_pressed(KEY_SHIFT):
		end_pos = _snap_to_angle_and_distance(start_point, end_pos)

	var direction := end_pos - start_point
	var total_length := direction.length()
	var visual_core_length := total_length + _current_core_width
	var visual_shoulder_length := total_length + _current_total_width

	if _ghost_core:
		_update_transform(_ghost_core, start_point, end_pos, _current_core_width, 0.08, visual_core_length)
	if _ghost_shoulder:
		_update_transform(_ghost_shoulder, start_point, end_pos, _current_total_width, 0.04, visual_shoulder_length)

	# 浮动提示
	_draw_taxiway_tooltip(end_pos)

	if _is_left_just_clicked():
		var length := start_point.distance_to(end_pos)
		if length >= 5.0:
			_finalize_taxiway(start_point, end_pos)
		else:
			tooltip = "距离太近了，往远拖一点！"
			tooltip_changed.emit(tooltip)


func _snap_to_angle_and_distance(start: Vector3, end: Vector3) -> Vector3:
	var direction := end - start
	var distance := direction.length()
	if distance < 0.1:
		return end

	var angle := rad_to_deg(atan2(direction.x, direction.z))
	var snapped_angle := roundf(angle / 45.0) * 45.0

	var snapped_distance := roundf(distance / 100.0) * 100.0
	if snapped_distance < 100.0:
		snapped_distance = 100.0

	var snapped_dir := Vector3(0, 0, 1).rotated(Vector3.UP, deg_to_rad(snapped_angle))
	var new_end := start + snapped_dir * snapped_distance
	new_end.x = roundf(new_end.x)
	new_end.z = roundf(new_end.z)
	return new_end


func _update_transform(mesh: MeshInstance3D, p1: Vector3, p2: Vector3, width: float, y_offset: float, visual_length: float) -> void:
	if mesh == null:
		return
	var midpoint := (p1 + p2) / 2.0
	midpoint.y = y_offset
	mesh.position = midpoint

	# 直接设置 BoxMesh 尺寸
	var box: BoxMesh = mesh.mesh as BoxMesh
	if box:
		box.size = Vector3(width, 0.1, visual_length)
	else:
		mesh.scale = Vector3(width, 0.1, visual_length)

	# 用 atan2 精确旋转
	var flat_dir := Vector3(p2.x - p1.x, 0.0, p2.z - p1.z)
	if flat_dir.length() > 0.001:
		mesh.rotation = Vector3(0.0, atan2(flat_dir.x, flat_dir.z), 0.0)


func _finalize_taxiway(p1: Vector3, p2: Vector3) -> void:
	var direction := p2 - p1
	var total_length := direction.length()
	var visual_core_length := total_length + _current_core_width
	var visual_shoulder_length := total_length + _current_total_width

	# 道面实体 — 先加入场景树再设 transform
	var final_core := _create_box_mesh()
	final_core.name = "Taxiway_Core_Code%s" % current_category_name
	get_tree().current_scene.add_child(final_core)
	_update_transform(final_core, p1, p2, _current_core_width, 0.08, visual_core_length)
	if placed_core_material:
		final_core.material_override = placed_core_material

	# 道肩实体
	if _current_total_width > _current_core_width:
		var final_shoulder := _create_box_mesh()
		final_shoulder.name = "Taxiway_Shoulder_Code%s" % current_category_name
		get_tree().current_scene.add_child(final_shoulder)
		_update_transform(final_shoulder, p1, p2, _current_total_width, 0.04, visual_shoulder_length)
		if placed_shoulder_material:
			final_shoulder.material_override = placed_shoulder_material

	# 图论网络
	_generate_taxiway_network(p1, p2)

	_clear_ghosts()
	_exit_build_mode()
	tooltip = "滑行道组件 (Code %s) 建造完成！" % current_category_name
	tooltip_changed.emit(tooltip)
	build_completed.emit()


func _generate_taxiway_network(start: Vector3, end: Vector3) -> void:
	var start_node := PathNode.new(
		"TWY_%s_Start" % current_category_name, start,
		NavigationEnums.NodeType.TAXIWAY_POINT
	)
	var end_node := PathNode.new(
		"TWY_%s_End" % current_category_name, end,
		NavigationEnums.NodeType.TAXIWAY_POINT
	)

	var dist := start.distance_to(end)
	start_node.add_edge(end_node, dist, NavigationEnums.EdgeType.STANDARD_TAXIWAY, false)

	var twy_id := "TWY_%s_%s" % [current_category_name, str(randi()).substr(0, 5)]
	var twy_data := RunwayData.new(twy_id, dist, _current_core_width)
	twy_data.threshold_node = start_node
	twy_data.end_node = end_node
	twy_data.centerline_nodes.append(start_node)
	twy_data.centerline_nodes.append(end_node)

	if RunwayNetworkManager:
		RunwayNetworkManager.register_runway(twy_data)


func cancel_build() -> void:
	_clear_ghosts()
	super.cancel_build()
	tooltip = "已取消建造滑行道。"
	tooltip_changed.emit(tooltip)


func _clear_ghosts() -> void:
	if _ghost_core:
		_ghost_core.queue_free()
		_ghost_core = null
	if _ghost_shoulder:
		_ghost_shoulder.queue_free()
		_ghost_shoulder = null


func _draw_taxiway_tooltip(end_pos: Vector3) -> void:
	if floating_label == null:
		return
	var length := start_point.distance_to(end_pos)
	var shoulder_hint := "\n(+巨幅道肩)" if _current_total_width > _current_core_width else ""
	floating_label.text = "Code %s Taxiway%s\n%dm" % [current_category_name, shoulder_hint, int(length)]
	floating_label.visible = true
	floating_label.position = get_viewport().get_mouse_position() + Vector2(20, 20)


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
