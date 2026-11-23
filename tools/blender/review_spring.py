"""Open the released .blend in Blender before running this inspection and studio render."""
import sys
from pathlib import Path
import bpy

root=Path(__file__).resolve().parents[2]
folder=root/'reports/evidence/blender-spring'
scene=bpy.context.scene
rig=bpy.data.objects['Spring · pose controls']
assert len(rig.data.bones)==14 and rig.animation_data.action
parts=[o for o in scene.objects if o.get('joint')]
assert len(parts)>500 and all(o.parent==rig for o in parts)
assert len({o.parent_bone for o in parts})>=13
assert bpy.data.images['Spring · painted face'].packed_file
scene.frame_set(33)
assert abs(rig.pose.bones['Head'].rotation_euler.y)>.01
scene.frame_set(1)
print('REOPENED',bpy.data.filepath,len(parts),'parts, packed face paint, editable rig and keyed pose verified',flush=True)
cycles=bpy.context.preferences.addons['cycles'].preferences
for api in ('OPTIX','CUDA','HIP','METAL'):
    try:
        cycles.compute_device_type=api
        cycles.refresh_devices()
        devices=[d for d in cycles.devices if d.type==api]
        if devices:
            for d in cycles.devices: d.use=d in devices
            scene.cycles.device='GPU'
            print('Render device',devices[0].name,flush=True)
            break
    except (TypeError,RuntimeError): pass
quick='--quick' in sys.argv
if quick:
    scene.render.resolution_percentage=48
    scene.cycles.samples=8
names=['Portrait'] if quick else ['Portrait','Front','Profile','Reverse','Face detail','Obi detail']
for name in names:
    scene.camera=bpy.data.objects[name]
    if not quick and 'detail' in name:
        scene.render.resolution_x=scene.render.resolution_y=960
    dest=(root/'UnityProject/Library/spring-blender-proof.png') if quick else (folder/(name.replace(' ','-').lower()+'.png'))
    scene.render.filepath=str(dest)
    bpy.ops.render.render(write_still=True)
    assert dest.stat().st_size>15000
