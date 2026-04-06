## camera_movement.gd
## 策略/建造游戏风格的 RTS 相机控制器
## 迁移自: CameraMovement.cs (312 行)
## 附加到 Camera3D 节点上使用
extends Camera3D

@export_group("Movement (移动)")
@export var move_speed: float = 10.0
@export var move_accel: float = 10.0
@export var move_decel: float = 10.0

@export_group("Rotation & Orbit (旋转与环绕)")
@export var rotation_speed: float = 80.0
@export var mouse_sensitivity: float = 0.3
## 最小俯仰角 (负值 = 向下看)。-89 = 几乎正下方
@export var min_pitch: float = -89.0
## 最大俯仰角。-5 = 几乎平视地平线
@export var max_pitch: float = -5.0

@export_group("Mouse Controls (鼠标控制)")
@export var zoom_sensitivity: float = 0.01
@export var zoom_damping: float = 5.0

@export_group("Default Focus Distance")
@export var default_focus_distance: float = 15.0

@export_group("Movement Bounds (移动范围限制)")
@export var clamp_y: bool = true
@export var min_y: float = 1.0
@export var max_y: float = 50.0
@export var clamp_x: bool = false
@export var min_x: float = -100.0
@export var max_x: float = 100.0
@export var clamp_z: bool = false
@export var min_z: float = -100.0
@export var max_z: float = 100.0

# 内部状态 - 用角度直接控制相机朝向，彻底避免桶滚
var _yaw: float = 0.0        # 水平旋转角 (度)
var _pitch: float = -45.0    # 俯仰角 (度, 负值 = 向下看)
var _zoom_velocity: float = 0.0
var _current_input: Vector3 = Vector3.ZERO
var _smoothed_move_dir: Vector3 = Vector3.ZERO
var _is_orbiting: bool = false


func _ready() -> void:
	# 从当前 transform 提取初始 yaw 和 pitch
	var euler := global_rotation_degrees
	_yaw = euler.y
	_pitch = euler.x
	# 确保 pitch 在合理范围
	if _pitch > 0.0:
		_pitch = -45.0
	_pitch = clampf(_pitch, min_pitch, max_pitch)
	# 立刻应用，锁死 roll = 0
	_apply_rotation()


func _unhandled_input(event: InputEvent) -> void:
	# 中键按下/释放 → 控制环绕模式
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_MIDDLE:
			if event.pressed:
				_is_orbiting = true
				Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
			else:
				_is_orbiting = false
				Input.mouse_mode = Input.MOUSE_MODE_VISIBLE

	# 滚轮缩放
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			_zoom_velocity += zoom_sensitivity * 120.0
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			_zoom_velocity -= zoom_sensitivity * 120.0

	# 鼠标环绕
	if event is InputEventMouseMotion and _is_orbiting:
		var focus_point := _get_focus_point()
		var old_pos := global_position
		var old_dist := old_pos.distance_to(focus_point)

		# 更新角度
		_yaw -= event.relative.x * mouse_sensitivity
		_pitch -= event.relative.y * mouse_sensitivity
		_pitch = clampf(_pitch, min_pitch, max_pitch)

		# 重新计算相机位置 (保持到焦点的距离不变)
		_apply_rotation()
		var cam_forward := -global_transform.basis.z
		global_position = focus_point - cam_forward * old_dist


func _process(delta: float) -> void:
	if get_tree().paused:
		if _is_orbiting:
			_is_orbiting = false
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		return

	_handle_movement_input(delta)
	_handle_rotation_input(delta)
	_apply_movement(delta)
	_apply_zoom(delta)
	_apply_bounds()


func _handle_movement_input(delta: float) -> void:
	var raw_x := Input.get_axis("move_left", "move_right")
	var raw_y := Input.get_axis("move_backward", "move_forward")
	var raw_v := Input.get_axis("move_descend", "move_ascend")

	if absf(raw_x) > 0.1:
		_current_input.x = move_toward(_current_input.x, raw_x, move_accel * delta)
	else:
		_current_input.x = move_toward(_current_input.x, 0.0, move_decel * delta)

	if absf(raw_y) > 0.1:
		_current_input.y = move_toward(_current_input.y, raw_y, move_accel * delta)
	else:
		_current_input.y = move_toward(_current_input.y, 0.0, move_decel * delta)

	if absf(raw_v) > 0.1:
		_current_input.z = move_toward(_current_input.z, raw_v, move_accel * delta)
	else:
		_current_input.z = move_toward(_current_input.z, 0.0, move_decel * delta)

	# 水平移动方向基于 yaw (忽略 pitch,防止向下看时前进变成俯冲)
	var yaw_rad := deg_to_rad(_yaw)
	var cam_forward := Vector3(-sin(yaw_rad), 0.0, -cos(yaw_rad))
	var cam_right := Vector3(cos(yaw_rad), 0.0, -sin(yaw_rad))

	_smoothed_move_dir = cam_forward * _current_input.y + cam_right * _current_input.x + Vector3.UP * _current_input.z


## Q/E 键绕焦点做水平旋转 (纯 yaw, 零 roll)
func _handle_rotation_input(delta: float) -> void:
	var rotate_input := Input.get_axis("rotate_left", "rotate_right")
	if absf(rotate_input) > 0.01:
		var focus_point := _get_focus_point()
		var old_dist := global_position.distance_to(focus_point)

		# 只改 yaw
		_yaw -= rotate_input * rotation_speed * delta
		_apply_rotation()

		# 重新算位置,保持到焦点距离不变
		var cam_forward := -global_transform.basis.z
		global_position = focus_point - cam_forward * old_dist


func _apply_movement(delta: float) -> void:
	if _smoothed_move_dir.length_squared() > 0.0001:
		global_position += _smoothed_move_dir * move_speed * delta


func _apply_zoom(delta: float) -> void:
	if absf(_zoom_velocity) > 0.001:
		var move_this_frame := _zoom_velocity * delta
		global_position += (-global_transform.basis.z) * move_this_frame
		_zoom_velocity = lerpf(_zoom_velocity, 0.0, zoom_damping * delta)
	else:
		_zoom_velocity = 0.0


func _apply_bounds() -> void:
	var pos := global_position
	if clamp_y:
		pos.y = clampf(pos.y, min_y, max_y)
	if clamp_x:
		pos.x = clampf(pos.x, min_x, max_x)
	if clamp_z:
		pos.z = clampf(pos.z, min_z, max_z)
	global_position = pos


## 核心：用 _yaw 和 _pitch 直接设置旋转，roll 永远为 0
func _apply_rotation() -> void:
	global_rotation_degrees = Vector3(_pitch, _yaw, 0.0)


## 获取相机前方地面焦点 (用于环绕旋转的中心)
func _get_focus_point() -> Vector3:
	# 1. 物理射线
	var space_state := get_world_3d().direct_space_state
	var from := global_position
	var to := from + (-global_transform.basis.z) * 10000.0
	var query := PhysicsRayQueryParameters3D.create(from, to)
	var result := space_state.intersect_ray(query)
	if result.size() > 0:
		return result["position"]

	# 2. 数学 fallback: 射线与 Y=0 平面求交
	var forward := -global_transform.basis.z
	if forward.y < -0.001:
		var t := -global_position.y / forward.y
		return global_position + forward * t

	# 3. 平视时用默认距离
	return global_position + forward * default_focus_distance

