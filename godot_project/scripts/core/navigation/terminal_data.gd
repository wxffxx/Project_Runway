## terminal_data.gd
## 航站楼数据结构 — 存储布局、楼层、机位接入点等信息
class_name TerminalData

enum TerminalType {
	REGIONAL,       ## 支线航站楼 (小型, 2-4 机位)
	DOMESTIC,       ## 国内航站楼 (中型, 4-8 机位)
	INTERNATIONAL,  ## 国际航站楼 (大型, 8-16 机位, 全玻璃幕墙)
	CARGO           ## 货运航站楼 (单层, 大跨度)
}

## 各类型默认配置参数
const TYPE_CONFIG := {
	TerminalType.REGIONAL: {
		"display_name": "支线航站楼",
		"floor_height": 4.0,
		"max_floors": 2,
		"gate_spacing": 30.0,
		"wall_thickness": 0.3,
		"glass_sides": false,
	},
	TerminalType.DOMESTIC: {
		"display_name": "国内航站楼",
		"floor_height": 5.0,
		"max_floors": 3,
		"gate_spacing": 36.0,
		"wall_thickness": 0.4,
		"glass_sides": false,
	},
	TerminalType.INTERNATIONAL: {
		"display_name": "国际航站楼",
		"floor_height": 6.0,
		"max_floors": 4,
		"gate_spacing": 50.0,
		"wall_thickness": 0.5,
		"glass_sides": true,
	},
	TerminalType.CARGO: {
		"display_name": "货运航站楼",
		"floor_height": 8.0,
		"max_floors": 1,
		"gate_spacing": 40.0,
		"wall_thickness": 0.6,
		"glass_sides": false,
	},
}

var terminal_name: String
var terminal_type: int = TerminalType.DOMESTIC
var floor_count: int = 1
var footprint_min: Vector3          ## 占地矩形左下角 (世界坐标)
var footprint_max: Vector3          ## 占地矩形右上角 (世界坐标)
var gate_attachment_nodes: Array[PathNode] = []   ## 沿机坪侧边缘自动生成的机位接入点


func _init(p_name: String, p_type: int, p_floors: int, p_min: Vector3, p_max: Vector3) -> void:
	terminal_name = p_name
	terminal_type = p_type
	floor_count = p_floors
	footprint_min = p_min
	footprint_max = p_max
	gate_attachment_nodes = []


func get_width() -> float:
	return absf(footprint_max.x - footprint_min.x)


func get_depth() -> float:
	return absf(footprint_max.z - footprint_min.z)


func get_area() -> float:
	return get_width() * get_depth()


func get_center() -> Vector3:
	return (footprint_min + footprint_max) / 2.0


func get_config() -> Dictionary:
	return TYPE_CONFIG.get(terminal_type, TYPE_CONFIG[TerminalType.DOMESTIC])


func get_total_height() -> float:
	var config := get_config()
	return float(config["floor_height"]) * floor_count
