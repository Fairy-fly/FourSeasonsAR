"""Run with Blender -b -P this_file to rebuild display models from the archived GLBs."""
from pathlib import Path
import sys
import bpy

root = Path(__file__).resolve().parents[2]
output = root / "UnityProject/Assets/Resources/Gallery"
blends = root / "derived/blender/hunyuan"
blends.mkdir(parents=True, exist_ok=True)
roles = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sorted(p.stem for p in (root/"assets/models/hunyuan").glob("*.glb"))
for role in roles:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.import_scene.gltf(filepath=str(root / "assets/models/hunyuan" / (role + ".glb")), merge_vertices=True)
    obj, = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.name = role
    mesh = obj.data
    original = len(mesh.polygons)
    modifier = obj.modifiers.new("Mobile silhouette", 'DECIMATE')
    modifier.ratio = min(1.0, 70000 / original)
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    if len(mesh.polygons) > 72000 or not mesh.uv_layers:
        raise RuntimeError(f"Invalid display mesh: {role} {len(mesh.polygons)}")
    # Keep the original embedded PBR maps in the editable Blender file.
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(blends / (role + ".blend")), compress=True)
    mesh.materials.clear()
    directory = output / role
    directory.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(directory/"Figure.fbx"), use_selection=True,
                             object_types={'MESH'}, bake_anim=False, add_leaf_bones=False,
                             bake_space_transform=True, axis_forward='-Z', axis_up='Y')
    print(f"GALLERY {role} {original} -> {len(mesh.polygons)} triangles", flush=True)
