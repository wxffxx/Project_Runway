## fps_counter.gd
## FPS 计数器 + 相机信息显示
## 迁移自: FPSCounter.cs
## 附加到 Label 节点上
extends Label

var _delta_time: float = 0.0


func _process(delta: float) -> void:
	# 使用未缩放的 delta 防止暂停时 FPS 归零
	_delta_time += (delta - _delta_time) * 0.1

	var msec := _delta_time * 1000.0
	var fps := 1.0 / _delta_time if _delta_time > 0.0 else 0.0

	var fps_color: String
	if fps >= 60.0:
		fps_color = "green"
	elif fps >= 30.0:
		fps_color = "yellow"
	else:
		fps_color = "red"

	var cam_info := _get_camera_info()

	# 使用 BBCode 富文本 (需要 RichTextLabel) 或纯文本
	text = "%d FPS | %s" % [int(fps), cam_info]

	# 动态改变颜色
	if fps >= 60.0:
		add_theme_color_override("font_color", Color.GREEN)
	elif fps >= 30.0:
		add_theme_color_override("font_color", Color.YELLOW)
	else:
		add_theme_color_override("font_color", Color.RED)


func _get_camera_info() -> String:
	var camera := get_viewport().get_camera_3d()
	if camera == null:
		return "No Active Camera"

	var pos := camera.global_position
	var zoom_info: String
	if camera.projection == Camera3D.PROJECTION_ORTHOGONAL:
		zoom_info = "Size: %.2f" % camera.size
	else:
		zoom_info = "FOV: %.2f" % camera.fov

	return "Pos: (%.2f, %.2f, %.2f) | %s" % [pos.x, pos.y, pos.z, zoom_info]
