## keybind_row.gd
## 按键绑定 UI 行 - 支持交互式重绑定
## 迁移自: KeybindRow.cs (123 行)
## 附加到包含 Label + Button 的 HBoxContainer 上
extends HBoxContainer

# ⚠️ BOTTLENECK: Godot 没有 Unity InputAction.PerformInteractiveRebinding() 等价物。
# 这里完全手动实现交互式重绑定：
# 1. 进入"等待输入"状态
# 2. 监听下一个按键事件
# 3. 用 InputMap.action_erase_events() + InputMap.action_add_event() 替换绑定
# 4. 保存到 ConfigFile
# 这比 Unity 的方案代码更多，但功能完全等价。
# 👉 如果需要支持游戏手柄，需要额外处理 InputEventJoypadButton。

@export var action_name_label: Label
@export var binding_name_label: Label
@export var rebind_button: Button
@export var waiting_overlay: Control

var _action_name: String = ""
var _display_name: String = ""
var _is_waiting_for_input: bool = false

var _config := ConfigFile.new()
const CONFIG_PATH := "user://keybinds.cfg"


func initialize(action_name: String, display_name: String) -> void:
	_action_name = action_name
	_display_name = display_name

	if action_name_label:
		action_name_label.text = display_name

	_load_binding_override()
	_update_binding_display()

	if rebind_button:
		rebind_button.pressed.connect(_start_rebinding)

	if waiting_overlay:
		waiting_overlay.visible = false


func _update_binding_display() -> void:
	if binding_name_label == null or _action_name.is_empty():
		return

	var events := InputMap.action_get_events(_action_name)
	if events.size() > 0:
		binding_name_label.text = events[0].as_text()
	else:
		binding_name_label.text = "[未绑定]"


func _start_rebinding() -> void:
	_is_waiting_for_input = true
	if waiting_overlay:
		waiting_overlay.visible = true
	if rebind_button:
		rebind_button.disabled = true


func _input(event: InputEvent) -> void:
	if not _is_waiting_for_input:
		return

	# 过滤鼠标移动和鼠标位置
	if event is InputEventMouseMotion:
		return

	# ESC 键取消重绑定
	if event is InputEventKey and event.keycode == KEY_ESCAPE:
		_cancel_rebinding()
		get_viewport().set_input_as_handled()
		return

	# 接受键盘和鼠标按钮
	if (event is InputEventKey or event is InputEventMouseButton) and event.pressed:
		# 执行重绑定
		InputMap.action_erase_events(_action_name)
		InputMap.action_add_event(_action_name, event)

		_is_waiting_for_input = false
		if waiting_overlay:
			waiting_overlay.visible = false
		if rebind_button:
			rebind_button.disabled = false

		_update_binding_display()
		_save_binding_override(event)
		get_viewport().set_input_as_handled()


func _cancel_rebinding() -> void:
	_is_waiting_for_input = false
	if waiting_overlay:
		waiting_overlay.visible = false
	if rebind_button:
		rebind_button.disabled = false


func _save_binding_override(event: InputEvent) -> void:
	_config.load(CONFIG_PATH)
	# 存储事件的文本表示 (简化方案)
	_config.set_value("keybinds", _action_name, event.as_text())
	_config.save(CONFIG_PATH)


func _load_binding_override() -> void:
	if _config.load(CONFIG_PATH) != OK:
		return
	# 注意: 完整实现需要从文本反序列化为 InputEvent
	# 这里仅做框架演示，实际使用需要更完善的序列化方案
	# 👉 可以使用 InputEvent 的 store_string/from_string 或手动记录 keycode
