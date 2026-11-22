"""Create the first Eclipse inspection building and its Unity-ready FBX.

Run from the Unity project root with Blender 4.x:
    blender --background --factory-startup --python Blender/BuildingInspection/generate_building.py

The exported asset uses meters, Z-up source coordinates, applied transforms,
beveled hard-surface parts, and named materials for deterministic Unity setup.
"""
import bpy
import math
import os
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Resources/Inspection"
OUT.mkdir(parents=True, exist_ok=True)
PREVIEW_PATH = Path(__file__).resolve().parent / "BuildingInspection_Preview.png"

def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

def material(name, color, metallic=0.0, roughness=0.45, emission=None, emission_strength=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1)
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    return m

CERAMIC = material("Ceramic | warm titanium", (0.47, 0.62, 0.70), 0.58, 0.28)
CARBON = material("Carbon | midnight composite", (0.025, 0.055, 0.095), 0.35, 0.34)
GLASS = material("Glass | deep arctic blue", (0.018, 0.15, 0.25), 0.68, 0.16)
WINDOW = material("Window | illuminated cyan", (0.025, 0.34, 0.48), 0.28, 0.22, (0.015, 0.40, 0.68), 0.65)
WINDOW_WARM = material("Window | warm interior", (0.62, 0.25, 0.08), 0.18, 0.28, (0.95, 0.24, 0.045), 0.45)
SIGNAL = material("Signal | cyan ceramic light", (0.012, 0.55, 0.78), 0.2, 0.23, (0.015, 0.58, 0.95), 1.4)
GOLD = material("Signal | amber light", (0.70, 0.22, 0.055), 0.2, 0.24, (0.95, 0.16, 0.025), 0.8)
PAVING = material("Foundation | basalt", (0.06, 0.10, 0.15), 0.25, 0.62)

def finish(obj, mat, bevel=0.0, smooth=False):
    obj.data.materials.append(mat)
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    if bevel > 0:
        mod = obj.modifiers.new("Machined edge radius", "BEVEL")
        mod.width = bevel
        mod.segments = 3
        mod.limit_method = "ANGLE"
        norm = obj.modifiers.new("Weighted face normals", "WEIGHTED_NORMAL")
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.ops.object.modifier_apply(modifier=norm.name)
    return obj

def box(name, loc, dims, mat, bevel=0.06, rot=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    obj.rotation_euler[2] = rot
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, mat, min(bevel, min(dims) * 0.22))

def cylinder(name, loc, radius, depth, mat, vertices=16, bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, mat, bevel)

def torus(name, loc, major, minor, mat, major_segments=64, minor_segments=8):
    bpy.ops.mesh.primitive_torus_add(major_segments=major_segments, minor_segments=minor_segments,
        location=loc, major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, mat, 0.0, True)

def tapered_core():
    # Faceted tower body with a small, deliberate lean and stepped setbacks.
    rings = [(5.2, 13.8, 11.6, -0.3), (18.0, 13.8, 11.6, -0.15),
             (31.0, 12.4, 10.6, 0.0), (43.0, 10.8, 9.6, 0.28),
             (52.0, 9.6, 8.4, 0.42)]
    verts = []
    for z, w, d, sx in rings:
        verts += [(sx-w/2, -d/2, z), (sx+w/2, -d/2, z),
                  (sx+w/2, d/2, z), (sx-w/2, d/2, z)]
    faces = [(0, 3, 2, 1)]
    for r in range(len(rings)-1):
        a, b = r*4, (r+1)*4
        for i in range(4): faces.append((a+i, a+(i+1)%4, b+(i+1)%4, b+i))
    a = (len(rings)-1)*4
    faces.append((a, a+1, a+2, a+3))
    mesh = bpy.data.meshes.new("Spire core | faceted shell")
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(GLASS)
    obj = bpy.data.objects.new("01 | faceted arctic glass tower", mesh)
    bpy.context.collection.objects.link(obj)
    bevel = obj.modifiers.new("Crisp panel edge glints", "BEVEL")
    bevel.width = 0.10
    bevel.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    normal = obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    bpy.ops.object.modifier_apply(modifier=normal.name)

def create_building():
    # Wide, grounded podium with layered overhangs and a readable entry front.
    box("00 | monolithic foundation", (0, 0, 1.1), (34, 30, 2.2), PAVING, 0.42)
    box("00 | lower titanium plinth", (0, 0, 2.3), (30, 26, 1.0), CERAMIC, 0.22)
    box("00 | shadow reveal", (0, 0, 3.05), (27.8, 23.8, 0.42), CARBON, 0.10)
    box("00 | podium upper deck", (0, 0, 4.0), (29.0, 25.0, 1.8), CERAMIC, 0.30)
    box("00 | entry glass recess", (0, -12.60, 5.55), (8.0, 0.22, 2.3), CARBON, 0.10)
    for x in (-3.0, -1.0, 1.0, 3.0):
        box("00 | entry luminous mullion", (x, -12.76, 5.55), (0.08, 0.09, 1.9), SIGNAL, 0.025)

    tapered_core()

    # Slender vertical exoskeleton fins give the silhouette depth at every angle.
    fin_heights = (48, 44, 39, 34)
    corners = [(-1,-1), (1,-1), (1,1), (-1,1)]
    for idx, (sx, sy) in enumerate(corners):
        h = fin_heights[idx]
        x = sx * (6.1 - idx * 0.20)
        y = sy * (5.0 - idx * 0.16)
        box(f"02 | structural corner blade {idx+1}", (x, y, 5.4+h/2),
            (0.72, 0.72, h), CERAMIC, 0.18, rot=(idx%2)*0.12)
        box(f"02 | blade inset signal {idx+1}", (x, y+sy*0.39, 7.0+h*0.48),
            (0.12, 0.10, h*0.48), SIGNAL if idx%2==0 else GOLD, 0.035)

    # Recessed horizontal floor bands and window bays on all four faces.
    floor_z = [9.0, 13.2, 17.4, 21.6, 25.8, 30.0, 34.2, 38.4, 42.6, 46.8]
    for i, z in enumerate(floor_z):
        width = 13.8 - max(0, z-18.0)*0.11
        depth = 11.6 - max(0, z-18.0)*0.08
        box(f"03 | floating floor band {i+1}", (0, 0, z), (width+1.0, depth+0.9, 0.28), CERAMIC, 0.06)
        box(f"03 | front edge light {i+1}", (0, -depth/2-0.48, z+0.17),
            (width*0.82, 0.075, 0.06), SIGNAL if i%3 else GOLD, 0.02)

        sx = -0.20 + max(0, z-18.0)*0.018
        count = 5 if z < 30 else 4
        gap = width/(count+1)
        for j in range(count):
            wx = sx - width/2 + gap*(j+1)
            wmat = WINDOW_WARM if (i*7+j*3)%13 == 0 else WINDOW
            box(f"04 | front inset glass {i+1}-{j+1}", (wx, -depth/2-0.03, z+2.0),
                (1.28 if count==5 else 1.34, 0.10, 2.25), wmat, 0.055)
            box(f"04 | rear inset glass {i+1}-{j+1}", (wx, depth/2+0.03, z+2.0),
                (1.28 if count==5 else 1.34, 0.10, 2.25), WINDOW, 0.055)

        side_count = 4 if z < 30 else 3
        side_gap = depth/(side_count+1)
        for side in (-1,1):
            for j in range(side_count):
                wy = -depth/2 + side_gap*(j+1)
                box(f"04 | side inset glass {i+1}-{side}-{j+1}",
                    (side*(width/2+0.03), wy, z+2.0), (0.10, 1.35, 2.25),
                    WINDOW_WARM if (i*5+j)%17==0 else WINDOW, 0.05)

    # Three asymmetrical observation decks and ribbed undersides.
    for i, (z, w, d, angle) in enumerate([(16.0, 20.5, 15.0, -0.045),
                                         (29.0, 18.0, 14.0, 0.055),
                                         (41.0, 15.0, 12.8, -0.035)]):
        box(f"05 | cantilever deck {i+1}", (0.5*(i-1), 0, z), (w, d, 0.76), CARBON, 0.16, rot=angle)
        box(f"05 | deck armor cap {i+1}", (0.5*(i-1), 0, z+0.40), (w-0.8, d-0.8, 0.14), CERAMIC, 0.055, rot=angle)
        for x in (-w*0.34, 0, w*0.34):
            box(f"05 | deck support rib {i+1}", (x, d*0.38, z-0.44), (0.22, 1.2, 0.75), CERAMIC, 0.05)
        box(f"05 | deck perimeter light {i+1}", (0, -d/2+0.10, z+0.51), (w*0.68, 0.08, 0.075), SIGNAL if i!=1 else GOLD, 0.02)

    # Sculpted crown: dark cap, raised glowing annulus, and three tuned blades.
    box("06 | crown shoulder", (0.5, 0, 52.8), (13.2, 11.0, 1.5), CARBON, 0.24)
    box("06 | crown armor", (0.5, 0, 53.8), (11.8, 9.6, 0.65), CERAMIC, 0.14)
    torus("06 | luminous orbital crown", (0.5, 0, 55.0), 6.6, 0.20, SIGNAL, 72, 10)
    torus("06 | inner crown ring", (0.5, 0, 55.0), 4.4, 0.10, GOLD, 64, 8)
    for i, x in enumerate((-3.5, 0.5, 4.5)):
        blade = box(f"06 | crown fin {i+1}", (x, 0, 58.2), (0.42, 0.54, 5.0 if i==1 else 3.3), CERAMIC, 0.08)
        blade.rotation_euler[1] = -0.12 if i==0 else (0.12 if i==2 else 0)
        box(f"06 | crown fin light {i+1}", (x, -0.30, 58.3), (0.09, 0.08, 2.2), SIGNAL, 0.025)
    cylinder("06 | crown beacon", (0.5, 0, 61.3), 0.35, 1.1, GOLD, 12, 0.08)

    # Pedestrian scale and site detail around the podium.
    for i, x in enumerate((-12.0, 12.0)):
        box(f"07 | approach bollard base {i+1}", (x, -16.8, 0.9), (0.65, 0.65, 1.8), CARBON, 0.12)
        cylinder(f"07 | approach lamp {i+1}", (x, -16.8, 1.9), 0.22, 0.30, GOLD, 12, 0.04)
    for i in range(6):
        x = -10 + i*4
        box(f"07 | front foundation reveal {i+1}", (x, -15.06, 2.35), (2.2, 0.08, 0.08), SIGNAL if i in (1,4) else GOLD, 0.025)

def merge_named_material_meshes():
    # Reduce the FBX object count while preserving separate material groups.
    objects = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    names = sorted({o.data.materials[0].name if o.data.materials else "unassigned" for o in objects})
    merged = []
    for name in names:
        group = [o for o in bpy.context.scene.objects if o.type=="MESH" and o.data.materials
                 and o.data.materials[0].name==name]
        if len(group) == 1:
            merged.append(group[0])
            continue
        bpy.ops.object.select_all(action="DESELECT")
        for o in group: o.select_set(True)
        bpy.context.view_layer.objects.active = group[0]
        bpy.ops.object.join()
        joined = bpy.context.object
        joined.name = "MaterialGroup | " + name
        merged.append(joined)
    bpy.ops.object.select_all(action="DESELECT")
    for o in merged:
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return merged

def render_preview():
    bpy.ops.mesh.primitive_plane_add(size=250, location=(0,0,0))
    ground = bpy.context.object
    ground.name = "Preview only | ground"
    ground.data.materials.append(PAVING)
    bpy.ops.object.camera_add(location=(82,-112,74))
    camera = bpy.context.object
    camera.data.lens = 52
    direction = Vector((0,0,30)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera
    for name, loc, energy, size in [("Key", (25,-55,100), 7200, 9),
                                    ("Rim", (-60,25,60), 4800, 12),
                                    ("Fill", (55,45,42), 2600, 15)]:
        bpy.ops.object.light_add(type="AREA", location=loc)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        light.rotation_euler = (Vector((0,0,28))-light.location).to_track_quat("-Z", "Y").to_euler()
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 20
    scene.cycles.use_denoising = False
    scene.render.resolution_x = 960
    scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = "AgX"
    scene.render.filepath = str(PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)
    # Preview-only ground, camera and lights stay out of the exported asset.
    bpy.data.objects.remove(ground, do_unlink=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    for o in list(bpy.context.scene.objects):
        if o.type == "LIGHT": bpy.data.objects.remove(o, do_unlink=True)

def export_unity_obj():
    meshes = merge_named_material_meshes()
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.wm.obj_export(filepath=str(OUT / "HaloSpire.obj"), export_selected_objects=False,
        export_materials=True, export_uv=True, export_normals=True,
        export_triangulated_mesh=True, path_mode="AUTO",
        forward_axis="NEGATIVE_Z", up_axis="Y")

clear()
create_building()
render_preview()
export_unity_obj()
print("INSPECTION_BUILDING_OK", OUT / "HaloSpire.obj")
