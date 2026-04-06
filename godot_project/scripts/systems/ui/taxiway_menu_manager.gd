## taxiway_menu_manager.gd
## 滑行道建造菜单 - ICAO 类别选择
## 迁移自: TaxiwayMenuManager.cs
extends Control

@export_group("建造分类按钮")
@export var btn_category_a: Button
@export var btn_category_b: Button
@export var btn_category_c: Button
@export var btn_category_d: Button
@export var btn_category_e: Button
@export var btn_category_f: Button

var _core_widths := {
	NavigationEnums.ICAOTaxiwayCategory.A: 7.5,
	NavigationEnums.ICAOTaxiwayCategory.B: 10.5,
	NavigationEnums.ICAOTaxiwayCategory.C: 15.0,
	NavigationEnums.ICAOTaxiwayCategory.D: 18.0,
	NavigationEnums.ICAOTaxiwayCategory.E: 23.0,
	NavigationEnums.ICAOTaxiwayCategory.F: 25.0,
}

var _total_widths := {
	NavigationEnums.ICAOTaxiwayCategory.A: 7.5,
	NavigationEnums.ICAOTaxiwayCategory.B: 10.5,
	NavigationEnums.ICAOTaxiwayCategory.C: 15.0,
	NavigationEnums.ICAOTaxiwayCategory.D: 38.0,
	NavigationEnums.ICAOTaxiwayCategory.E: 38.0,
	NavigationEnums.ICAOTaxiwayCategory.F: 44.0,
}

var _cat_names := ["A", "B", "C", "D", "E", "F"]


func _ready() -> void:
	# 自动查找子节点
	if not btn_category_a:
		btn_category_a = get_node_or_null("ButtonRow/BtnCategoryA")
	if not btn_category_b:
		btn_category_b = get_node_or_null("ButtonRow/BtnCategoryB")
	if not btn_category_c:
		btn_category_c = get_node_or_null("ButtonRow/BtnCategoryC")
	if not btn_category_d:
		btn_category_d = get_node_or_null("ButtonRow/BtnCategoryD")
	if not btn_category_e:
		btn_category_e = get_node_or_null("ButtonRow/BtnCategoryE")
	if not btn_category_f:
		btn_category_f = get_node_or_null("ButtonRow/BtnCategoryF")

	var buttons := [btn_category_a, btn_category_b, btn_category_c, btn_category_d, btn_category_e, btn_category_f]
	for i in range(buttons.size()):
		if buttons[i]:
			var cat_idx := i
			buttons[i].pressed.connect(func(): _start_building(cat_idx))


func _start_building(category: int) -> void:
	var core_width: float = _core_widths[category]
	var total_width: float = _total_widths[category]
	var cat_name: String = _cat_names[category]
	print("[建造指令] 开始建造滑行道, ICAO等级: %s, 核心宽度: %.1fm, 总宽度: %.1fm" % [cat_name, core_width, total_width])

	var builders := get_tree().get_nodes_in_group("taxiway_builder")
	if builders.size() > 0:
		var builder: BaseBuilder = builders[0]
		builder.start_building_taxiway(core_width, total_width, cat_name)

		# 监听建造结束信号 → 恢复菜单显示
		if not builder.build_completed.is_connected(_on_build_finished):
			builder.build_completed.connect(_on_build_finished)
		if not builder.build_cancelled.is_connected(_on_build_finished):
			builder.build_cancelled.connect(_on_build_finished)

		visible = false
	else:
		push_error("场景中找不到 TaxiwayBuilder！")


func _on_build_finished() -> void:
	visible = true

