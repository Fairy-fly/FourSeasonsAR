"""Open the released spring_zhiyi.blend and run with -- <Unity project parent>.

The mobile figurine and plinth are a single fused surface. Slice below both ankles,
keep the connected character and close its two soles before skinning the mobile copy.
"""
from pathlib import Path
import math
import sys

import bmesh
import bpy
from mathutils import Vector

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
body = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
bpy.context.view_layer.objects.active = body
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mesh = bmesh.new()
mesh.from_mesh(body.data)
bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=0.00005)
sole = .117
cut = bmesh.ops.bisect_plane(mesh, geom=list(mesh.verts)+list(mesh.edges)+list(mesh.faces),
                            plane_co=(0,0,sole), plane_no=(0,0,1), clear_inner=True)
# Exclude disconnected petals of the old pedestal below the hem.
seed = max(mesh.verts, key=lambda v:v.co.z)
connected = {seed}
stack = [seed]
while stack:
    for edge in stack.pop().link_edges:
        for vertex in edge.verts:
            if vertex not in connected:
                connected.add(vertex)
                stack.append(vertex)
bmesh.ops.delete(mesh, geom=[v for v in mesh.verts if v not in connected], context='VERTS')
rim = [e for e in mesh.edges if e.is_boundary and all(abs(v.co.z-sole)<.0001 for v in e.verts)]
bmesh.ops.holes_fill(mesh, edges=rim, sides=0)
mesh.to_mesh(body.data)
mesh.free()
arm = bpy.data.armatures.new('SpringRig')
rig = bpy.data.objects.new('SpringRig', arm)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head, b.tail = head, tail
    if parent:
        b.parent = arm.edit_bones[parent]
bone('Hips',(0,0,.39),(0,0,.47))
bone('Chest',(0,0,.47),(0,-.02,.65),'Hips')
bone('Head',(0,-.02,.65),(0,0,1.04),'Chest')
for side,sgn in [('L',1),('R',-1)]:
    def at(x,y,z):return (x*sgn,y,z)
    hip,knee,ankle=at(.037,-.03,.40),at(.043,-.05,.27),at(.038,0,.15)
    shoulder=at(.075,-.02,.62)
    elbow=at(.102,-.073,.55) if sgn==-1 else at(.132,-.045,.53)
    wrist=at(.056,-.138,.61) if sgn==-1 else at(.169,-.084,.464)
    for n,h,t,parent in [('Thigh',hip,knee,'Hips'),('Shin',knee,ankle,'Thigh'+side),
                         ('Foot',ankle,at(.04,-.041,.124),'Shin'+side),
                         ('UpperArm',shoulder,elbow,'Chest'),('Forearm',elbow,wrist,'UpperArm'+side),
                         ('Palm',wrist,at(abs(wrist[0])+.012,wrist[1]-.015,wrist[2]-.018),'Forearm'+side),
                         ('Curl',at(.18,.008,.79),at(.215,-.004,.51),'Head'),
                         ('Hem',at(.095,-.005,.49),at(.151,-.022,.38),'Hips')]:
        bone(n+side,h,t,parent)
bpy.ops.object.mode_set(mode='OBJECT')
groups = {b.name:body.vertex_groups.new(name=b.name) for b in arm.bones}
for vertex in body.data.vertices:
    p=vertex.co; x,y,z=p; side='L' if x>0 else 'R'
    if z < .39:
        candidates=['Thigh'+side,'Shin'+side,'Foot'+side] if z<.345 or abs(x)<.068 else ['Hips','Hem'+side]
    elif z>.80:
        candidates=['Head']
    elif abs(x)>.139 and z>.49 and (z>.61 or y>-.045):
        candidates=['Head','Curl'+side]
    else:
        candidates=['Head','Chest','Hips','Hem'+side,'UpperArm'+side,'Forearm'+side,'Palm'+side]
    weights=[]
    for name in candidates:
        b=arm.bones[name]; d=b.tail_local-b.head_local
        t=max(0,min(1,(p-b.head_local).dot(d)/d.length_squared))
        dist=(p-b.head_local-t*d).length
        weights.append((1/(.012+dist)**4,name))
    weights=sorted(weights,reverse=True)[:4]
    total=sum(w for w,n in weights)
    for w,n in weights:groups[n].add([vertex.index],w/total,'REPLACE')
body.parent=rig
mod=body.modifiers.new('Spring skin','ARMATURE')
mod.object=rig
rig.show_in_front=True
bpy.context.preferences.filepaths.save_version=0
editable=root/'derived/blender/companions/spring_zhiyi.blend'
editable.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(editable),compress=True)
body.data.materials.clear()
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
rig.select_set(True)
target=root/'UnityProject/Assets/Resources/Gallery/spring_zhiyi/Companion.fbx'
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'ARMATURE','MESH'},
                         bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y')
print('SPRING_SKIN',len(arm.bones),'bones',len(body.data.polygons),'faces')
