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
builder.add_material("chassis", 0.95, 0.95, 0.95)
builder.add_material("glass", 0.1, 0.1, 0.15)
builder.add_material("wheel", 0.2, 0.2, 0.2)
builder.add_material("step", 0.3, 0.3, 0.35)
builder.add_material("rail", 0.1, 0.4, 0.85)
builder.add_material("bumper", 0.1, 0.1, 0.1)

# Base chassis
builder.set_material("chassis")
builder.add_box(0, 0.8, 0, 2.6, 0.6, 6.0)

# Bumpers
builder.set_material("bumper")
builder.add_box(0, 0.8, 3.1, 2.8, 0.4, 0.2)
builder.add_box(0, 0.8, -3.1, 2.8, 0.4, 0.2)

# Wheels
builder.set_material("wheel")
for z in [-2.0, 2.0]:
    builder.add_box(-1.3, 0.5, z, 0.5, 1.0, 1.0)
    builder.add_box(1.3, 0.5, z, 0.5, 1.0, 1.0)

# Driver Cabin (Front of truck, tucked under the stairs)
builder.set_material("chassis")
builder.add_box(0, 1.6, 2.0, 2.2, 1.0, 1.6)
# Cabin Glass
builder.set_material("glass")
builder.add_box(0, 2.2, 2.0, 2.3, 0.8, 1.7)

# Main Stairs Assembly
# Stairs start LOW at the rear (z = -3.5) and rise HIGH towards the front (z = 0.5)
z_start = -3.5
z_end = 0.5
y_start = 1.3
y_end = 4.5
steps = 15

builder.set_material("step")
for i in range(steps):
    t = i / float(steps - 1)
    sz = z_start + (t * (z_end - z_start))
    sy = y_start + (t * (y_end - y_start))
    builder.add_box(0, sy, sz, 2.0, 0.15, 0.4)

# Side Panels / Guard Rails for the stairs
builder.set_material("rail")
builder.add_box(-1.2, (y_start+y_end)/2.0 + 0.5, (z_start+z_end)/2.0, 0.2, 1.0, z_end - z_start)
builder.add_box(1.2, (y_start+y_end)/2.0 + 0.5, (z_start+z_end)/2.0, 0.2, 1.0, z_end - z_start)

# Top Platform for boarding the plane (Overhangs the front cabin!)
platform_z = 2.0 # Central point of platform, extending from z=0.5 to z=3.5
platform_width = 2.4
platform_length = 3.0
builder.set_material("step")
builder.add_box(0, y_end, platform_z, platform_width, 0.2, platform_length)

# Top Platform safety rails
builder.set_material("rail")
# Side rails
builder.add_box(-1.1, y_end + 0.6, platform_z, 0.2, 1.0, platform_length)
builder.add_box(1.1, y_end + 0.6, platform_z, 0.2, 1.0, platform_length)
# Front extension buffers to kiss the plane
builder.set_material("bumper")
builder.add_box(-0.8, y_end + 0.1, platform_z + 1.6, 0.4, 0.2, 0.4)
builder.add_box(0.8, y_end + 0.1, platform_z + 1.6, 0.4, 0.2, 0.4)

builder.save("../assets/models/boarding_stairs.obj", "../assets/models/boarding_stairs.mtl")
print("Boarding stairs truck generated (reversed version).")
