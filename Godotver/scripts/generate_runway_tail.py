import os

class VoxelBuilder:
    def __init__(self):
        self.vertices = []
        self.faces = []
        self.materials = {}
        self.current_material = "default"
        self.v_count = 0

    def add_material(self, name, r, g, b):
        self.materials[name] = (r, g, b)

    def set_material(self, name):
        self.current_material = name

    def add_box(self, cx, cy, cz, w, h, d):
        hx, hy, hz = w/2.0, h/2.0, d/2.0
        v = [
            (cx-hx, cy-hy, cz-hz),
            (cx+hx, cy-hy, cz-hz),
            (cx+hx, cy+hy, cz-hz),
            (cx-hx, cy+hy, cz-hz),
            (cx-hx, cy-hy, cz+hz),
            (cx+hx, cy-hy, cz+hz),
            (cx+hx, cy+hy, cz+hz),
            (cx-hx, cy+hy, cz+hz),
        ]
        cv = self.v_count
        self.vertices.extend(v)
        f = [
            (cv+1, cv+2, cv+3, cv+4, self.current_material),
            (cv+2, cv+6, cv+7, cv+3, self.current_material),
            (cv+6, cv+5, cv+8, cv+7, self.current_material),
            (cv+5, cv+1, cv+4, cv+8, self.current_material),
            (cv+4, cv+3, cv+7, cv+8, self.current_material),
            (cv+5, cv+6, cv+2, cv+1, self.current_material),
        ]
        self.faces.extend(f)
        self.v_count += 8

    def save(self, obj_path, mtl_path):
        mtl_filename = os.path.basename(mtl_path)
        with open(mtl_path, 'w') as f:
            for name, (r, g, b) in self.materials.items():
                f.write(f"newmtl {name}\nKd {r} {g} {b}\nKs 0.0 0.0 0.0\nNs 0\n\n")
        with open(obj_path, 'w') as f:
            f.write(f"mtllib {mtl_filename}\n")
            for v in self.vertices:
                f.write(f"v {v[0]} {v[1]} {v[2]}\n")
            current_mat = None
            for face in self.faces:
                if face[4] != current_mat:
                    current_mat = face[4]
                    f.write(f"usemtl {current_mat}\n")
                f.write(f"f {face[0]} {face[1]} {face[2]} {face[3]}\n")

builder = VoxelBuilder()
builder.add_material("asphalt", 0.15, 0.15, 0.15)
builder.add_material("stopway", 0.1, 0.1, 0.1) # Darker/rougher asphalt for stopway
builder.add_material("paint_white", 0.95, 0.95, 0.95)
builder.add_material("paint_yellow", 0.9, 0.7, 0.1)
builder.add_material("pole", 0.4, 0.4, 0.4)
builder.add_material("light_red", 1.0, 0.1, 0.1)
builder.add_material("light_white", 1.0, 1.0, 1.0)

# RUNWAY TAIL (Runway End)
# Asphalt runway segment (z = -20 to 40)
builder.set_material("asphalt")
builder.add_box(0, 0.1, 10.0, 40.0, 0.2, 60.0)

# Stopway / Overrun area (z = 40 to 60)
builder.set_material("stopway")
builder.add_box(0, 0.1, 50.0, 40.0, 0.2, 20.0)

# Centerline (Dashed) fading into the threshold
builder.set_material("paint_white")
for z in range(-20, 36, 6):
    builder.add_box(0, 0.25, z, 0.8, 0.15, 3.0)

# Runway Edge Lines
builder.add_box(-19.0, 0.25, 10.0, 0.6, 0.15, 60.0)
builder.add_box(19.0, 0.25, 10.0, 0.6, 0.15, 60.0)

# Yellow Chevrons (V shapes) on the overrun area 
# Chevron vertices point towards the runway end (at z=40)
builder.set_material("paint_yellow")
for cz in [44, 52]:
    # We build the V shape using small overlapping boxes steps since they are diagonal
    # Left wing of V
    for s in range(16):
        vx = -s 
        vz = cz + (s * 0.5) 
        builder.add_box(vx, 0.25, vz, 1.5, 0.15, 1.5)
    # Right wing of V
    for s in range(16):
        vx = s 
        vz = cz + (s * 0.5) 
        builder.add_box(vx, 0.25, vz, 1.5, 0.15, 1.5)

# Runway End Lights (Red lights facing approaching aircraft, at the boundary z=40)
# A row of bright red light boxes across the edge
for x in range(-19, 20, 3):
    builder.set_material("pole")
    builder.add_box(x, 0.3, 40.0, 0.4, 0.4, 0.4)
    builder.set_material("light_red")
    # Red light front lens (facing -z, towards incoming planes)
    builder.add_box(x, 0.3, 39.75, 0.3, 0.3, 0.1)

# ILS / Navigation Antenna array (often situated behind the runway end)
# Located around z = 55, simple metallic framework
builder.set_material("pole")
builder.add_box(0, 1.0, 58.0, 12.0, 0.2, 0.2) # Main crossbar
for pole_x in [-5, 0, 5]:
    builder.add_box(pole_x, 0.5, 58.0, 0.2, 1.0, 0.2) # Vertical poles

# Antenna radomes / panels
builder.set_material("light_white")
for rx in [-5, -2.5, 0, 2.5, 5]:
    builder.add_box(rx, 1.4, 58.0, 1.2, 0.8, 0.2)

builder.save("../assets/models/runway_tail.obj", "../assets/models/runway_tail.mtl")
print("Runway tail generated.")
