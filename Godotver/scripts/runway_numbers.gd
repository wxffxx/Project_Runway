@tool
extends Node3D

@export_range(1, 36) var runway_heading: int = 9:
	set(val):
		runway_heading = val
		_generate_numbers()

@export var letter_spacing: float = 3.5
@export var voxel_size: float = 0.8  # Size of each pixel block

# 3x5 voxel grid for digits (1 means draw a box)
const DIGITS = {
	0: [1,1,1, 1,0,1, 1,0,1, 1,0,1, 1,1,1],
	1: [0,1,0, 1,1,0, 0,1,0, 0,1,0, 1,1,1],
	2: [1,1,1, 0,0,1, 1,1,1, 1,0,0, 1,1,1],
	3: [1,1,1, 0,0,1, 1,1,1, 0,0,1, 1,1,1],
	4: [1,0,1, 1,0,1, 1,1,1, 0,0,1, 0,0,1],
	5: [1,1,1, 1,0,0, 1,1,1, 0,0,1, 1,1,1],
	6: [1,1,1, 1,0,0, 1,1,1, 1,0,1, 1,1,1],
	7: [1,1,1, 0,0,1, 0,0,1, 0,0,1, 0,0,1],
	8: [1,1,1, 1,0,1, 1,1,1, 1,0,1, 1,1,1],
	9: [1,1,1, 1,0,1, 1,1,1, 0,0,1, 1,1,1]
}

func _ready() -> void:
	_generate_numbers()

func _generate_numbers() -> void:
	if not is_inside_tree():
		return
		
	# Clear previously generated boxes
	for child in get_children():
		child.queue_free()
	
	var heading_str = str(runway_heading).pad_zeros(2)
	
	# Calculate total width to center the numbers
	var bg_width = (heading_str.length() * letter_spacing)
	var start_x = -bg_width / 2.0 + (letter_spacing / 2.0)
	
	var mat = StandardMaterial3D.new()
	mat.albedo_color = Color(0.95, 0.95, 0.95)
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED # Keep flat pixel style
	
	var i = 0
	for char in heading_str:
		var digit = int(char)
		var grid = DIGITS.get(digit, DIGITS[0])
		var px = start_x + (i * letter_spacing)
		
		# Draw the digit using the 3x5 grid
		for row in range(5):
			for col in range(3):
				if grid[row * 3 + col] == 1:
					var box = CSGBox3D.new()
					# Elongate the block along Z-axis like real runway markings (perspective stretch)
					box.size = Vector3(voxel_size, 0.1, voxel_size * 2.0)
					
					var vx = px + (col * voxel_size) - (1.5 * voxel_size)
					var vz = (row * box.size.z) - (2.5 * box.size.z)
					
					box.position = Vector3(vx, 0.0, vz)
					box.material_override = mat
					add_child(box)
		i += 1
