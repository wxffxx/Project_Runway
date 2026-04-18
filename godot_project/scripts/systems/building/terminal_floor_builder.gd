## terminal_floor_builder.gd
## 航站楼建造器 — 矩形拖拽 + 多层立体航站楼自动生成
## 增强版: 玻璃幕墙 / 多楼层可视化 / 结构柱 / 屋顶 / 自动机位接入点 / 全局注册
## 原始: TerminalFloorBuilder.cs (194 行) → 增强重写
extends BaseBuilder
class_name TerminalFloorBuilder

static var instance: BaseBuilder = null

# ---- 建造参数 ----
var current_terminal_type: int = TerminalData.TerminalType.DOMESTIC
var current_floor_count: int = 1

# ---- 内部状态 ----
var _ghost_floor: MeshInstance3D = null
var built_terminals: Array[TerminalData] = []

# ---- 动态材质 (在 _ready 中创建, 保证视觉效果) ----
var _mat_ghost: StandardMaterial3D
var _mat_floor_slab: StandardMaterial3D
var _mat_glass: StandardMaterial3D
var _mat_wall: StandardMaterial3D
var _mat_roof: StandardMaterial3D
var _mat_column: StandardMaterial3D


func _ready() -> void:
	if instance == null:
		instance = self
	else:
		queue_free()
	_create_materials()


# ============================================================
# 材质工厂
# ============================================================
func _create_materials() -> void:
	# 幽灵预览 — 半透明蓝
	_mat_ghost = StandardMaterial3D.new()
	_mat_ghost.albedo_color = Color(0.4, 0.7, 0.9, 0.25)
	_mat_ghost.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_mat_ghost.cull_mode = BaseMaterial3D.CULL_DISABLED

	# 楼层板 — 混凝土灰
	_mat_floor_slab = StandardMaterial3D.new()
	_mat_floor_slab.albedo_color = Color(0.78, 0.76, 0.72)
	_mat_floor_slab.roughness = 0.9

	# 玻璃幕墙 — 半透明天蓝 + 微发光
	_mat_glass = StandardMaterial3D.new()
	_mat_glass.albedo_color = Color(0.55, 0.78, 0.95, 0.35)
	_mat_glass.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_mat_glass.emission_enabled = true
	_mat_glass.emission = Color(0.3, 0.5, 0.7)
	_mat_glass.emission_energy_multiplier = 0.15
	_mat_glass.metallic = 0.6
	_mat_glass.roughness = 0.1
	_mat_glass.cull_mode = BaseMaterial3D.CULL_DISABLED

	# 实体墙 — 浅灰白
	_mat_wall = StandardMaterial3D.new()
	_mat_wall.albedo_color = Color(0.88, 0.86, 0.82)
	_mat_wall.roughness = 0.85

	# 屋顶 — 深灰, 略带金属感
	_mat_roof = StandardMaterial3D.new()
	_mat_roof.albedo_color = Color(0.35, 0.38, 0.42)
	_mat_roof.metallic = 0.3
	_mat_roof.roughness = 0.7

	# 结构柱 — 中灰
	_mat_column = StandardMaterial3D.new()
	_mat_column.albedo_color = Color(0.70, 0.68, 0.65)
	_mat_column.roughness = 0.8


# ============================================================
# 公开 API — 给 TerminalMenuManager 调用
# ============================================================
func start_building_terminal(type: int, floors: int) -> void:
	current_terminal_type = type
	current_floor_count = floors
	current_state = BuildState.PLACING_START

	var config: Dictionary = TerminalData.TYPE_CONFIG.get(type, {})
	var name_str: String = config.get("display_name", "航站楼")
	tooltip = "建造%s (%d层)：点击确定第一个角点。\n【PageUp/Down】调整楼层数。按 ESC 取消。" % [name_str, floors]
	tooltip_changed.emit(tooltip)

	if _ghost_floor:
		_ghost_floor.queue_free()
		_ghost_floor = null


# ============================================================
# _process — 楼层调整 + 基类状态机
# ============================================================
func _process(delta: float) -> void:
	super._process(delta)
	if current_state == BuildState.IDLE:
		return

	var config: Dictionary = TerminalData.TYPE_CONFIG.get(current_terminal_type, {})
	var max_f: int = config.get("max_floors", 3)

	if Input.is_action_just_pressed("floor_up"):
		if current_floor_count < max_f:
			current_floor_count += 1
			_update_tooltip()
	elif Input.is_action_just_pressed("floor_down"):
		if current_floor_count > 1:
			current_floor_count -= 1
			_update_tooltip()


func _update_tooltip() -> void:
	var config: Dictionary = TerminalData.TYPE_CONFIG.get(current_terminal_type, {})
	var name_str: String = config.get("display_name", "航站楼")
	var fh: float = config.get("floor_height", 5.0)
	var total_h := fh * current_floor_count
	if current_state == BuildState.PLACING_START:
		tooltip = "建造%s (%d层, 高%.0fm)：点击确定第一个角点。" % [name_str, current_floor_count, total_h]
	elif current_state == BuildState.PLACING_END:
		tooltip = "拖动鼠标确定航站楼范围 (%d层, 高%.0fm)。左键确认建造。" % [current_floor_count, total_h]
	tooltip_changed.emit(tooltip)


# ============================================================
# 第一阶段 — 确定起点角
# ============================================================
func _handle_placing_start() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos != null and _is_left_just_clicked():
		start_point = hit_pos
		start_point.x = roundf(start_point.x / 5.0) * 5.0
		start_point.z = roundf(start_point.z / 5.0) * 5.0
		start_point.y = 0.0

		current_state = BuildState.PLACING_END
		_update_tooltip()

		_ghost_floor = MeshInstance3D.new()
		_ghost_floor.mesh = BoxMesh.new()
		_ghost_floor.name = "Ghost_Terminal"
		_ghost_floor.material_override = _mat_ghost
		add_child(_ghost_floor)


# ============================================================
# 第二阶段 — 拖拽矩形 + 实时预览
# ============================================================
func _handle_placing_end() -> void:
	var hit_pos = get_mouse_ground_position()
	if hit_pos == null:
		return

	var end_pos: Vector3 = hit_pos
	end_pos.x = roundf(end_pos.x / 5.0) * 5.0
	end_pos.z = roundf(end_pos.z / 5.0) * 5.0

	# 最小尺寸: 20m × 20m
	if absf(end_pos.x - start_point.x) < 20.0:
		end_pos.x += 20.0 if end_pos.x >= start_point.x else -20.0
	if absf(end_pos.z - start_point.z) < 20.0:
		end_pos.z += 20.0 if end_pos.z >= start_point.z else -20.0

	_update_ghost(start_point, end_pos)
	_draw_terminal_tooltip(start_point, end_pos)

	if _is_left_just_clicked():
		_finalize_terminal(start_point, end_pos)


func _update_ghost(p1: Vector3, p2: Vector3) -> void:
	if _ghost_floor == null:
		return
	var config: Dictionary = TerminalData.TYPE_CONFIG.get(current_terminal_type, {})
	var fh: float = config.get("floor_height", 5.0)
	var total_h := fh * current_floor_count

	var min_pt := Vector3(minf(p1.x, p2.x), 0.0, minf(p1.z, p2.z))
	var max_pt := Vector3(maxf(p1.x, p2.x), 0.0, maxf(p1.z, p2.z))
	var center := (min_pt + max_pt) / 2.0
	center.y = total_h / 2.0
	var size := Vector3(max_pt.x - min_pt.x, total_h, max_pt.z - min_pt.z)

	_ghost_floor.global_position = center
	var box: BoxMesh = _ghost_floor.mesh as BoxMesh
	if box:
		box.size = size


# ============================================================
# 最终建造 — 生成 3D 航站楼 + 数据注册
# ============================================================
func _finalize_terminal(p1: Vector3, p2: Vector3) -> void:
	var min_pt := Vector3(minf(p1.x, p2.x), 0.0, minf(p1.z, p2.z))
	var max_pt := Vector3(maxf(p1.x, p2.x), 0.0, maxf(p1.z, p2.z))
	var config: Dictionary = TerminalData.TYPE_CONFIG.get(current_terminal_type, {})
	var fh: float = config.get("floor_height", 5.0)
	var wall_t: float = config.get("wall_thickness", 0.4)
	var glass_sides: bool = config.get("glass_sides", false)
	var total_h := fh * current_floor_count

	var width := max_pt.x - min_pt.x
	var depth := max_pt.z - min_pt.z

	# --- 创建父容器 ---
	var terminal_root := Node3D.new()
	var terminal_id := "T%d_%s" % [current_terminal_type, str(randi()).substr(0, 5)]
	terminal_root.name = "Terminal_%s" % terminal_id
	get_tree().current_scene.add_child(terminal_root)

	# --- 逐层构建 3D 结构 ---
	for floor_i in range(current_floor_count):
		var floor_y := float(floor_i) * fh
		var is_top := (floor_i == current_floor_count - 1)
		_build_floor_geometry(terminal_root, floor_i, floor_y, fh, min_pt, max_pt,
			width, depth, wall_t, glass_sides, is_top)

	# --- 屋顶 ---
	_build_roof(terminal_root, total_h, min_pt, max_pt, width, depth)

	# --- 创建数据并注册 ---
	var terminal_data := TerminalData.new(terminal_id, current_terminal_type,
		current_floor_count, min_pt, max_pt)

	_generate_gate_attachment_nodes(terminal_data, min_pt, max_pt, width, depth)

	if AirportManager:
		AirportManager.register_terminal(terminal_data)
	built_terminals.append(terminal_data)

	# --- 清理 ---
	if _ghost_floor:
		_ghost_floor.queue_free()
		_ghost_floor = null

	_exit_build_mode()
	var display_name: String = config.get("display_name", "航站楼")
	tooltip = "%s 建造完成！面积 %dm × %dm, %d层, %d个机位接入点。" % [
		display_name, int(width), int(depth),
		current_floor_count, terminal_data.gate_attachment_nodes.size()
	]
	tooltip_changed.emit(tooltip)
	build_completed.emit()
	print("[航站楼系统] %s 建造完成: %s (%dm×%dm, %d层)" % [
		display_name, terminal_id, int(width), int(depth), current_floor_count
	])


# ============================================================
# 3D 几何生成 — 单层结构
# ============================================================
func _build_floor_geometry(parent: Node3D, floor_idx: int, floor_y: float,
		fh: float, min_pt: Vector3, max_pt: Vector3,
		width: float, depth: float, wall_t: float,
		glass_sides: bool, _is_top: bool) -> void:
	var center_x := (min_pt.x + max_pt.x) / 2.0
	var center_z := (min_pt.z + max_pt.z) / 2.0
	var slab_thickness := 0.3

	# ---- 楼层板 (混凝土) ----
	var slab := _create_box(
		Vector3(width, slab_thickness, depth),
		Vector3(center_x, floor_y + slab_thickness / 2.0, center_z),
		_mat_floor_slab
	)
	slab.name = "FloorSlab_%d" % floor_idx
	parent.add_child(slab)

	var wall_h := fh - slab_thickness
	var wall_center_y := floor_y + slab_thickness + wall_h / 2.0

	# ---- 前墙 (正 Z 方向 = 机坪侧 = 玻璃幕墙) ----
	var front_wall := _create_box(
		Vector3(width, wall_h, wall_t),
		Vector3(center_x, wall_center_y, max_pt.z - wall_t / 2.0),
		_mat_glass
	)
	front_wall.name = "GlassWall_Front_%d" % floor_idx
	parent.add_child(front_wall)

	# ---- 后墙 (负 Z 方向 = 陆侧 = 实体墙) ----
	var back_wall := _create_box(
		Vector3(width, wall_h, wall_t),
		Vector3(center_x, wall_center_y, min_pt.z + wall_t / 2.0),
		_mat_wall
	)
	back_wall.name = "Wall_Back_%d" % floor_idx
	parent.add_child(back_wall)

	# ---- 左墙 (负 X 方向) ----
	var left_mat := _mat_glass if glass_sides else _mat_wall
	var left_wall := _create_box(
		Vector3(wall_t, wall_h, depth - wall_t * 2.0),
		Vector3(min_pt.x + wall_t / 2.0, wall_center_y, center_z),
		left_mat
	)
	left_wall.name = "Wall_Left_%d" % floor_idx
	parent.add_child(left_wall)

	# ---- 右墙 (正 X 方向) ----
	var right_mat := _mat_glass if glass_sides else _mat_wall
	var right_wall := _create_box(
		Vector3(wall_t, wall_h, depth - wall_t * 2.0),
		Vector3(max_pt.x - wall_t / 2.0, wall_center_y, center_z),
		right_mat
	)
	right_wall.name = "Wall_Right_%d" % floor_idx
	parent.add_child(right_wall)

	# ---- 结构柱 (每 20m 一根, 沿宽度方向) ----
	var col_spacing := 20.0
	var num_cols := int(width / col_spacing)
	for ci in range(1, num_cols):
		var col_x := min_pt.x + float(ci) * col_spacing
		# 前排柱 (靠近机坪侧)
		var col_front := _create_box(
			Vector3(0.6, wall_h, 0.6),
			Vector3(col_x, wall_center_y, max_pt.z - 3.0),
			_mat_column
		)
		col_front.name = "Column_F_%d_%d" % [floor_idx, ci]
		parent.add_child(col_front)
		# 后排柱 (靠近陆侧)
		var col_back := _create_box(
			Vector3(0.6, wall_h, 0.6),
			Vector3(col_x, wall_center_y, min_pt.z + 3.0),
			_mat_column
		)
		col_back.name = "Column_B_%d_%d" % [floor_idx, ci]
		parent.add_child(col_back)


# ============================================================
# 3D 几何生成 — 屋顶 (带 2m 出挑)
# ============================================================
func _build_roof(parent: Node3D, total_h: float, min_pt: Vector3, max_pt: Vector3,
		width: float, depth: float) -> void:
	var roof_thickness := 0.5
	var overhang := 2.0
	var center_x := (min_pt.x + max_pt.x) / 2.0
	var center_z := (min_pt.z + max_pt.z) / 2.0

	var roof := _create_box(
		Vector3(width + overhang, roof_thickness, depth + overhang),
		Vector3(center_x, total_h + roof_thickness / 2.0, center_z),
		_mat_roof
	)
	roof.name = "Roof"
	parent.add_child(roof)


# ============================================================
# 机位接入点生成 — 沿机坪侧 (正 Z) 边缘自动放置
# ============================================================
func _generate_gate_attachment_nodes(data: TerminalData, min_pt: Vector3, max_pt: Vector3,
		width: float, _depth: float) -> void:
	var config: Dictionary = data.get_config()
	var gate_spacing: float = config.get("gate_spacing", 40.0)

	# 机位接入点位于机坪侧外 5m (方便停机位 builders 吸附)
	var airside_z := max_pt.z + 5.0
	var margin := 10.0
	var usable_width := width - margin * 2.0

	if usable_width < gate_spacing:
		print("[航站楼] %s 宽度不足以布置机位接入点 (需 ≥ %.0fm)。" % [data.terminal_name, gate_spacing + margin * 2.0])
		return

	var num_gates := int(usable_width / gate_spacing) + 1
	if num_gates < 1:
		num_gates = 1
	var actual_spacing := usable_width / maxf(float(num_gates - 1), 1.0)
	var start_x := min_pt.x + margin

	for gi in range(num_gates):
		var gate_x := start_x + float(gi) * actual_spacing
		var gate_pos := Vector3(gate_x, 0.0, airside_z)
		var node_id := "%s_GP_%d" % [data.terminal_name, gi]
		var gate_node := PathNode.new(node_id, gate_pos, NavigationEnums.NodeType.GATE_NODE)
		data.gate_attachment_nodes.append(gate_node)

	# 将机位接入点注册到寻路网络 (使用 RunwayData 作为载体, 与滑行道做法统一)
	if RunwayNetworkManager and data.gate_attachment_nodes.size() > 0:
		var proxy := RunwayData.new(
			"TERM_%s_Gates" % data.terminal_name,
			width, 0.0
		)
		for gn in data.gate_attachment_nodes:
			proxy.centerline_nodes.append(gn)
		RunwayNetworkManager.register_runway(proxy)

	print("[航站楼] %s 沿机坪侧生成 %d 个机位接入点, 间距 %.0fm。" % [
		data.terminal_name, data.gate_attachment_nodes.size(), actual_spacing
	])


# ============================================================
# 浮动提示
# ============================================================
func _draw_terminal_tooltip(p1: Vector3, p2: Vector3) -> void:
	if floating_label == null:
		return
	var w := absf(p2.x - p1.x)
	var d := absf(p2.z - p1.z)
	var config: Dictionary = TerminalData.TYPE_CONFIG.get(current_terminal_type, {})
	var name_str: String = config.get("display_name", "航站楼")
	var fh: float = config.get("floor_height", 5.0)
	var total_h := fh * current_floor_count
	var gate_spacing: float = config.get("gate_spacing", 40.0)
	var margin := 10.0
	var est_gates := maxi(0, int((w - margin * 2.0) / gate_spacing) + 1)
	floating_label.text = "%s %d层\n%dm × %dm | 高 %.0fm\n面积 %dm² | 预计 %d 个机位点" % [
		name_str, current_floor_count, int(w), int(d),
		total_h, int(w * d), est_gates
	]
	floating_label.visible = true
	floating_label.position = get_viewport().get_mouse_position() + Vector2(20, 20)


# ============================================================
# 取消建造
# ============================================================
func cancel_build() -> void:
	if _ghost_floor:
		_ghost_floor.queue_free()
		_ghost_floor = null
	super.cancel_build()


# ============================================================
# 工具函数
# ============================================================
func _create_box(box_size: Vector3, pos: Vector3, material: StandardMaterial3D) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	var box := BoxMesh.new()
	box.size = box_size
	mi.mesh = box
	mi.material_override = material
	mi.global_position = pos
	return mi


var _last_left_frame: int = -1
func _is_left_just_clicked() -> bool:
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		var f := Engine.get_process_frames()
		if f != _last_left_frame:
			_last_left_frame = f
			return true
	return false
