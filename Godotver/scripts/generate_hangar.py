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
        for i in range(8):
            self.vertices.append(v[i])
        
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
builder.add_material("wall", 0.85, 0.85, 0.88)
builder.add_material("roof", 0.25, 0.35, 0.45)
builder.add_material("stripe", 0.9, 0.7, 0.1)

# Back Wall
builder.set_material("wall")
builder.add_box(0, 4, -7, 20, 8, 1)

# Left Wall
builder.add_box(-9.5, 4, 0, 1, 8, 13)

# Right Wall
builder.add_box(9.5, 4, 0, 1, 8, 13)

# Stepped Arch Roof (to simulate a rounded hangar roof in voxel style)
builder.set_material("roof")
builder.add_box(0, 8.5, -0.5, 22, 1, 16)
builder.add_box(0, 9.5, -0.5, 18, 1, 16)
builder.add_box(0, 10.5, -0.5, 12, 1, 16)

# Doorway details - safety stripes at the front entrance
builder.set_material("stripe")
builder.add_box(-9.5, 4, 6.75, 1.2, 8, 0.5)
builder.add_box(9.5, 4, 6.75, 1.2, 8, 0.5)
builder.add_box(0, 7.75, 6.75, 18, 0.5, 0.5)

builder.save("../assets/models/hangar.obj", "../assets/models/hangar.mtl")
print("Hangar generated.")
