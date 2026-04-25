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
builder.add_material("body", 0.95, 0.95, 0.95)
builder.add_material("belly_stripe", 0.3, 0.35, 0.5) 
builder.add_material("glass", 0.1, 0.1, 0.15)
builder.add_material("engine", 0.25, 0.25, 0.25)
builder.add_material("wing", 0.85, 0.85, 0.85)

# --- FUSELAGE ---
# Main body (Massive cylinder-like box) width=4.5, height=4.5, length=30, centered slightly back
builder.set_material("body")
builder.add_box(0, 4.5, 0, 4.2, 4.2, 30.0)

# Colored belly stripe to give it an airliner/cargo feel
builder.set_material("belly_stripe")
builder.add_box(0, 2.8, 0, 4.3, 0.8, 30.0)

# Nose section (tapering front)
builder.set_material("body")
builder.add_box(0, 4.3, 17.5, 3.8, 3.8, 5.0)
builder.add_box(0, 4.0, 21.0, 2.5, 2.5, 2.0)

# Nose Cockpit Glass
builder.set_material("glass")
builder.add_box(0, 5.8, 17.0, 3.9, 0.8, 2.5)

# Tail Cone (tapering back)
builder.set_material("body")
builder.add_box(0, 4.3, -17.5, 3.4, 3.4, 5.0)
builder.add_box(0, 4.6, -21.0, 2.0, 2.0, 2.0)

# --- WINGS ---
builder.set_material("wing")
# Main long wingspan
builder.add_box(0, 3.8, 2.0, 36.0, 0.6, 6.0)
# Swept back layered effect
builder.add_box(0, 3.8, -1.0, 28.0, 0.6, 4.0)
builder.add_box(0, 3.8, -3.0, 18.0, 0.6, 3.0)

# --- TAIL (EMPENNAGE) ---
# Vertical Stabilizer (layered for swept back effect)
builder.set_material("belly_stripe") # Tail base colored
builder.add_box(0, 7.5, -18.0, 0.6, 4.0, 7.0)
builder.add_box(0, 10.5, -19.5, 0.6, 4.0, 5.0)
builder.add_box(0, 13.5, -21.0, 0.6, 3.0, 3.0)

# Horizontal Stabilizers
builder.set_material("wing")
builder.add_box(0, 5.5, -19.5, 14.0, 0.4, 4.0)
builder.add_box(0, 5.5, -21.5, 10.0, 0.4, 2.0)

# --- ENGINES ---
# Left Engine
builder.set_material("engine")
builder.add_box(-9.0, 2.2, 4.0, 2.6, 2.6, 6.0) # Engine nacelle
builder.add_box(-9.0, 3.1, 3.0, 0.4, 1.0, 2.0) # Pylon connecting engine to wing
# Right Engine
builder.add_box(9.0, 2.2, 4.0, 2.6, 2.6, 6.0) # Engine nacelle
builder.add_box(9.0, 3.1, 3.0, 0.4, 1.0, 2.0) # Pylon connecting engine to wing

# --- LANDING GEAR ---
builder.set_material("engine")
# Main Landing Gear Struts (very thick for cargo plane)
builder.add_box(1.5, 1.2, 0, 0.5, 2.5, 1.0)
builder.add_box(-1.5, 1.2, 0, 0.5, 2.5, 1.0)
# Nose Gear Strut
builder.add_box(0, 1.2, 18.0, 0.4, 2.5, 0.4)

# Wheels
builder.set_material("engine") # dark grey/black
# Heavy multi-wheel bogeys (main)
builder.add_box(2.0, 0.5, 0, 1.6, 1.0, 3.0)
builder.add_box(-2.0, 0.5, 0, 1.6, 1.0, 3.0)
# Nose wheels
builder.add_box(0, 0.5, 18.0, 0.8, 1.0, 1.0)

builder.save("../assets/models/cargo_plane.obj", "../assets/models/cargo_plane.mtl")
print("Cargo plane generated.")
