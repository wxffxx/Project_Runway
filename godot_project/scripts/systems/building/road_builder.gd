## road_builder.gd
## 陆侧公路建造器 - 连续航点折线模式
## 迁移自: RoadBuilder.cs (257 行)
extends BaseBuilder

@export_group("Materials")
@export var ghost_road_material: StandardMaterial3D
@export var placed_road_material: StandardMaterial3D
@export var road_width: float = 8.0

static var instance: BaseBuilder = null

var _current_waypoints: PackedVector3Array = PackedVector3Array()
var _ghost_segments: Array[MeshInstance3D] = []
var all_built_roads: Array[RoadData] = []


func _ready() -> void:
	if instance == null:
		instance = self
	else:
		queue_free()


func start_building_road() -> void:
	current_state = BuildState.PLACING_START
	tooltip = "修建高架路：点击第一下确定公路起点。"
	tooltip_changed.emit(tooltip)
	_clear_ghosts()
	_current_waypoints = PackedVector3Array()


func _handle_placing_start() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos != null and _is_left_just_clicked():
		var start: Vector3 = hit_pos
		start.x = roundf(start.x)
		start.z = roundf(start.z)
		_current_waypoints.append(start)
		start_point = start
		current_state = BuildState.PLACING_END
		tooltip = "移动鼠标拉出路线。再次【左键】打下新节点。【右键】确认并修路。"
		tooltip_changed.emit(tooltip)
		_create_ghost_segment()


func _handle_placing_end() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos == null:
		return

	var current_pos: Vector3 = hit_pos
	if Input.is_key_pressed(KEY_SHIFT):
		current_pos = _snap_to_angle(_current_waypoints[_current_waypoints.size() - 1], current_pos)

	# 更新最后一段幽灵预览
	if _ghost_segments.size() > 0:
		var last_ghost := _ghost_segments[_ghost_segments.size() - 1]
		_update_segment_transform(last_ghost, _current_waypoints[_current_waypoints.size() - 1], current_pos)

	if _is_left_just_clicked():
		if _current_waypoints[_current_waypoints.size() - 1].distance_to(current_pos) > 1.0:
			_current_waypoints.append(current_pos)
			_create_ghost_segment()

	# ⚠️ BOTTLENECK: Godot 不支持在 _process 中直接检测 "刚刚按下右键"。
	# Input.is_action_just_pressed 只能用于 project.godot 中定义的 action。
	# 这里用帧计数手动模拟 "just pressed" 状态。
	if _is_right_just_clicked():
		if _current_waypoints.size() > 1:
			_finalize_road()
		else:
			cancel_build()


func _create_ghost_segment() -> void:
	var mi := MeshInstance3D.new()
	mi.mesh = BoxMesh.new()
	mi.name = "Ghost_Road_Segment_%d" % _ghost_segments.size()
	if ghost_road_material:
		mi.material_override = ghost_road_material
	add_child(mi)
	_ghost_segments.append(mi)


func _update_segment_transform(mesh: MeshInstance3D, p1: Vector3, p2: Vector3) -> void:
	if mesh == null:
		return
	var diff := p2 - p1
	var dist := diff.length()
	mesh.global_position = (p1 + p2) / 2.0 + Vector3(0, 0.05, 0)
	mesh.scale = Vector3(road_width, 0.1, dist)
	if dist > 0.01:
		var flat_dir := Vector3(diff.x, 0.0, diff.z).normalized()
		mesh.look_at(mesh.global_position + flat_dir, Vector3.UP)


func _finalize_road() -> void:
	var new_road := RoadData.new("Road_%s" % str(randi()).substr(0, 5))

	for i in range(_current_waypoints.size()):
		new_road.add_waypoint(_current_waypoints[i])
		if i < _current_waypoints.size() - 1:
			var final_seg := MeshInstance3D.new()
			final_seg.mesh = BoxMesh.new()
			final_seg.name = "Placed_Road_Seg_%d" % i
			_update_segment_transform(final_seg, _current_waypoints[i], _current_waypoints[i + 1])
			if placed_road_material:
				final_seg.material_override = placed_road_material
			get_tree().current_scene.add_child(final_seg)

	all_built_roads.append(new_road)
	print("[半寻路网络] 已注册新道路: %s, 全长: %.1fm, 包含 %d 个主航点。" % [new_road.id, new_road.total_length, new_road.waypoints.size()])

	_clear_ghosts()
	_current_waypoints = PackedVector3Array()
	_exit_build_mode()
	tooltip = "陆侧路网建造完成！"
	tooltip_changed.emit(tooltip)
	build_completed.emit()


func _snap_to_angle(start: Vector3, end: Vector3) -> Vector3:
	var direction := end - start
	var distance := direction.length()
	if distance < 0.1:
		return end
	var angle := rad_to_deg(atan2(direction.x, direction.z))
	var snapped_angle := roundf(angle / 45.0) * 45.0
	var snapped_dir := Vector3(0, 0, 1).rotated(Vector3.UP, deg_to_rad(snapped_angle))
	return start + snapped_dir * distance


func _clear_ghosts() -> void:
	for g in _ghost_segments:
		if g != null and is_instance_valid(g):
			g.queue_free()
	_ghost_segments.clear()


func cancel_build() -> void:
	_clear_ghosts()
	_current_waypoints = PackedVector3Array()
	super.cancel_build()
	tooltip = "已取消修路。"
	tooltip_changed.emit(tooltip)


# 覆写 _process 以拦截右键 (右键在路网建设中是"确认")
func _process(delta: float) -> void:
	match current_state:
		BuildState.PLACING_START:
			_handle_placing_start()
		BuildState.PLACING_END:
			_handle_placing_end()

	if Input.is_action_just_pressed("pause"):
		cancel_build()


var _last_left_frame: int = -1
var _last_right_frame: int = -1

func _is_left_just_clicked() -> bool:
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		var f := Engine.get_process_frames()
		if f != _last_left_frame:
			_last_left_frame = f
			return true
	return false

func _is_right_just_clicked() -> bool:
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		var f := Engine.get_process_frames()
		if f != _last_right_frame:
			_last_right_frame = f
			return true
	return false
