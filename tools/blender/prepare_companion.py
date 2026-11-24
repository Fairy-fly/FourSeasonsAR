"""Open the released spring_zhiyi.blend or autumn_manwu.blend and run with -- <Unity project parent>.

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
role = Path(bpy.data.filepath).stem
if role not in ('spring_zhiyi', 'autumn_manwu'):
    raise ValueError("Open the original spring or autumn gallery Blender file")
autumn = role == 'autumn_manwu'
body = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
bpy.context.view_layer.objects.active = body
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mesh = bmesh.new()
mesh.from_mesh(body.data)
bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=0.00005)
sole = .049 if autumn else .117
bmesh.ops.bisect_plane(mesh, geom=list(mesh.verts)+list(mesh.edges)+list(mesh.faces),
                            plane_co=(0,0,sole), plane_no=(0,0,1), clear_inner=True)
if autumn:
    # The pedestal's leaves sit beside or behind the shoes and are fused at their edges.
    bmesh.ops.delete(mesh, geom=[v for v in mesh.verts
        if v.co.z < .151 and (v.co.x < -.079 or v.co.x > .067 or v.co.y > .040)], context='VERTS')
    # The sculpt welds the inside edges of both shoes and calves together.
    # Split below the skirt, then close the narrow opening on each side.
    cuttable = [v for v in mesh.verts if v.co.z < .30]
    cut_edges = {e for v in cuttable for e in v.link_edges}
    cut_faces = {f for e in cut_edges for f in e.link_faces}
    seam = bmesh.ops.bisect_plane(mesh, geom=cuttable + list(cut_edges) + list(cut_faces),
                               plane_co=(-.006,0,0), plane_no=(1,0,0), dist=.00001)
    edges = [e for e in seam['geom_cut'] if isinstance(e, bmesh.types.BMEdge)]
    bmesh.ops.split_edges(mesh, edges=edges)
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
rim = [e for e in mesh.edges if e.is_boundary and
       (all(abs(v.co.z-sole)<.0001 for v in e.verts) or
        (autumn and all(v.co.z<.302 for v in e.verts)))]
bmesh.ops.holes_fill(mesh, edges=rim, sides=0)
mesh.to_mesh(body.data)
mesh.free()
rig_name = 'autumn_manwu_rig' if autumn else 'SpringRig'
arm = bpy.data.armatures.new(rig_name)
rig = bpy.data.objects.new(rig_name, arm)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head, b.tail = head, tail
    if parent:
        b.parent = arm.edit_bones[parent]
hips, chest, head = ((-.007,-.045,.389),(-.006,-.064,.505),(-.011,-.029,.728)) if autumn else ((0,0,.39),(0,0,.47),(0,-.02,.65))
bone('Hips',hips,chest)
bone('Chest',chest,head,'Hips')
bone('Head',head,(0,0,1.04),'Chest')
for side,sgn in [('L',1),('R',-1)]:
    def at(x,y,z):return (x*sgn,y,z)
    # The sculpt has a staggered stance. Fit each joint to its own calf and ankle.
    if side == 'L':
        hip,knee,ankle=(.038,-.060,.407),(.051,-.061,.267),(.059,.006,.15)
    else:
        hip,knee,ankle=(-.035,-.044,.407),(-.032,-.027,.267),(-.015,.031,.15)
    shoulder=at(.075,-.02,.62)
    elbow=at(.102,-.073,.55) if sgn==-1 else at(.132,-.045,.53)
    wrist=at(.056,-.138,.61) if sgn==-1 else at(.169,-.084,.464)
    curl_top,curl_end=at(.18,.008,.79),at(.215,-.004,.51)
    hem_top,hem_end=at(.095,-.005,.49),at(.151,-.022,.38)
    toe=(ankle[0],ankle[1]-.052,.124)
    if autumn:
        if side == 'L':
            hip,knee,ankle=(.026,-.050,.398),(.021,-.028,.267),(.019,-.008,.143)
            shoulder,elbow,wrist=(.047,-.043,.648),(.077,-.058,.558),(.128,-.097,.481)
            curl_top,curl_end=(.093,.097,.745),(.089,.145,.620)
        else:
            hip,knee,ankle=(-.040,-.050,.398),(-.036,-.021,.267),(-.035,-.008,.143)
            shoulder,elbow,wrist=(-.060,-.046,.648),(-.087,-.072,.549),(-.047,-.123,.627)
            curl_top,curl_end=(-.09,.037,.80),(-.087,.03,.68)
        hem_top,hem_end=at(.061,-.062,.475),at(.156,.025,.304)
        toe=(ankle[0],ankle[1]-.053,.071)
    for n,h,t,parent in [('Thigh',hip,knee,'Hips'),('Shin',knee,ankle,'Thigh'+side),
                         ('Foot',ankle,toe,'Shin'+side),
                         ('UpperArm',shoulder,elbow,'Chest'),('Forearm',elbow,wrist,'UpperArm'+side),
                         ('Palm',wrist,at(abs(wrist[0])+.012,wrist[1]-.015,wrist[2]-.018),'Forearm'+side),
                         ('Curl',curl_top,curl_end,'Head'),
                         ('Hem',hem_top,hem_end,'Hips')]:
        bone(n+side,h,t,parent)
bpy.ops.object.mode_set(mode='OBJECT')
groups = {b.name:body.vertex_groups.new(name=b.name) for b in arm.bones}
# Below the knees the shoes are two separate islands, even where a shoe
# crosses x=0. Assign each entire island to its own leg before blending joints.
lower = {v.index for v in body.data.vertices if v.co.z < .245}
neighbours = {i: [] for i in lower}
for edge in body.data.edges:
    a,b=edge.vertices
    if a in lower and b in lower:
        neighbours[a].append(b)
        neighbours[b].append(a)
leg_side = {}
while lower:
    island = [lower.pop()]
    for index in island:
        for neighbour in neighbours[index]:
            if neighbour in lower:
                lower.remove(neighbour)
                island.append(neighbour)
    center = sum(body.data.vertices[i].co.x for i in island) / len(island)
    for index in island:
        leg_side[index] = 'L' if center > (-.006 if autumn else .025) else 'R'
if set(leg_side.values()) != {'L', 'R'}:
    raise ValueError("Both shoes must be present before skinning")
for vertex in body.data.vertices:
    p=vertex.co; x,y,z=p; side=leg_side.get(vertex.index, 'L' if x>0 else 'R')
    if z < (.150 if autumn else .173):
        # A slipper and its sole keep their shape, including the toe across the midline.
        groups['Foot'+side].add([vertex.index], 1.0, 'REPLACE')
        continue
    if autumn and z < .48 and (abs(x+.008) > .065 or y > .049):
        candidates=['Hips','Hem'+side]
    elif autumn and x>.055 and y>.075 and .57<z<.84:
        candidates=['Head','CurlL']
    elif z < .39:
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
mod=body.modifiers.new('Companion skin','ARMATURE')
mod.object=rig
rig.show_in_front=True
bpy.context.preferences.filepaths.save_version=0
editable=root/'derived/blender/companions'/(role+'.blend')
editable.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(editable),compress=True)
body.data.materials.clear()
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
rig.select_set(True)
target=root/'UnityProject/Assets/Resources/Gallery'/role/'Companion.fbx'
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'ARMATURE','MESH'},
                         bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y')
print('COMPANION_SKIN',role,len(arm.bones),'bones',len(body.data.polygons),'faces')
