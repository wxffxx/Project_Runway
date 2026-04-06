## terminal_floor_builder.gd
## 航站楼地块建造器 - 拖拽矩形模式
## 迁移自: TerminalFloorBuilder.cs (194 行)
extends BaseBuilder

@export_group("Materials")
@export var ghost_floor_material: StandardMaterial3D
@export var placed_floor_material: StandardMaterial3D

static var instance: BaseBuilder = null

var current_floor_layer: int = 0
const FLOOR_HEIGHT: float = 6.0

var _ghost_floor: MeshInstance3D = null
var built_terminal_floors: Array[MeshInstance3D] = []


func _ready() -> void:
	if instance == null:
		instance = self
	else:
		queue_free()


func start_building_terminal_floor() -> void:
	current_state = BuildState.PLACING_START
	tooltip = "建造航站楼地块 (当前楼层: %dF)：\n点击并按住拖拽以绘制一个矩形。\n【PageUp/PageDown】切换楼层高度。" % current_floor_layer
	tooltip_changed.emit(tooltip)
	if _ghost_floor:
		_ghost_floor.queue_free()
		_ghost_floor = null


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
		tooltip = "建造航站楼地块 (当前楼层: %dF)：\n点击并按住拖拽以绘制一个矩形。\n【PageUp/PageDown】切换楼层高度。" % current_floor_layer
	elif current_state == BuildState.PLACING_END:
		tooltip = "拖动鼠标改变航站楼地块大小 (当前: %dF)。再次点击完成建设。" % current_floor_layer
	tooltip_changed.emit(tooltip)


func _handle_placing_start() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos != null and _is_left_just_clicked():
		start_point = hit_pos
		start_point.x = roundf(start_point.x / 5.0) * 5.0
		start_point.z = roundf(start_point.z / 5.0) * 5.0
		start_point.y = 0.0

		current_state = BuildState.PLACING_END
		_update_floor_tooltip()

		_ghost_floor = MeshInstance3D.new()
		_ghost_floor.mesh = BoxMesh.new()
		_ghost_floor.name = "Ghost_Terminal_Floor"
		if ghost_floor_material:
			_ghost_floor.material_override = ghost_floor_material
		add_child(_ghost_floor)


func _handle_placing_end() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos == null:
		return

	var end_pos: Vector3 = hit_pos
	end_pos.x = roundf(end_pos.x / 5.0) * 5.0
	end_pos.z = roundf(end_pos.z / 5.0) * 5.0

	if absf(end_pos.x - start_point.x) < 5.0:
		end_pos.x += 5.0 if end_pos.x >= start_point.x else -5.0
	if absf(end_pos.z - start_point.z) < 5.0:
		end_pos.z += 5.0 if end_pos.z >= start_point.z else -5.0

	_update_rect_transform(_ghost_floor, start_point, end_pos)

	if _is_left_just_clicked():
		_finalize_terminal_floor(start_point, end_pos)


func _update_rect_transform(mesh: MeshInstance3D, p1: Vector3, p2: Vector3) -> void:
	if mesh == null:
		return
	var min_pt := Vector3(minf(p1.x, p2.x), 0.0, minf(p1.z, p2.z))
	var max_pt := Vector3(maxf(p1.x, p2.x), 0.0, maxf(p1.z, p2.z))
	var center := (min_pt + max_pt) / 2.0
	center.y = 0.5 + (current_floor_layer * FLOOR_HEIGHT)
	var size := Vector3(max_pt.x - min_pt.x, 1.0, max_pt.z - min_pt.z)
	mesh.global_position = center
	mesh.scale = size


func _finalize_terminal_floor(p1: Vector3, p2: Vector3) -> void:
	var final_floor := MeshInstance3D.new()
	final_floor.mesh = BoxMesh.new()
	final_floor.name = "TerminalFloor_%s" % str(randi()).substr(0, 5)
	_update_rect_transform(final_floor, p1, p2)
	if placed_floor_material:
		final_floor.material_override = placed_floor_material
	get_tree().current_scene.add_child(final_floor)
	built_terminal_floors.append(final_floor)

	if _ghost_floor:
		_ghost_floor.queue_free()
		_ghost_floor = null

	_exit_build_mode()
	tooltip = "航站楼地块建设完成！"
	tooltip_changed.emit(tooltip)
	build_completed.emit()


func cancel_build() -> void:
	if _ghost_floor:
		_ghost_floor.queue_free()
		_ghost_floor = null
	super.cancel_build()


var _last_left_frame: int = -1
func _is_left_just_clicked() -> bool:
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		var f := Engine.get_process_frames()
		if f != _last_left_frame:
			_last_left_frame = f
			return true
	return false
