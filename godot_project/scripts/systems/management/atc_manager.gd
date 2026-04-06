## atc_manager.gd
## 空中交通管制系统 (Autoload 单例)
## 负责对进出港航班进行逻辑分配和跑道防冲突锁定
## 迁移自: ATCManager.cs
extends Node


func request_landing_runway(flight_id: String) -> RunwayData:
	if AirportManager.active_runways.size() == 0:
		push_warning("[ATC] 无法为航班 %s 分配降落跑道，机场无可用跑道！" % flight_id)
		return null

	for runway in AirportManager.active_runways:
		if runway.current_status == NavigationEnums.RunwayStatus.FREE:
			runway.current_status = NavigationEnums.RunwayStatus.LANDING
			runway.active_flight_id = flight_id
			print("[ATC] 指派跑道 %s 供航班 %s 降落。" % [runway.runway_name, flight_id])
			return runway

	print("[ATC] 航班 %s 申请降落被拒绝，所有跑道均在忙碌中。" % flight_id)
	return null


func report_runway_vacated(runway_name: String, flight_id: String) -> void:
	for runway in AirportManager.active_runways:
		if runway.runway_name == runway_name and runway.active_flight_id == flight_id:
			runway.current_status = NavigationEnums.RunwayStatus.FREE
			runway.active_flight_id = ""
			print("[ATC] 航班 %s 已脱离跑道 %s，跑道重新开放。" % [flight_id, runway_name])
			return
