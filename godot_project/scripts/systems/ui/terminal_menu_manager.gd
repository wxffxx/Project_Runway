## terminal_menu_manager.gd
## 航站楼类型选择面板 — 自动创建 UI 按钮, 连接到 TerminalFloorBuilder
extends Control


const TERMINAL_OPTIONS := [
	{
		"type": TerminalData.TerminalType.REGIONAL,
		"label": "支线航站楼",
		"desc": "小型 | 最多2层 | 30m机位间距",
		"default_floors": 1,
	},
	{
		"type": TerminalData.TerminalType.DOMESTIC,
		"label": "国内航站楼",
		"desc": "中型 | 最多3层 | 36m机位间距",
		"default_floors": 2,
	},
	{
		"type": TerminalData.TerminalType.INTERNATIONAL,
		"label": "国际航站楼",
		"desc": "大型 | 最多4层 | 全玻璃幕墙 | 50m机位间距",
		"default_floors": 3,
	},
	{
		"type": TerminalData.TerminalType.CARGO,
		"label": "货运航站楼",
		"desc": "单层 | 大跨度 | 40m间距",
		"default_floors": 1,
	},
]

var _info_label: Label = null


func _ready() -> void:
	_build_ui()


func _build_ui() -> void:
	# 按钮行
	var btn_row := HBoxContainer.new()
	btn_row.name = "ButtonRow"
	btn_row.set_anchors_preset(Control.PRESET_TOP_WIDE)
	btn_row.offset_left = 10.0
	btn_row.offset_right = -10.0
	btn_row.offset_top = 5.0
	btn_row.offset_bottom = 40.0
	add_child(btn_row)

	for option in TERMINAL_OPTIONS:
		var btn := Button.new()
		btn.text = option["label"]
		btn.custom_minimum_size = Vector2(140, 30)
		btn.pressed.connect(_on_type_selected.bind(option["type"], option["default_floors"]))
		btn.tooltip_text = option["desc"]
		btn_row.add_child(btn)

	# 信息标签
	_info_label = Label.new()
	_info_label.name = "InfoLabel"
	_info_label.text = "选择航站楼类型开始建造。建造时按 PageUp/PageDown 调整楼层数。"
	_info_label.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_info_label.offset_left = 10.0
	_info_label.offset_top = 45.0
	_info_label.offset_right = -10.0
	_info_label.offset_bottom = 70.0
	add_child(_info_label)


func _on_type_selected(type: int, default_floors: int) -> void:
	var builder = _find_terminal_builder()
	if builder == null:
		push_warning("[TerminalMenu] 未找到 TerminalFloorBuilder 实例！")
		if _info_label:
			_info_label.text = "⚠️ 系统错误：未找到航站楼建造器。"
		return

	var config: Dictionary = TerminalData.TYPE_CONFIG.get(type, {})
	var max_f: int = config.get("max_floors", 2)
	var floors := mini(default_floors, max_f)

	builder.start_building_terminal(type, floors)

	if _info_label:
		var name_str: String = config.get("display_name", "航站楼")
		_info_label.text = "正在建造 %s (%d层)。点击地面确定起点，拖拽确定范围。" % [name_str, floors]


func _find_terminal_builder():
	# 优先通过 group 查找
	var nodes := get_tree().get_nodes_in_group("terminal_builder")
	if nodes.size() > 0:
		return nodes[0]
	# 兜底: 静态实例
	if TerminalFloorBuilder.instance:
		return TerminalFloorBuilder.instance
	return null
