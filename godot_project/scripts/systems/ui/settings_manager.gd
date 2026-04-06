## settings_manager.gd
## 设置管理器 - 显示/控制/游戏设置面板
## 迁移自: SettingsManager.cs (309 行)
## 附加到设置面板的根 Control 节点上
extends Control

@export_group("标签页按钮 (Tabs)")
@export var tab_display_btn: Button
@export var tab_quality_btn: Button
@export var tab_controls_btn: Button
@export var tab_game_btn: Button

@export_group("子面板 (Panels)")
@export var display_panel: Control
@export var quality_panel: Control
@export var controls_panel: Control
@export var game_panel: Control

@export_group("显示设置 UI")
@export var resolution_dropdown: OptionButton
@export var window_mode_dropdown: OptionButton
@export var framerate_dropdown: OptionButton

@export_group("控制设置 UI")
@export var mouse_sensitivity_slider: HSlider
@export var mouse_sensitivity_label: Label
@export var invert_y_toggle: CheckBox

@export_group("游戏设置 UI")
@export var show_fps_toggle: CheckBox

# ⚠️ BOTTLENECK: Godot 没有 PlayerPrefs。这里用 ConfigFile 替代。
# ConfigFile 每次 save() 都会写磁盘。频繁修改滑块可能导致密集 IO。
# 👉 优化建议: 仅在设置面板关闭时批量保存一次，而不是每次滑块变化都保存。
var _config := ConfigFile.new()
const CONFIG_PATH := "user://settings.cfg"

var _resolutions := [
	Vector2i(1280, 720),
	Vector2i(1920, 1080),
	Vector2i(2560, 1440),
	Vector2i(3840, 2160)
]
var _framerates := [30, 60, 120, 144, -1]


func _ready() -> void:
	# 自动查找子节点
	if not tab_display_btn:
		tab_display_btn = get_node_or_null("TabBar/TabDisplayBtn")
	if not tab_controls_btn:
		tab_controls_btn = get_node_or_null("TabBar/TabControlsBtn")
	if not tab_game_btn:
		tab_game_btn = get_node_or_null("TabBar/TabGameBtn")
	if not display_panel:
		display_panel = get_node_or_null("DisplayPanel")
	if not controls_panel:
		controls_panel = get_node_or_null("ControlsPanel")
	if not game_panel:
		game_panel = get_node_or_null("GamePanel")
	if not resolution_dropdown and display_panel:
		resolution_dropdown = display_panel.get_node_or_null("ResolutionDropdown")
	if not window_mode_dropdown and display_panel:
		window_mode_dropdown = display_panel.get_node_or_null("WindowModeDropdown")
	if not framerate_dropdown and display_panel:
		framerate_dropdown = display_panel.get_node_or_null("FramerateDropdown")
	if not mouse_sensitivity_slider and controls_panel:
		mouse_sensitivity_slider = controls_panel.get_node_or_null("MouseSensitivitySlider")
	if not mouse_sensitivity_label and controls_panel:
		mouse_sensitivity_label = controls_panel.get_node_or_null("MouseSensitivityLabel")
	if not invert_y_toggle and controls_panel:
		invert_y_toggle = controls_panel.get_node_or_null("InvertYToggle")
	if not show_fps_toggle and game_panel:
		show_fps_toggle = game_panel.get_node_or_null("ShowFPSToggle")

	_config.load(CONFIG_PATH)
	_apply_startup_display_settings()

	if tab_display_btn:
		tab_display_btn.pressed.connect(func(): _switch_tab(display_panel))
	if tab_quality_btn:
		tab_quality_btn.pressed.connect(func(): _switch_tab(quality_panel))
	if tab_controls_btn:
		tab_controls_btn.pressed.connect(func(): _switch_tab(controls_panel))
	if tab_game_btn:
		tab_game_btn.pressed.connect(func(): _switch_tab(game_panel))

	_init_ui()
	_switch_tab(display_panel)


func _apply_startup_display_settings() -> void:
	var res_idx: int = _config.get_value("display", "resolution_index", 1)
	var mode_idx: int = _config.get_value("display", "window_mode_index", 2)
	var fps_idx: int = _config.get_value("display", "framerate_index", 1)

	var res: Vector2i = _resolutions[res_idx]
	var mode: DisplayServer.WindowMode = _get_mode_from_index(mode_idx)

	DisplayServer.window_set_mode(mode)
	DisplayServer.window_set_size(res)
	Engine.max_fps = _framerates[fps_idx]


func _get_mode_from_index(index: int) -> DisplayServer.WindowMode:
	match index:
		0: return DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN
		1: return DisplayServer.WINDOW_MODE_FULLSCREEN
		_: return DisplayServer.WINDOW_MODE_WINDOWED


func _init_ui() -> void:
	# 分辨率下拉
	if resolution_dropdown:
		resolution_dropdown.clear()
		resolution_dropdown.add_item("1280x720 (720p)")
		resolution_dropdown.add_item("1920x1080 (1080p)")
		resolution_dropdown.add_item("2560x1440 (2K)")
		resolution_dropdown.add_item("3840x2160 (4K)")
		resolution_dropdown.selected = _config.get_value("display", "resolution_index", 1)
		resolution_dropdown.item_selected.connect(_on_resolution_changed)

	# 窗口模式下拉
	if window_mode_dropdown:
		window_mode_dropdown.clear()
		window_mode_dropdown.add_item("Exclusive Fullscreen")
		window_mode_dropdown.add_item("Borderless Fullscreen")
		window_mode_dropdown.add_item("Windowed")
		window_mode_dropdown.selected = _config.get_value("display", "window_mode_index", 2)
		window_mode_dropdown.item_selected.connect(_on_window_mode_changed)

	# 帧率下拉
	if framerate_dropdown:
		framerate_dropdown.clear()
		framerate_dropdown.add_item("30 FPS")
		framerate_dropdown.add_item("60 FPS")
		framerate_dropdown.add_item("120 FPS")
		framerate_dropdown.add_item("144 FPS")
		framerate_dropdown.add_item("Unlimited")
		framerate_dropdown.selected = _config.get_value("display", "framerate_index", 1)
		framerate_dropdown.item_selected.connect(_on_framerate_changed)

	# 鼠标灵敏度
	if mouse_sensitivity_slider:
		var sens: float = _config.get_value("controls", "mouse_sensitivity", 0.5)
		mouse_sensitivity_slider.value = sens
		if mouse_sensitivity_label:
			mouse_sensitivity_label.text = "%.2f" % sens
		mouse_sensitivity_slider.value_changed.connect(_on_sensitivity_changed)

	# 反转Y轴
	if invert_y_toggle:
		invert_y_toggle.button_pressed = _config.get_value("controls", "invert_y", false)
		invert_y_toggle.toggled.connect(_on_invert_y_changed)

	# FPS 显示
	if show_fps_toggle:
		show_fps_toggle.button_pressed = _config.get_value("game", "show_fps", false)
		show_fps_toggle.toggled.connect(_on_show_fps_changed)


func _switch_tab(active_panel: Control) -> void:
	if display_panel:
		display_panel.visible = false
	if quality_panel:
		quality_panel.visible = false
	if controls_panel:
		controls_panel.visible = false
	if game_panel:
		game_panel.visible = false
	if active_panel:
		active_panel.visible = true


func _on_resolution_changed(index: int) -> void:
	_config.set_value("display", "resolution_index", index)
	_config.save(CONFIG_PATH)
	_apply_screen_change()


func _on_window_mode_changed(index: int) -> void:
	_config.set_value("display", "window_mode_index", index)
	_config.save(CONFIG_PATH)
	_apply_screen_change()


func _apply_screen_change() -> void:
	var res_idx: int = _config.get_value("display", "resolution_index", 1)
	var mode_idx: int = _config.get_value("display", "window_mode_index", 2)
	var res: Vector2i = _resolutions[res_idx]
	var mode: DisplayServer.WindowMode = _get_mode_from_index(mode_idx)
	DisplayServer.window_set_mode(mode)
	DisplayServer.window_set_size(res)


func _on_framerate_changed(index: int) -> void:
	_config.set_value("display", "framerate_index", index)
	_config.save(CONFIG_PATH)
	Engine.max_fps = _framerates[index]


func _on_sensitivity_changed(value: float) -> void:
	_config.set_value("controls", "mouse_sensitivity", value)
	_config.save(CONFIG_PATH)
	if mouse_sensitivity_label:
		mouse_sensitivity_label.text = "%.2f" % value


func _on_invert_y_changed(is_on: bool) -> void:
	_config.set_value("controls", "invert_y", is_on)
	_config.save(CONFIG_PATH)


func _on_show_fps_changed(is_on: bool) -> void:
	_config.set_value("game", "show_fps", is_on)
	_config.save(CONFIG_PATH)
