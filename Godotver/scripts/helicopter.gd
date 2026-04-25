extends Node3D

@export var engine_on: bool = true
@export var main_rotor_speed: float = 15.0
@export var tail_rotor_speed: float = 20.0

@onready var main_rotor: Node3D = $MainRotor # 请确保主旋翼的MeshInstance节点名字叫 MainRotor
@onready var tail_rotor: Node3D = $TailRotor # 请确保尾旋翼的MeshInstance节点名字叫 TailRotor

func _process(delta: float) -> void:
	if engine_on:
		if main_rotor:
			# 绕Y轴旋转主旋翼
			main_rotor.rotate_y(main_rotor_speed * delta)
		
		if tail_rotor:
			# 绕X轴旋转尾旋翼
			tail_rotor.rotate_x(tail_rotor_speed * delta)
