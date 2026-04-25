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
builder.add_material("stripe", 0.1, 0.5, 0.8) # Azure / Cyan    
builder.add_material("glass", 0.1, 0.1, 0.15)    
builder.add_material("door", 0.25, 0.25, 0.25)
builder.add_material("wheel", 0.1, 0.1, 0.1)
builder.add_material("bumper", 0.15, 0.15, 0.15)
builder.add_material("grille", 0.05, 0.05, 0.05)

# --- APRON SHUTTLE BUS (Cobus Style) ---
# Extra wide, super low to the ground, long.

# 1. Main Body
builder.set_material("body")
# Center y slightly raised, length 14m, width 3.3m, height 2.8m
builder.add_box(0, 1.6, 0, 3.3, 2.8, 14.0)

# 2. Roof Air Conditioning Pods (Standard on shuttle buses)
builder.add_box(0, 3.15, -2.0, 2.2, 0.3, 4.0)
builder.add_box(0, 3.15, 4.0, 2.0, 0.3, 2.5)

# 3. Decorative Top Stripe
builder.set_material("stripe")
builder.add_box(0, 2.8, 0, 3.35, 0.3, 14.1)

# 4. Bumpers & Grille
builder.set_material("bumper")
builder.add_box(0, 0.4, 7.05, 3.35, 0.5, 0.2) # Front
builder.add_box(0, 0.4, -7.05, 3.35, 0.5, 0.2) # Rear
builder.set_material("grille")
builder.add_box(0, 0.8, 7.05, 1.8, 0.3, 0.1) # Small front cooling grille

# 5. Panoramic Windows
builder.set_material("glass")
# Huge front windshield covering almost the whole face
builder.add_box(0, 1.8, 7.02, 3.1, 1.6, 0.1)
# Wide rear window
builder.add_box(0, 1.8, -7.02, 3.0, 1.2, 0.1)

# Side window strips (spanning entire length except doors and structural poles)
# Left and Right
builder.add_box(-1.66, 1.7, 0, 0.1, 1.2, 13.0)
builder.add_box(1.66, 1.7, 0, 0.1, 1.2, 13.0)

# 6. Sliding Wide Doors (3 sets on both sides)
builder.set_material("door")
# Side doors are very wide and tall
for d_z in [-4.0, 0.0, 4.0]:
    builder.add_box(-1.68, 1.5, d_z, 0.15, 2.4, 1.8) # Left doors
    builder.add_box(1.68, 1.5, d_z, 0.15, 2.4, 1.8)  # Right doors
    
    # Door glass
    builder.set_material("glass")
    builder.add_box(-1.70, 1.6, d_z, 0.1, 1.8, 1.4)
    builder.add_box(1.70, 1.6, d_z, 0.1, 1.8, 1.4)
    builder.set_material("door")

# 7. Wheels & Skirts
builder.set_material("wheel")
for w_z in [-5.0, 5.0]:
    # Very low ground clearance, so wheels are small and inside
    builder.add_box(-1.5, 0.4, w_z, 0.5, 0.8, 1.2)
    builder.add_box(1.5, 0.4, w_z, 0.5, 0.8, 1.2)

builder.save("../assets/models/shuttle_bus.obj", "../assets/models/shuttle_bus.mtl")
print("Shuttle bus generated.")
