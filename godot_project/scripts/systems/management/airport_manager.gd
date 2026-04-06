## airport_manager.gd
## 全局机场管理器 (Autoload 单例)
## 负责统筹和存储玩家建造的所有关键设施数据
## 迁移自: AirportManager.cs
extends Node

var active_runways: Array[RunwayData] = []
var active_gates: Array[GateData] = []

@export var airport_name: String = "Player International Airport"
@export var icao_code: String = "ZBAA"


#region 设施注册 API

func register_runway(runway: RunwayData) -> void:
	if runway not in active_runways:
		active_runways.append(runway)
		print("[AirportManager] 跑道 %s 注册成功。当前总数: %d" % [runway.runway_name, active_runways.size()])


func register_gate(gate: GateData) -> void:
	if gate not in active_gates:
		active_gates.append(gate)
		print("[AirportManager] 机位 %s 注册成功。当前总数: %d" % [gate.gate_name, active_gates.size()])

#endregion


#region 设施查询 API

func get_available_gates(minimum_size: int) -> Array[GateData]:
	var available: Array[GateData] = []
	for gate in active_gates:
		if gate.supported_size >= minimum_size:
			available.append(gate)
	return available

#endregion
