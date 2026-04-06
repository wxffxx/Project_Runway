## grid_placement.gd
## 网格吸附预览系统 (附加到 Node3D)
## 迁移自: GridPlacement.cs
extends Node3D

@export var cell_size: float = 1.0
@export var ground_layer: int = 1  ## 物理层掩码
@export var preview_scene: PackedScene  ## 预览用的场景

var _preview_instance: Node3D = null


func _ready() -> void:
	if preview_scene:
		_preview_instance = preview_scene.instantiate()
		add_child(_preview_instance)


# ⚠️ BOTTLENECK: 每帧做物理射线检测来跟踪鼠标位置。
# 如果场景中 CollisionShape3D 非常多 (数百个), 射线检测开销会增大。
# 👉 优化建议: 可以改为每 2-3 帧检测一次，或仅用数学平面求交 (不走物理引擎)。
func _process(_delta: float) -> void:
	var camera := get_viewport().get_camera_3d()
	if camera == null:
		return

	var mouse_pos := get_viewport().get_mouse_position()
	var ray_origin := camera.project_ray_origin(mouse_pos)
	var ray_dir := camera.project_ray_normal(mouse_pos)
	var ray_end := ray_origin + ray_dir * 1000.0

	var space_state := get_world_3d().direct_space_state
	var query := PhysicsRayQueryParameters3D.create(ray_origin, ray_end, ground_layer)
	var result := space_state.intersect_ray(query)

	if result.size() > 0:
		var snapped_pos := _snap_to_grid(result["position"])
		if _preview_instance:
			_preview_instance.global_position = snapped_pos


func _snap_to_grid(pos: Vector3) -> Vector3:
	var x := roundf(pos.x / cell_size) * cell_size
	var z := roundf(pos.z / cell_size) * cell_size
	return Vector3(x, 0.1, z)
