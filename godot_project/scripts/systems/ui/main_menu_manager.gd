## main_menu_manager.gd
## 主菜单管理器 (开始游戏、设置、退出)
## 迁移自: MainMenuManager.cs
extends Control

@export_group("UI 面板")
@export var main_buttons_panel: Control
@export var settings_panel: Control

@export_group("主界面按钮")
@export var start_game_button: Button
@export var load_demo_button: Button
@export var open_settings_button: Button
@export var quit_game_button: Button
@export var close_settings_button: Button

@export_group("场景设置")
@export var game_scene: String = "res://scenes/game_scene.tscn"
@export var demo_scene: String = "res://scenes/demo_scene.tscn"


func _ready() -> void:
	# 自动查找子节点
	if not main_buttons_panel:
		main_buttons_panel = get_node_or_null("MainButtonsPanel")
	if not settings_panel:
		settings_panel = get_node_or_null("SettingsPanel")
	if not start_game_button and main_buttons_panel:
		start_game_button = main_buttons_panel.get_node_or_null("StartGameButton")
	if not load_demo_button and main_buttons_panel:
		load_demo_button = main_buttons_panel.get_node_or_null("LoadDemoButton")
	if not open_settings_button and main_buttons_panel:
		open_settings_button = main_buttons_panel.get_node_or_null("OpenSettingsButton")
	if not quit_game_button and main_buttons_panel:
		quit_game_button = main_buttons_panel.get_node_or_null("QuitGameButton")
	if not close_settings_button and settings_panel:
		close_settings_button = settings_panel.get_node_or_null("CloseSettingsButton")

	if main_buttons_panel:
		main_buttons_panel.visible = true
	if settings_panel:
		settings_panel.visible = false

	if start_game_button:
		start_game_button.pressed.connect(start_game)
	if load_demo_button:
		load_demo_button.pressed.connect(load_demo)
	if open_settings_button:
		open_settings_button.pressed.connect(toggle_settings)
	if close_settings_button:
		close_settings_button.pressed.connect(close_settings)
	if quit_game_button:
		quit_game_button.pressed.connect(quit_game)

	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	get_tree().paused = false


func start_game() -> void:
	get_tree().change_scene_to_file(game_scene)


func load_demo() -> void:
	get_tree().change_scene_to_file(demo_scene)


func toggle_settings() -> void:
	if settings_panel:
		settings_panel.visible = not settings_panel.visible


func close_settings() -> void:
	if settings_panel:
		settings_panel.visible = false


func quit_game() -> void:
	print("Quit Game Clicked!")
	get_tree().quit()
