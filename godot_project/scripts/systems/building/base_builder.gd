## base_builder.gd
## 建造系统的抽象基类 (跑道/滑行道/停机位/公路共用)
## 迁移自: BaseBuilder.cs
## 附加到 Node3D 节点上使用
extends Node3D
class_name BaseBuilder

enum BuildState { IDLE, PLACING_START, PLACING_END }

var current_state: int = BuildState.IDLE
var start_point: Vector3 = Vector3.ZERO
var tooltip: String = ""
var current_category_name: String = ""

# ⚠️ BOTTLENECK: OnGUI() 浮动提示在 Unity 中是即时模式渲染，
# Godot 中需要用 Label 节点或 CanvasLayer 实现。
# 这里使用信号通知 UI 层更新，避免在 _process 中直接操作 Control 节点。
signal tooltip_changed(text: String)
signal build_completed()
signal build_cancelled()

# 浮动提示 Label (在场景中作为子节点挂载)
@export var floating_label: Label


func _process(_delta: float) -> void:
	match current_state:
		BuildState.PLACING_START:
			_handle_placing_start()
		BuildState.PLACING_END:
			_handle_placing_end()

	if Input.is_action_just_pressed("pause"):
		cancel_build()


func _handle_placing_start() -> void:
	# 子类重写
	pass


func _handle_placing_end() -> void:
	# 子类重写
	pass


func _draw_floating_tooltip() -> void:
	# 子类重写
	pass


func cancel_build_from_external() -> void:
	if current_state != BuildState.IDLE:
		cancel_build()


func cancel_build() -> void:
	_exit_build_mode()
	tooltip = "已取消建造。"
	tooltip_changed.emit(tooltip)
	build_cancelled.emit()


func _exit_build_mode() -> void:
	current_state = BuildState.IDLE


# ⚠️ BOTTLENECK: 鼠标射线 → 地面交点。
# Unity 用 new Plane(Vector3.up, Vector3.zero).Raycast(ray)，这是纯数学运算。
# Godot 中我们也用纯数学实现 (不走物理引擎)，性能等价。
# 但如果改用 PhysicsRayQueryParameters3D，每帧都做物理查询会有开销。
func get_mouse_ground_position() -> Variant:
	var camera := get_viewport().get_camera_3d()
	if camera == null:
		return null

	var mouse_pos := get_viewport().get_mouse_position()
	var ray_origin := camera.project_ray_origin(mouse_pos)
	var ray_dir := camera.project_ray_normal(mouse_pos)

	# 数学平面求交: Y=0 平面
	if absf(ray_dir.y) < 0.0001:
		return null  # 射线几乎平行于地面

	var t := -ray_origin.y / ray_dir.y
	if t < 0.0:
		return null  # 射线朝上，不会碰到地面

	var hit_point := ray_origin + ray_dir * t

	# 网格吸附 (1m 一个格子)
	hit_point.x = roundf(hit_point.x)
	hit_point.z = roundf(hit_point.z)
	hit_point.y = 0.0

	return hit_point
