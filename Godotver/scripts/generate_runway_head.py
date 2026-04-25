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
builder.add_material("paint_white", 0.95, 0.95, 0.95)
builder.add_material("paint_yellow", 0.9, 0.7, 0.1)
builder.add_material("grass_pad", 0.2, 0.35, 0.2)
builder.add_material("pole", 0.4, 0.4, 0.4)
builder.add_material("light_red", 1.0, 0.1, 0.1)
builder.add_material("light_green", 0.1, 1.0, 0.1)
builder.add_material("light_white", 1.0, 1.0, 1.0)
builder.add_material("papi_box", 0.8, 0.4, 0.1) # orange box for PAPI

# RUNWAY THRESHOLD HEAD (Center is assumed around Z=0 for beginning of threshold, extends into +Z for runway, -Z for approach)

# 1. Base Pavement (Asphalt)
# Width = 40m, Length = 60m (just a segment)
builder.set_material("asphalt")
builder.add_box(0, 0.1, 30.0, 40.0, 0.2, 60.0)

# Optional dirt/grass approach pad
builder.set_material("grass_pad")
builder.add_box(0, 0.05, -15.0, 45.0, 0.1, 30.0)

# 2. Runway Edge Lines (Solid white lines along both sides)
builder.set_material("paint_white")
builder.add_box(-19.0, 0.25, 30.0, 0.6, 0.15, 60.0)
builder.add_box(19.0, 0.25, 30.0, 0.6, 0.15, 60.0)

# 3. "Piano Keys" Threshold Markings (Z = 2 to Z = 8)
# We draw ~12 thick stripes
keys_x = [-15, -12.5, -10, -7.5, -5, -2.5, 2.5, 5, 7.5, 10, 12.5, 15]
for x in keys_x:
    builder.add_box(x, 0.25, 5.0, 1.2, 0.15, 6.0)

# 4. Runway Number Markings (We can't easily draw letters, but we can draw blocks that mimic large numbers, like '0' '9')
# Left digit (0)
builder.add_box(-3.5, 0.25, 13.0, 0.6, 0.15, 4.0) # Left side
builder.add_box(-1.5, 0.25, 13.0, 0.6, 0.15, 4.0) # Right side
builder.add_box(-2.5, 0.25, 11.3, 2.0, 0.15, 0.6) # Top
builder.add_box(-2.5, 0.25, 14.7, 2.0, 0.15, 0.6) # Bottom
# Right digit (9)
builder.add_box(3.5, 0.25, 13.0, 0.6, 0.15, 4.0)  # Right side
builder.add_box(1.5, 0.25, 12.0, 0.6, 0.15, 2.0)  # Top Left side
builder.add_box(2.5, 0.25, 11.3, 2.0, 0.15, 0.6)  # Top
builder.add_box(2.5, 0.25, 13.0, 2.0, 0.15, 0.6)  # Middle
builder.add_box(2.5, 0.25, 14.7, 2.0, 0.15, 0.6)  # Bottom

# 5. Centerline (Dashed)
for z in range(20, 60, 6):
    builder.add_box(0, 0.25, z, 0.8, 0.15, 3.0)

# 6. Touchdown Zone Markings
# Sets of 3 bars, then 2 bars
# Z = 25 (3 bars)
for x in [-8, -10, -12]:
    builder.add_box(x, 0.25, 25.0, 1.0, 0.15, 5.0)
for x in [8, 10, 12]:
    builder.add_box(x, 0.25, 25.0, 1.0, 0.15, 5.0)
# Z = 40 (2 bars)
for x in [-8, -10]:
    builder.add_box(x, 0.25, 40.0, 1.0, 0.15, 5.0)
for x in [8, 10]:
    builder.add_box(x, 0.25, 40.0, 1.0, 0.15, 5.0)

# 7. Threshold Lights (End of pavement Row, emitting green facing approach)
# We make them visible blocks on the edge 
for x in range(-20, 21, 2):
    builder.set_material("pole")
    builder.add_box(x, 0.3, 0.0, 0.4, 0.4, 0.4)
    builder.set_material("light_green")
    builder.add_box(x, 0.3, -0.25, 0.3, 0.3, 0.1) # Green glass facing -Z

# 8. Approach Lighting System (ALS) poles in the dirt (Z: -5 to -25)
for z in [-5, -10, -15, -20, -25]:
    # Center Pole
    builder.set_material("pole")
    builder.add_box(0, 0.8, z, 0.2, 1.6, 0.2)
    # Crossbar
    builder.add_box(0, 1.6, z, 4.0, 0.2, 0.2)
    
    # 5 White lights on the crossbar
    builder.set_material("light_white")
    for lx in [-1.8, -0.9, 0, 0.9, 1.8]:
        builder.add_box(lx, 1.8, z, 0.4, 0.2, 0.4)

    # Red side-row lights for threshold wings (only next to the runway)
    if z >= -10:
        builder.set_material("pole")
        builder.add_box(-12, 0.6, z, 6.0, 0.2, 0.2)
        builder.add_box(12, 0.6, z, 6.0, 0.2, 0.2)
        builder.set_material("light_red")
        for lx in [-10, -12, -14]:
            builder.add_box(lx, 0.8, z, 0.4, 0.2, 0.4)
        for lx in [10, 12, 14]:
            builder.add_box(lx, 0.8, z, 0.4, 0.2, 0.4)

# 9. PAPI Lights (Left side at Z=30)
# 4 bright boxes
for i, px in enumerate([-23, -25, -27, -29]):
    builder.set_material("pole")
    builder.add_box(px, 0.4, 30.0, 0.2, 0.8, 0.2)
    builder.set_material("papi_box")
    builder.add_box(px, 0.8, 30.0, 1.2, 0.6, 0.8)
    
    # Normally 2 White 2 Red for "On Glide Path". Let's do exactly that.
    if i < 2:
        builder.set_material("light_white")
    else:
        builder.set_material("light_red")
    
    # Draw the light lens
    builder.add_box(px, 0.8, 29.5, 0.8, 0.4, 0.2) 

builder.save("../assets/models/runway_head.obj", "../assets/models/runway_head.mtl")
print("Runway head generated.")
