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
builder.add_material("chassis", 0.95, 0.75, 0.1) # Bright industrial yellow
builder.add_material("glass", 0.1, 0.1, 0.15)
builder.add_material("wheel", 0.2, 0.2, 0.2)
builder.add_material("hitch", 0.4, 0.4, 0.42)
builder.add_material("bumper", 0.1, 0.1, 0.1)

# --- PUSHBACK TRACTOR DESIGN ---
# Characteristics: Extremely low profile, very heavy, wide base. Designed to slip under the nose of planes.
# Body: 7m long, 3.5m wide, 1m high (base main frame).

# Central Chassis
builder.set_material("chassis")
builder.add_box(0, 0.8, 0, 3.5, 1.0, 7.0)

# Bumpers at front and back (black/dark rubber)
builder.set_material("bumper")
builder.add_box(0, 0.8, 3.55, 3.6, 0.8, 0.3)  # Front Bumper
builder.add_box(0, 0.8, -3.55, 3.6, 0.8, 0.3) # Rear Bumper

# The Low-Profile Cabin (typically mounted at the front corner, or centered, very flat)
builder.set_material("chassis")
builder.add_box(0, 1.8, 2.0, 2.5, 1.0, 2.0) # Cabin frame
builder.set_material("glass")
builder.add_box(0, 1.8, 2.1, 2.3, 0.8, 1.9) # Cabin windows (almost flush with frame)

# Tow Hitches (used to connect tow bar to the aircraft)
builder.set_material("hitch")
builder.add_box(0, 0.6, 3.8, 0.5, 0.3, 0.6)  # Front Hitch
builder.add_box(0, 0.6, -3.8, 0.5, 0.3, 0.6) # Rear Hitch

# Massive Heavy Duty Wheels
# Wheels are deeply embedded into the chassis
builder.set_material("wheel")
# Front Left & Right (z = +2)
builder.add_box(1.7, 0.6, 2.2, 0.8, 1.2, 1.2)
builder.add_box(-1.7, 0.6, 2.2, 0.8, 1.2, 1.2)
# Rear Left & Right (z = -2)
builder.add_box(1.7, 0.6, -2.2, 0.8, 1.2, 1.2)
builder.add_box(-1.7, 0.6, -2.2, 0.8, 1.2, 1.2)

builder.save("../assets/models/pushback_tractor.obj", "../assets/models/pushback_tractor.mtl")
print("Pushback tractor generated.")
