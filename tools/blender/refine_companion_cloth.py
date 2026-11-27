"""Open a rigged companion and run with -- <output directory>.

The summer sculpt joins the lantern to the skirt. Split that contact and skin
the lantern rigidly to the holding hand. Blend the outer autumn cuff into the
upper sleeve and dress along surface edges.
"""

from collections import deque
from pathlib import Path
import sys

import bmesh
import bpy


def smooth(start, end, value):
    t = max(0, min(1, (value - start) / (end - start)))
    return t*t*(3-2*t)


def lantern_vertices(points, edges):
    # The source/sink cut follows the thinnest contact near the front of the
    # dress. Use the outside petals and tassel as lantern seeds.
    size = len(points)
    source, sink = size, size + 1
    graph = [[] for _ in range(size + 2)]

    def link(a, b, capacity, other_capacity=0):
        graph[a].append([b, len(graph[b]), capacity])
        graph[b].append([a, len(graph[a])-1, other_capacity])

    candidate = [x < -.083 and y < -.070 and .235 < z < .611 for x, y, z in points]
    for a, b in edges:
        if not (candidate[a] or candidate[b]):
            continue
        x, y, z = (points[a]+points[b])*.5
        fused = x > -.166 and y > -.158 and .412 < z < .578
        cost = 8 if fused else 18
        if z > .586:
            cost = 4
        link(a, b, cost, cost)

    for i, (x, y, z) in enumerate(points):
        if not candidate[i]:
            if graph[i]:
                link(i, sink, 20000)
            continue
        outside_petals = x < -.167 and y < -.103 and .416 < z < .570
        front = x < -.142 and y < -.164
        tassel = z < .423 and x < -.156 and y < -.105
        if outside_petals or front or tassel:
            link(source, i, 20000)
            continue
        # A tilted plane is a weak prior. The edge cost moves the seam away
        # from visible petal surfaces where they curve towards the dress.
        separation = min((-.117 - .26*(y+.14) - x)/.035, (-.112-y)/.037)
        bias = int(max(-1, min(1, separation))*8)
        if bias > 0:
            link(source, i, bias)
        if bias < 0:
            link(i, sink, -bias)

    def reachable():
        distance = [-1] * len(graph)
        distance[source] = 0
        queue = deque([source])
        while queue:
            a = queue.popleft()
            for b, _, cap in graph[a]:
                if cap and distance[b] < 0:
                    distance[b] = distance[a] + 1
                    queue.append(b)
        return distance

    while True:
        distance = reachable()
        if distance[sink] < 0:
            return {i for i in range(size) if distance[i] >= 0}
        cursors = [0] * len(graph)

        def send(a, amount):
            if a == sink:
                return amount
            while cursors[a] < len(graph[a]):
                edge = graph[a][cursors[a]]
                b, reverse, cap = edge
                if cap and distance[b] == distance[a] + 1:
                    pushed = send(b, min(cap, amount))
                    if pushed:
                        edge[2] -= pushed
                        graph[b][reverse][2] += pushed
                        return pushed
                cursors[a] += 1
            return 0

        while send(source, 20000):
            pass


def save_seam_uv(new_faces, prop, uv):
    if not uv:
        return
    for face in new_faces:
        # Sample the nearest existing material on its own side of the cut.
        neighbours = {neighbour for vertex in face.verts for neighbour in vertex.link_faces
                      if neighbour not in new_faces and neighbour[prop] == face[prop]}
        refs = [(loop.vert.co, loop[uv].uv.copy()) for neighbour in neighbours for loop in neighbour.loops]
        if not refs:
            continue
        for loop in face.loops:
            loop[uv].uv = min(refs, key=lambda pair: (pair[0]-loop.vert.co).length_squared)[1]


def separate_lantern(body):
    points = [v.co.copy() for v in body.data.vertices]
    prop_ids = lantern_vertices(points, [e.vertices[:] for e in body.data.edges])
    if not 1500 < len(prop_ids) < 4500:
        raise ValueError(f'Unexpected lantern region: {len(prop_ids)} vertices')
    mesh = bmesh.new()
    mesh.from_mesh(body.data)
    prop = mesh.faces.layers.int.new('Lantern zone')
    for f in mesh.faces:
        f[prop] = int(sum(v.index in prop_ids for v in f.verts) > len(f.verts)/2)
    deform = mesh.verts.layers.deform.verify()
    uv = mesh.loops.layers.uv.active
    # The handle still meets the grip above this seam and follows the same hand.
    seam = [e for e in mesh.edges if len(e.link_faces)==2 and
            e.link_faces[0][prop]!=e.link_faces[1][prop] and
            max(v.co.z for v in e.verts)<.583]
    if not 15 < len(seam) < 310:
        raise ValueError(f'Unexpected skirt contact: {len(seam)} edges')
    split=bmesh.ops.split_edges(mesh, edges=seam)
    rims = [e for e in split['edges'] if e.is_boundary]
    for side in (0, 1):
        edges = [edge for edge in rims if edge.link_faces[0][prop] == side]
        fills = set(bmesh.ops.holes_fill(mesh,edges=edges,sides=0)['faces'])
        for face in fills:
            face[prop]=side
        save_seam_uv(fills,prop,uv)
    def index(name):
        return body.vertex_groups[name].index
    palm, hips, hem, chest = (index(n) for n in ('PalmR','Hips','HemR','Chest'))
    arm = {index(n) for n in ('UpperArmR','ForearmR','PalmR')}
    rigid = 0
    for vertex in mesh.verts:
        weights = vertex[deform]
        x,y,z = vertex.co
        memberships = {f[prop] for f in vertex.link_faces}
        if memberships == {1}:
            rigid += 1
            weights.clear()
            weights[palm] = 1
        elif x < -.075 and y < -.053 and z < .576 and sum(weights.get(g,0) for g in arm) > .04:
            # The garment beside the prop follows the torso/hem, with a fade
            # near the elbow so the wrist itself keeps its original grip.
            fade = 1-smooth(.536,.576,z)
            previous = dict(weights.items())
            support = smooth(.454,.568,z)
            target = {hem:.74*(1-support),hips:.26*(1-support),chest:support}
            assign(weights,{g:(1-fade)*previous.get(g,0)+fade*target.get(g,0)
                            for g in previous.keys() | target.keys()})
    bmesh.ops.recalc_face_normals(mesh,faces=list(mesh.faces))
    mesh.to_mesh(body.data)
    mesh.free()
    print('SUMMER_LANTERN',len(seam),'seam edges,',rigid,'rigid vertices',flush=True)


def assign(weights, values):
    strongest = sorted((w, g) for g, w in values.items() if w > .0001)[-4:]
    total = sum(w for w, _ in strongest)
    weights.clear()
    for w, g in strongest:
        weights[g] = w / total


def blend_cuff(body):
    mesh = bmesh.new()
    mesh.from_mesh(body.data)
    deform = mesh.verts.layers.deform.verify()
    arm_groups = {g.index for g in body.vertex_groups if g.name in ('UpperArmL','ForearmL','PalmL')}
    cloth = body.vertex_groups['HemL'].index
    chest = body.vertex_groups['Chest'].index
    region = {v for v in mesh.verts if .052 < v.co.x < .222 and
              -.174 < v.co.y < .052 and .433 < v.co.z < .669}
    neighbours = {v:[edge.other_vert(v) for edge in v.link_edges] for v in region}
    previous = {v:dict(v[deform].items()) for v in region.union(n for group in neighbours.values() for n in group)}
    current = previous.copy()
    for _ in range(10):
        updated = {}
        for v in region:
            adjacent = neighbours[v]
            average = {g:.52*w for g,w in current[v].items()}
            if adjacent:
                for neighbour in adjacent:
                    for g, w in current[neighbour].items():
                        average[g] = average.get(g,0) + .48*w/len(adjacent)
            updated[v] = average
        current.update(updated)
    for v in region:
        x,y,z = v.co
        a = smooth(.053,.083,x)*(1-smooth(.192,.219,x))
        a *= smooth(-.173,-.140,y)*(1-smooth(.013,.050,y))
        a *= smooth(.435,.462,z)*(1-smooth(.624,.667,z))
        values, soft = previous[v],current[v]
        weights = {g:(1-a)*values.get(g,0)+a*soft.get(g,0) for g in values.keys() | soft.keys()}
        # Loose fabric below the cuff should travel with the skirt. Above
        # the cuff, soften the sleeve without changing the hand or shoulder.
        sleeve = smooth(.467,.546,z)
        loosen = (1-sleeve)*.78*a
        relaxed = sum(weights.get(g,0) for g in arm_groups)*loosen
        for g in arm_groups:
            if g in weights:
                weights[g] *= 1-loosen
        weights[cloth] = weights.get(cloth,0)+relaxed*(1-sleeve*.60)
        weights[chest] = weights.get(chest,0)+relaxed*sleeve*.60
        assign(v[deform],weights)
    mesh.to_mesh(body.data)
    mesh.free()
    print('AUTUMN_CUFF',len(region),'vertices in transition',flush=True)


output = Path(sys.argv[sys.argv.index('--')+1]).resolve()
role = Path(bpy.data.filepath).stem
fix = {'summer_huajian': separate_lantern, 'autumn_manwu': blend_cuff}[role]
body = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
fix(body)
output.mkdir(parents=True, exist_ok=True)
blend = output / (role + '.blend')
fbx = output / (role + '.fbx')
if blend.exists() or fbx.exists():
    raise FileExistsError(f'This operation expects a new output directory: {output}')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(blend), compress=True)
body.data.materials.clear()
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},
                         bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y')
