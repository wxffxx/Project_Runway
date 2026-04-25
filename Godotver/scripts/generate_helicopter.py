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

def spawn_materials(builder):
    builder.add_material("body_color", 0.9, 0.2, 0.2)
    builder.add_material("body_second", 0.9, 0.9, 0.9)
    builder.add_material("glass", 0.1, 0.1, 0.15)
    builder.add_material("rotor", 0.2, 0.2, 0.2)

# --- 1. Helicopter Body ---
b_body = VoxelBuilder()
spawn_materials(b_body)

b_body.set_material("body_color")
b_body.add_box(0, 1.8, 0, 2.0, 2.0, 4.5)
b_body.set_material("glass")
b_body.add_box(0, 2.1, 2.2, 1.8, 1.0, 1.0)
b_body.add_box(0, 2.1, 0, 2.1, 0.8, 3.0)
b_body.set_material("body_second")
b_body.add_box(0, 2.2, -3.5, 0.6, 0.6, 5.0)
b_body.set_material("body_color")
b_body.add_box(0, 2.5, -5.5, 0.3, 2.0, 1.0)
b_body.set_material("rotor")
b_body.add_box(0.8, 0.5, 1.0, 0.2, 0.8, 0.2)
b_body.add_box(-0.8, 0.5, 1.0, 0.2, 0.8, 0.2)
b_body.add_box(0.8, 0.5, -1.0, 0.2, 0.8, 0.2)
b_body.add_box(-0.8, 0.5, -1.0, 0.2, 0.8, 0.2)
b_body.add_box(1.0, 0.1, 0, 0.3, 0.2, 5.0)
b_body.add_box(-1.0, 0.1, 0, 0.3, 0.2, 5.0)
b_body.add_box(0, 3.2, 0, 0.4, 0.8, 0.4) # rotor mast
b_body.save("../assets/models/helicopter_body.obj", "../assets/models/helicopter_body.mtl")

# --- 2. Main Rotor (Centered at Origin) ---
b_main = VoxelBuilder()
spawn_materials(b_main)
b_main.set_material("rotor")
# Center offset so pivot is correct
b_main.add_box(0, 0.0, 0, 8.0, 0.1, 0.4)
b_main.add_box(0, 0.0, 0, 0.4, 0.1, 8.0)
b_main.save("../assets/models/helicopter_main_rotor.obj", "../assets/models/helicopter_main_rotor.mtl")

# --- 3. Tail Rotor (Centered at Origin) ---
b_tail = VoxelBuilder()
spawn_materials(b_tail)
b_tail.set_material("rotor")
b_tail.add_box(-0.2, 0, 0, 0.6, 0.2, 0.2) # rotor motor hub
b_tail.add_box(0.0, 0, 0, 0.1, 1.8, 0.2) # blade 1
b_tail.add_box(0.0, 0, 0, 0.1, 0.2, 1.8) # blade 2
b_tail.save("../assets/models/helicopter_tail_rotor.obj", "../assets/models/helicopter_tail_rotor.mtl")

print("Helicopter split models generated.")
