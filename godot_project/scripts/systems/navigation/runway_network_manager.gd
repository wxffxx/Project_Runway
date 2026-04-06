## runway_network_manager.gd
## 全局寻路网络管家 (Autoload 单例)
## 存储所有跑道/滑行道/停机位的图论数据 + 实时可视化
## 迁移自: RunwayNetworkManager.cs
extends Node

var all_runways: Array[RunwayData] = []
var all_gates: Array[GateData] = []

# --- 可视化节点容器 ---
var _vis_root: Node3D = null
var _vis_visible: bool = true  # 默认开启可视化

# 节点颜色配置
const NODE_COLORS := {
	0: Color(1.0, 0.3, 0.3),   # RUNWAY_CENTERLINE - 红
	1: Color(1.0, 1.0, 0.2),   # RUNWAY_THRESHOLD  - 黄
	2: Color(1.0, 0.5, 0.0),   # RUNWAY_END         - 橙
	3: Color(0.3, 0.8, 1.0),   # TAXIWAY_POINT      - 青
	4: Color(1.0, 0.2, 1.0),   # HOLD_SHORT_NODE    - 粉
	5: Color(0.3, 1.0, 0.3),   # GATE_NODE          - 绿
}

const EDGE_COLORS := {
	0: Color(1.0, 0.4, 0.4, 0.7),  # RUNWAY_SEGMENT    - 红线
	1: Color(1.0, 0.8, 0.2, 0.7),  # HIGH_SPEED_EXIT   - 黄线
	2: Color(0.4, 0.8, 1.0, 0.7),  # STANDARD_TAXIWAY  - 青线
	3: Color(0.8, 0.4, 1.0, 0.7),  # PUSHBACK_PATH     - 紫线
}

const NODE_SPHERE_RADIUS := 1.5
const EDGE_LINE_WIDTH := 0.4


func _ready() -> void:
	# 监听场景切换，确保 _vis_root 始终有效
	get_tree().tree_changed.connect(_ensure_vis_root)
	call_deferred("_ensure_vis_root")


func _ensure_vis_root() -> void:
	var scene := get_tree().current_scene
	if scene == null:
		return
	# 如果 _vis_root 已存在且仍在场景树中，不重复创建
	if _vis_root != null and _vis_root.is_inside_tree():
		return
	# 创建新的
	_vis_root = Node3D.new()
	_vis_root.name = "NetworkVisualization"
	scene.add_child(_vis_root)
	# 如果有已注册的数据，重建可视化
	if all_runways.size() > 0 or all_gates.size() > 0:
		_rebuild_visualization()


func _input(event: InputEvent) -> void:
	if not (event is InputEventKey and event.pressed and not event.echo):
		return
	match event.keycode:
		KEY_N:
			toggle_visualization()
		KEY_M:
			_print_network_graph()


## 自动连接半径: 新节点在此距离内会自动连接到最近的已有节点
const AUTO_CONNECT_RADIUS := 30.0


func register_runway(new_data: RunwayData) -> void:
	all_runways.append(new_data)
	print("[寻路网络] 已注册跑道 %s, 长度 %dm, 包含 %d 个主线节点。" % [
		new_data.runway_name,
		int(new_data.length),
		new_data.centerline_nodes.size()
	])
	# 尝试自动连接到已有网络
	_auto_connect_new_nodes(new_data.centerline_nodes)
	_rebuild_visualization()


func register_gate(new_data: GateData) -> void:
	all_gates.append(new_data)
	print("[停机位网络] 已注册停机位 %s" % new_data.gate_name)
	if new_data.gate_node:
		var gate_nodes: Array[PathNode] = [new_data.gate_node]
		_auto_connect_new_nodes(gate_nodes)
	_rebuild_visualization()


## 核心: 将新节点自动连接到半径内最近的已有节点
func _auto_connect_new_nodes(new_nodes: Array[PathNode]) -> void:
	# 收集所有"已有"节点 (不含本次新注册的)
	var existing_nodes: Array[PathNode] = []
	for runway in all_runways:
		for node in runway.centerline_nodes:
			if node not in new_nodes:
				existing_nodes.append(node)
	for gate in all_gates:
		if gate.gate_node and gate.gate_node not in new_nodes:
			existing_nodes.append(gate.gate_node)

	if existing_nodes.size() == 0:
		return

	var connections_made := 0
	for new_node in new_nodes:
		# 只连接端点 (起/止/滑行/停机位)，中间节点不需要跨结构连接
		if new_node.type == NavigationEnums.NodeType.RUNWAY_CENTERLINE:
			continue

		var closest: PathNode = null
		var closest_dist := AUTO_CONNECT_RADIUS

		for existing in existing_nodes:
			var d: float = new_node.position.distance_to(existing.position)
			if d < closest_dist:
				closest_dist = d
				closest = existing

		if closest != null:
			# 检查是否已经连过
			var already_connected := false
			for edge in new_node.connected_edges:
				if edge.to_node == closest or edge.from_node == closest:
					already_connected = true
					break
			if not already_connected:
				# 确定边类型
				var edge_type := NavigationEnums.EdgeType.STANDARD_TAXIWAY
				if new_node.type == NavigationEnums.NodeType.GATE_NODE:
					edge_type = NavigationEnums.EdgeType.PUSHBACK_PATH
				new_node.add_edge(closest, closest_dist, edge_type, false)
				connections_made += 1
				print("  ✅ 自动连接: %s ↔ %s (%.1fm, 类型:%d)" % [
					new_node.id, closest.id, closest_dist, edge_type
				])

	if connections_made > 0:
		print("[自动连接] 共建立 %d 条跨结构连接。" % connections_made)
	else:
		print("[自动连接] 未找到 %.0fm 内的可连接节点。" % AUTO_CONNECT_RADIUS)


## 查找距离目标位置最近的滑行道节点
func find_nearest_taxiway_node(target_pos: Vector3, max_distance: float) -> PathNode:
	var closest: PathNode = null
	var min_dist := max_distance

	for runway in all_runways:
		for node in runway.centerline_nodes:
			if node.type == NavigationEnums.NodeType.TAXIWAY_POINT:
				var dist := target_pos.distance_to(node.position)
				if dist < min_dist:
					min_dist = dist
					closest = node
	return closest


func toggle_visualization() -> void:
	_vis_visible = not _vis_visible
	if _vis_root:
		_vis_root.visible = _vis_visible
	print("[网络可视化] %s" % ("ON" if _vis_visible else "OFF"))


## 按 M 键打印完整的网络拓扑图到控制台
func _print_network_graph() -> void:
	print("\n" + "============================================================")
	print("📊 寻路网络拓扑 (按 M 触发)")
	print("============================================================")

	var total_nodes := 0
	var total_edges := 0
	var drawn_edges := {}

	for i in range(all_runways.size()):
		var rwy := all_runways[i]
		print("\n📦 结构 #%d: %s (%.0fm)" % [i, rwy.runway_name, rwy.length])
		print("   节点数: %d" % rwy.centerline_nodes.size())

		for node in rwy.centerline_nodes:
			total_nodes += 1
			var type_name := _get_node_type_name(node.type)
			var edge_count := node.connected_edges.size()
			var position_str := "(%.0f, %.0f)" % [node.position.x, node.position.z]
			print("   ├─ %s [%s] 位置%s  连接:%d条边" % [
				node.id, type_name, position_str, edge_count
			])

			for edge in node.connected_edges:
				var edge_id := _get_edge_id(edge.from_node, edge.to_node)
				if edge_id not in drawn_edges:
					drawn_edges[edge_id] = true
					total_edges += 1
					var dir_str := "→" if edge.is_one_way else "↔"
					var edge_type_name := _get_edge_type_name(edge.type)
					print("   │  %s %s %s (%.1fm, %s)" % [
						edge.from_node.id, dir_str, edge.to_node.id,
						edge.distance, edge_type_name
					])

	for gate in all_gates:
		if gate.gate_node:
			total_nodes += 1
			print("\n🚪 停机位: %s 位置(%.0f, %.0f) 连接:%d条边" % [
				gate.gate_name, gate.gate_node.position.x,
				gate.gate_node.position.z, gate.gate_node.connected_edges.size()
			])

	# 连通性检测
	var all_nodes := _collect_all_nodes()
	var components := _find_connected_components(all_nodes)

	print("\n" + "----------------------------------------")
	print("📈 统计: %d 节点, %d 条边" % [total_nodes, total_edges])
	if components.size() == 1:
		print("✅ 网络完全连通！所有节点可达。")
	else:
		print("⚠️  网络有 %d 个孤立分区 (未互通):" % components.size())
		for ci in range(components.size()):
			var comp: Array = components[ci]
			var names: PackedStringArray = []
			for n in comp:
				names.append(n.id)
			print("   分区 %d (%d个节点): %s" % [ci + 1, comp.size(), ", ".join(names)])
	print("============================================================\n")


## 收集所有节点
func _collect_all_nodes() -> Array[PathNode]:
	var nodes: Array[PathNode] = []
	for rwy in all_runways:
		for node in rwy.centerline_nodes:
			if node not in nodes:
				nodes.append(node)
	for gate in all_gates:
		if gate.gate_node and gate.gate_node not in nodes:
			nodes.append(gate.gate_node)
	return nodes


## BFS 连通分量检测
func _find_connected_components(nodes: Array[PathNode]) -> Array:
	var visited := {}
	var components: Array = []

	for node in nodes:
		if node.id in visited:
			continue
		# BFS
		var queue: Array[PathNode] = [node]
		var component: Array[PathNode] = []
		visited[node.id] = true

		while queue.size() > 0:
			var current: PathNode = queue.pop_front()
			component.append(current)
			for edge in current.connected_edges:
				var neighbor: PathNode = edge.to_node
				if neighbor.id not in visited:
					visited[neighbor.id] = true
					queue.append(neighbor)

		components.append(component)

	return components


func _get_node_type_name(t: int) -> String:
	match t:
		0: return "跑道中轴"
		1: return "跑道入口"
		2: return "跑道尾端"
		3: return "滑行道点"
		4: return "等待点"
		5: return "停机位"
		_: return "未知"


func _get_edge_type_name(t: int) -> String:
	match t:
		0: return "跑道段"
		1: return "快速脱离"
		2: return "滑行道"
		3: return "推出路径"
		_: return "未知"


## 完全重建可视化 (每次有新元素注册时调用)
func _rebuild_visualization() -> void:
	if _vis_root == null:
		return

	# 清空旧的
	for child in _vis_root.get_children():
		child.queue_free()

	# 等一帧让 queue_free 生效
	await get_tree().process_frame

	# 绘制所有节点和边
	var drawn_edges := {}  # 防止重复绘制

	for runway in all_runways:
		for node in runway.centerline_nodes:
			_draw_node_sphere(node)

			for edge in node.connected_edges:
				var edge_id := _get_edge_id(edge.from_node, edge.to_node)
				if edge_id not in drawn_edges:
					drawn_edges[edge_id] = true
					_draw_edge_line(edge)

	# 绘制停机位节点
	for gate in all_gates:
		if gate.gate_node:
			_draw_node_sphere_from_data(gate.gate_node.position, NavigationEnums.NodeType.GATE_NODE, gate.gate_name)

	_vis_root.visible = _vis_visible


## 在节点位置绘制一个高亮球体 + 3D 标签
func _draw_node_sphere(node: PathNode) -> void:
	_draw_node_sphere_from_data(node.position, node.type, node.id)


func _draw_node_sphere_from_data(pos: Vector3, node_type: int, label_text: String) -> void:
	var mi := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = NODE_SPHERE_RADIUS
	sphere.height = NODE_SPHERE_RADIUS * 2.0
	sphere.radial_segments = 8
	sphere.rings = 4
	mi.mesh = sphere

	# 发光材质
	var mat := StandardMaterial3D.new()
	mat.albedo_color = NODE_COLORS.get(node_type, Color.WHITE)
	mat.emission_enabled = true
	mat.emission = mat.albedo_color
	mat.emission_energy_multiplier = 0.5
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.albedo_color.a = 0.85
	mi.material_override = mat

	mi.position = pos + Vector3(0, NODE_SPHERE_RADIUS + 0.2, 0)
	mi.name = "Node_%s" % label_text.replace(" ", "_")
	_vis_root.add_child(mi)

	# 节点标签 (3D 文字，始终面向相机)
	var label := Label3D.new()
	label.text = label_text
	label.font_size = 48
	label.pixel_size = 0.02
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label.outline_size = 8
	label.modulate = mat.albedo_color
	label.position = pos + Vector3(0, NODE_SPHERE_RADIUS * 2 + 1.5, 0)
	label.name = "Label_%s" % label_text.replace(" ", "_")
	_vis_root.add_child(label)


## 在两个节点之间绘制一条连线
func _draw_edge_line(edge: PathEdge) -> void:
	var from_pos := edge.from_node.position + Vector3(0, 0.3, 0)
	var to_pos := edge.to_node.position + Vector3(0, 0.3, 0)

	var diff := to_pos - from_pos
	var dist := diff.length()
	if dist < 0.1:
		return

	var mi := MeshInstance3D.new()
	var box := BoxMesh.new()
	# 直接设置 BoxMesh 的真实尺寸，不用 scale 拉伸
	box.size = Vector3(EDGE_LINE_WIDTH, 0.15, dist)
	mi.mesh = box

	# 发光材质
	var mat := StandardMaterial3D.new()
	mat.albedo_color = EDGE_COLORS.get(edge.type, Color(0.8, 0.8, 0.8, 0.7))
	mat.emission_enabled = true
	mat.emission = mat.albedo_color
	mat.emission_energy_multiplier = 0.3
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mi.material_override = mat

	# 位置 = 中点，旋转用 atan2 (与建造器统一)
	var center := (from_pos + to_pos) / 2.0
	mi.position = center

	var flat_dir := Vector3(diff.x, 0.0, diff.z)
	if flat_dir.length() > 0.01:
		mi.rotation = Vector3(0.0, atan2(flat_dir.x, flat_dir.z), 0.0)

	# 单向边 → 加箭头
	if edge.is_one_way:
		mi.name = "Edge_OneWay"
		_draw_arrow(to_pos, from_pos, mat)
	else:
		mi.name = "Edge_BiDir"

	_vis_root.add_child(mi)


## 在单向边终点绘制箭头
func _draw_arrow(to_pos: Vector3, from_pos: Vector3, mat: StandardMaterial3D) -> void:
	var arrow := MeshInstance3D.new()
	var cone := CylinderMesh.new()
	cone.top_radius = 0.0
	cone.bottom_radius = 0.8
	cone.height = 1.5
	arrow.mesh = cone
	arrow.material_override = mat

	var dir := (to_pos - from_pos)
	var flat_dir := Vector3(dir.x, 0.0, dir.z)
	var arrow_pos := to_pos - flat_dir.normalized() * 2.0

	arrow.position = arrow_pos + Vector3(0, 0.8, 0)
	arrow.name = "Arrow"

	# 箭头朝向: 先 Y 轴旋转对齐方向，再 X 轴倾倒 90° 让尖端朝前
	if flat_dir.length() > 0.01:
		arrow.rotation.y = atan2(flat_dir.x, flat_dir.z)
		arrow.rotation.x = deg_to_rad(90)

	_vis_root.add_child(arrow)


func _get_edge_id(node_a: PathNode, node_b: PathNode) -> String:
	if node_a.id < node_b.id:
		return node_a.id + "_" + node_b.id
	return node_b.id + "_" + node_a.id
