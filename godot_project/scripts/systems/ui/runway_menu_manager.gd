## runway_menu_manager.gd
## 跑道建造菜单 - ICAO 类别选择
## 迁移自: RunwayMenuManager.cs
extends Control

@export_group("ICAO Category Buttons")
@export var cat_a_button: Button
@export var cat_b_button: Button
@export var cat_c_button: Button
@export var cat_d_button: Button
@export var cat_e_button: Button
@export var cat_f_button: Button

@export_group("Info")
@export var info_label: Label

var _category_widths := {
	NavigationEnums.ICAORunwayCategory.A: 15,
	NavigationEnums.ICAORunwayCategory.B: 23,
	NavigationEnums.ICAORunwayCategory.C: 30,
	NavigationEnums.ICAORunwayCategory.D: 45,
	NavigationEnums.ICAORunwayCategory.E: 45,
	NavigationEnums.ICAORunwayCategory.F: 60,
}

var _category_names := {
	NavigationEnums.ICAORunwayCategory.A: "A",
	NavigationEnums.ICAORunwayCategory.B: "B",
	NavigationEnums.ICAORunwayCategory.C: "C",
	NavigationEnums.ICAORunwayCategory.D: "D",
	NavigationEnums.ICAORunwayCategory.E: "E",
	NavigationEnums.ICAORunwayCategory.F: "F",
}


func _ready() -> void:
	# 自动查找子节点
	if not cat_a_button:
		cat_a_button = get_node_or_null("ButtonRow/CatAButton")
	if not cat_b_button:
		cat_b_button = get_node_or_null("ButtonRow/CatBButton")
	if not cat_c_button:
		cat_c_button = get_node_or_null("ButtonRow/CatCButton")
	if not cat_d_button:
		cat_d_button = get_node_or_null("ButtonRow/CatDButton")
	if not cat_e_button:
		cat_e_button = get_node_or_null("ButtonRow/CatEButton")
	if not cat_f_button:
		cat_f_button = get_node_or_null("ButtonRow/CatFButton")
	if not info_label:
		info_label = get_node_or_null("InfoLabel")

	if cat_a_button:
		cat_a_button.pressed.connect(func(): _start_building(NavigationEnums.ICAORunwayCategory.A))
	if cat_b_button:
		cat_b_button.pressed.connect(func(): _start_building(NavigationEnums.ICAORunwayCategory.B))
	if cat_c_button:
		cat_c_button.pressed.connect(func(): _start_building(NavigationEnums.ICAORunwayCategory.C))
	if cat_d_button:
		cat_d_button.pressed.connect(func(): _start_building(NavigationEnums.ICAORunwayCategory.D))
	if cat_e_button:
		cat_e_button.pressed.connect(func(): _start_building(NavigationEnums.ICAORunwayCategory.E))
	if cat_f_button:
		cat_f_button.pressed.connect(func(): _start_building(NavigationEnums.ICAORunwayCategory.F))

	if info_label:
		info_label.text = "点击跑道规格 (A-F) 立刻开始建造白模。"


func _start_building(category: int) -> void:
	var width: int = _category_widths[category]
	var cat_name: String = _category_names[category]
	print("[建造指令] 开始建造跑道白模, ICAO等级: %s, 宽度: %dm" % [cat_name, width])

	# 查找场景中的 RunwayBuilder
	var builders := get_tree().get_nodes_in_group("runway_builder")
	if builders.size() > 0:
		var builder: BaseBuilder = builders[0]
		builder.start_building_runway(float(width), cat_name)

		# 监听建造结束信号 → 恢复菜单显示
		if not builder.build_completed.is_connected(_on_build_finished):
			builder.build_completed.connect(_on_build_finished)
		if not builder.build_cancelled.is_connected(_on_build_finished):
			builder.build_cancelled.connect(_on_build_finished)

		visible = false
	else:
		push_error("场景中找不到 RunwayBuilder！")


func _on_build_finished() -> void:
	visible = true

