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
        # 8 vertices for a box, cx,cy,cz is the center
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
        for i in range(8):
            self.vertices.append(v[i])
        
        # 6 faces
        f = [
            (cv+1, cv+2, cv+3, cv+4, self.current_material), # Front
            (cv+2, cv+6, cv+7, cv+3, self.current_material), # Right
            (cv+6, cv+5, cv+8, cv+7, self.current_material), # Back
            (cv+5, cv+1, cv+4, cv+8, self.current_material), # Left
            (cv+4, cv+3, cv+7, cv+8, self.current_material), # Top
            (cv+5, cv+6, cv+2, cv+1, self.current_material), # Bottom
        ]
        self.faces.extend(f)
        self.v_count += 8

    def save(self, obj_path, mtl_path):
        mtl_filename = os.path.basename(mtl_path)
        
        with open(mtl_path, 'w') as f:
            for name, (r, g, b) in self.materials.items():
                f.write(f"newmtl {name}\n")
                # Add specific properties to ensure Godot renders it completely flat
                f.write(f"Kd {r} {g} {b}\n")
                f.write(f"Ks 0.0 0.0 0.0\n") 
                f.write(f"Ns 0\n\n")
                
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
builder.add_material("concrete", 0.7, 0.7, 0.7)
builder.add_material("base_stripe", 0.3, 0.3, 0.3)
builder.add_material("glass", 0.2, 0.6, 0.8)
builder.add_material("roof", 0.2, 0.2, 0.2)
builder.add_material("antenna_red", 0.9, 0.1, 0.1)
builder.add_material("antenna_white", 0.9, 0.9, 0.9)

# Build using large boxes to prevent internal shadowing/grid lines

# Base (Width 3x3, Height 10)
# We recreate the stripes using tall boxes
y_offset = 0
for stripe in range(4):
    # Dark stripe (h=1)
    builder.set_material("base_stripe")
    builder.add_box(0, y_offset + 0.5, 0, 3, 1, 3)
    y_offset += 1
    
    # Light stripe (h=2)
    if stripe < 3: # don't add the last light stripe to keep height = 10
        builder.set_material("concrete")
        builder.add_box(0, y_offset + 1, 0, 3, 2, 3)
        y_offset += 2

# Control Room Floor (Platform, 5x5, h=1)
builder.set_material("roof")
builder.add_box(0, 10 + 0.5, 0, 5, 1, 5)

# Control Room Glass (4x4, h=1)
builder.set_material("glass")
builder.add_box(0, 11 + 0.5, 0, 4, 1, 4)

# Control Room Roof (5x5, h=1)
builder.set_material("roof")
builder.add_box(0, 12 + 0.5, 0, 5, 1, 5)

# Antenna base (1x1, h=2)
builder.set_material("concrete")
builder.add_box(0, 13 + 1, 0, 1, 2, 1)

# Antenna top rings
for i in range(4):
    if i % 2 == 0:
        builder.set_material("antenna_red")
    else:
        builder.set_material("antenna_white")
    builder.add_box(0, 15 + i + 0.5, 0, 1, 1, 1)

# Save
builder.save("../assets/models/tower.obj", "../assets/models/tower.mtl")
print("Successfully generated optimized tower.obj")
