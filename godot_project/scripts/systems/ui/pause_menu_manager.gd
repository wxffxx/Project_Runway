## pause_menu_manager.gd
## 暂停菜单系统
## 迁移自: PauseMenuManager.cs
## 附加到 CanvasLayer/Control 节点上
extends Control

@export_group("UI 面板引用")
@export var pause_main_panel: Control
@export var settings_panel: Control

@export_group("游戏中 UI 控制")
@export var in_game_ui_root: Control

@export_group("按钮引用")
@export var resume_button: Button
@export var return_to_menu_button: Button
@export var open_settings_button: Button
@export var close_settings_button: Button
@export var quit_button: Button

@export_group("场景设置")
@export var main_menu_scene: String = "res://scenes/main_menu.tscn"

var _is_paused: bool = false


func _ready() -> void:
	# 自动查找子节点 (如果 @export 未手动设置)
	if not pause_main_panel:
		pause_main_panel = get_node_or_null("PauseMainPanel")
	if not settings_panel:
		settings_panel = get_node_or_null("SettingsPanel")
	if not in_game_ui_root:
		var ui_layer = get_parent()
		if ui_layer:
			in_game_ui_root = ui_layer.get_node_or_null("InGameUI")

	# 自动查找按钮
	if not resume_button and pause_main_panel:
		resume_button = pause_main_panel.get_node_or_null("VBoxContainer/ResumeButton")
	if not return_to_menu_button and pause_main_panel:
		return_to_menu_button = pause_main_panel.get_node_or_null("VBoxContainer/ReturnToMenuButton")
	if not open_settings_button and pause_main_panel:
		open_settings_button = pause_main_panel.get_node_or_null("VBoxContainer/SettingsButton")
	if not quit_button and pause_main_panel:
		quit_button = pause_main_panel.get_node_or_null("VBoxContainer/QuitButton")
	if not close_settings_button and settings_panel:
		close_settings_button = settings_panel.get_node_or_null("CloseSettingsButton")

	# 绑定按钮事件
	if resume_button:
		resume_button.pressed.connect(resume_game)
	if return_to_menu_button:
		return_to_menu_button.pressed.connect(return_to_main_menu)
	if quit_button:
		quit_button.pressed.connect(quit_game)
	if open_settings_button:
		open_settings_button.pressed.connect(toggle_settings)
	if close_settings_button:
		close_settings_button.pressed.connect(close_settings)

	# 初始隐藏
	if pause_main_panel:
		pause_main_panel.visible = false
	if settings_panel:
		settings_panel.visible = false


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("pause"):
		toggle_pause()
		get_viewport().set_input_as_handled()


func toggle_pause() -> void:
	if _is_paused:
		if settings_panel and settings_panel.visible:
			close_settings()
		else:
			resume_game()
	else:
		pause_game()


func pause_game() -> void:
	_is_paused = true
	if pause_main_panel:
		pause_main_panel.visible = true
	if settings_panel:
		settings_panel.visible = false
	if in_game_ui_root:
		in_game_ui_root.visible = false

	# 强制打断正在进行的建造
	# ⚠️ BOTTLENECK: 使用 get_tree().get_nodes_in_group() 查找 Builder 实例。
	# 如果场景节点数量极多，此操作有微量开销。
	# 但只在暂停时调用一次，不影响游戏帧率。
	for node in get_tree().get_nodes_in_group("builders"):
		if node.has_method("cancel_build_from_external"):
			node.cancel_build_from_external()

	get_tree().paused = true
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE


func resume_game() -> void:
	_is_paused = false
	if pause_main_panel:
		pause_main_panel.visible = false
	if settings_panel:
		settings_panel.visible = false
	if in_game_ui_root:
		in_game_ui_root.visible = true

	get_tree().paused = false


func toggle_settings() -> void:
	if settings_panel:
		settings_panel.visible = not settings_panel.visible


func close_settings() -> void:
	if settings_panel:
		settings_panel.visible = false


func return_to_main_menu() -> void:
	get_tree().paused = false
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	get_tree().change_scene_to_file(main_menu_scene)


func quit_game() -> void:
	print("Quit Game Clicked!")
	get_tree().paused = false
	get_tree().quit()
