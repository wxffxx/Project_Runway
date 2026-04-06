## building_menu_manager.gd
## 建造菜单管理器 - 飞行区/乘客区/后勤区分类切换
## 迁移自: BuildingMenuManager.cs
extends Control

@export_group("Main Categories")
@export var flight_zone_button: Button
@export var passenger_zone_button: Button
@export var logistics_zone_button: Button

@export_group("Main Category Panels")
@export var flight_zone_panel: Control
@export var passenger_zone_panel: Control
@export var logistics_zone_panel: Control

@export_group("Flight Zone Sub-Categories")
@export var runway_button: Button
@export var taxiway_button: Button
@export var hangar_button: Button
@export var nav_aid_button: Button
@export var decoration_button: Button

@export_group("Flight Zone Sub-Panels")
@export var runway_panel: Control
@export var taxiway_panel: Control
@export var hangar_panel: Control
@export var nav_aid_panel: Control
@export var decoration_panel: Control


func _ready() -> void:
	# 自动查找子节点
	if not flight_zone_button:
		flight_zone_button = get_node_or_null("MainCategoryBar/FlightZoneButton")
	if not passenger_zone_button:
		passenger_zone_button = get_node_or_null("MainCategoryBar/PassengerZoneButton")
	if not logistics_zone_button:
		logistics_zone_button = get_node_or_null("MainCategoryBar/LogisticsZoneButton")
	if not flight_zone_panel:
		flight_zone_panel = get_node_or_null("FlightZonePanel")
	if not passenger_zone_panel:
		passenger_zone_panel = get_node_or_null("PassengerZonePanel")
	if not logistics_zone_panel:
		logistics_zone_panel = get_node_or_null("LogisticsZonePanel")
	if not runway_button and flight_zone_panel:
		runway_button = flight_zone_panel.get_node_or_null("SubCategoryBar/RunwayButton")
	if not taxiway_button and flight_zone_panel:
		taxiway_button = flight_zone_panel.get_node_or_null("SubCategoryBar/TaxiwayButton")
	if not hangar_button and flight_zone_panel:
		hangar_button = flight_zone_panel.get_node_or_null("SubCategoryBar/HangarButton")
	if not runway_panel and flight_zone_panel:
		runway_panel = flight_zone_panel.get_node_or_null("RunwayPanel")
	if not taxiway_panel and flight_zone_panel:
		taxiway_panel = flight_zone_panel.get_node_or_null("TaxiwayPanel")
	if not hangar_panel and flight_zone_panel:
		hangar_panel = flight_zone_panel.get_node_or_null("HangarPanel")

	if flight_zone_button:
		flight_zone_button.pressed.connect(show_flight_zone)
	if passenger_zone_button:
		passenger_zone_button.pressed.connect(show_passenger_zone)
	if logistics_zone_button:
		logistics_zone_button.pressed.connect(show_logistics_zone)

	if runway_button:
		runway_button.pressed.connect(show_runway_panel)
	if taxiway_button:
		taxiway_button.pressed.connect(show_taxiway_panel)
	if hangar_button:
		hangar_button.pressed.connect(show_hangar_panel)
	if nav_aid_button:
		nav_aid_button.pressed.connect(show_nav_aid_panel)
	if decoration_button:
		decoration_button.pressed.connect(show_decoration_panel)

	show_flight_zone()


func show_flight_zone() -> void:
	_set_main_panels(true, false, false)
	_set_button_state(flight_zone_button, true)
	_set_button_state(passenger_zone_button, false)
	_set_button_state(logistics_zone_button, false)
	show_runway_panel()


func show_passenger_zone() -> void:
	_set_main_panels(false, true, false)
	_set_button_state(flight_zone_button, false)
	_set_button_state(passenger_zone_button, true)
	_set_button_state(logistics_zone_button, false)


func show_logistics_zone() -> void:
	_set_main_panels(false, false, true)
	_set_button_state(flight_zone_button, false)
	_set_button_state(passenger_zone_button, false)
	_set_button_state(logistics_zone_button, true)


func _set_main_panels(flight: bool, passenger: bool, logistics: bool) -> void:
	if flight_zone_panel:
		flight_zone_panel.visible = flight
	if passenger_zone_panel:
		passenger_zone_panel.visible = passenger
	if logistics_zone_panel:
		logistics_zone_panel.visible = logistics


func show_runway_panel() -> void:
	_set_flight_sub_panels(true, false, false, false, false)

func show_taxiway_panel() -> void:
	_set_flight_sub_panels(false, true, false, false, false)

func show_hangar_panel() -> void:
	_set_flight_sub_panels(false, false, true, false, false)

func show_nav_aid_panel() -> void:
	_set_flight_sub_panels(false, false, false, true, false)

func show_decoration_panel() -> void:
	_set_flight_sub_panels(false, false, false, false, true)


func _set_flight_sub_panels(run: bool, taxi: bool, hangar: bool, nav: bool, decor: bool) -> void:
	if runway_panel:
		runway_panel.visible = run
	if taxiway_panel:
		taxiway_panel.visible = taxi
	if hangar_panel:
		hangar_panel.visible = hangar
	if nav_aid_panel:
		nav_aid_panel.visible = nav
	if decoration_panel:
		decoration_panel.visible = decor


func _set_button_state(btn: Button, is_active: bool) -> void:
	if btn:
		btn.disabled = is_active
