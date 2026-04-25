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
builder.add_material("stripe", 0.1, 0.4, 0.8)    
builder.add_material("glass", 0.1, 0.1, 0.15)    
builder.add_material("prop_wheel", 0.2, 0.2, 0.2)

builder.set_material("body")
builder.add_box(0, 1.5, 0, 2.0, 1.5, 8.0)
builder.add_box(0, 1.4, 4.5, 1.6, 1.2, 1.0)
builder.set_material("glass")
builder.add_box(0, 2.6, 1.5, 1.8, 0.8, 1.2)
builder.add_box(0, 2.6, 0.0, 2.05, 0.8, 1.8)
builder.set_material("body")
builder.add_box(0, 3.2, 0.5, 2.0, 0.4, 3.0)
builder.set_material("stripe")
builder.add_box(0, 1.5, -2, 2.1, 0.4, 4)
builder.set_material("body")
builder.add_box(0, 3.1, 0.5, 12.0, 0.3, 2.0)
builder.add_box(0, 2.5, -3.5, 0.4, 2.0, 1.5)
builder.set_material("stripe")
builder.add_box(0, 3.0, -3.5, 0.45, 0.5, 1.5)
builder.set_material("body")
builder.add_box(0, 1.5, -3.8, 4.0, 0.2, 1.2)
builder.set_material("prop_wheel")
builder.add_box(0, 1.4, 5.2, 0.4, 0.4, 0.4)
builder.add_box(0, 1.4, 5.2, 0.2, 3.0, 0.1)
builder.set_material("body")
builder.add_box(1.0, 0.75, 1.0, 0.2, 1.0, 0.2)
builder.add_box(-1.0, 0.75, 1.0, 0.2, 1.0, 0.2)
builder.add_box(0, 0.75, 4.0, 0.2, 1.0, 0.2)
builder.set_material("prop_wheel")
builder.add_box(1.2, 0.3, 1.0, 0.4, 0.6, 0.6)
builder.add_box(-1.2, 0.3, 1.0, 0.4, 0.6, 0.6)
builder.add_box(0.0, 0.3, 4.0, 0.3, 0.5, 0.5)

builder.save("../assets/models/small_airplane.obj", "../assets/models/small_airplane.mtl")
