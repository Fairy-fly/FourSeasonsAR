"""Separate fused soles and attach each shoe shell to its own leg."""

from heapq import heappop, heappush

import bmesh

# Heights use the original Blender sculpt's coordinate scale. The shoe remains rigid
# below the first cuff height; the second marks its fade into the shin.
SHOES = {
    'summer_huajian': (.320, .155, .222),
    'winter_xueling': (.318, .182, .242),
    'hidden_shijian': (.244, .106, .165),
    'hidden_xingmian': (.282, .104, .164),
}

def split_fused_pair(mesh, role):
    if role == 'winter_xueling':
        split_boots(mesh)
        return
    if role != 'hidden_shijian':
        return
    height,center,rear_limit=.112,-.009,.035
    band = [vert for vert in mesh.verts
            if vert.co.z<height and -.144<vert.co.y<rear_limit and abs(vert.co.x-center)<.060]
    edges = {edge for vert in band for edge in vert.link_edges}
    faces = {face for edge in edges for face in edge.link_faces}
    seam = bmesh.ops.bisect_plane(mesh, geom=band+list(edges)+list(faces),
                               plane_co=(center,0,0), plane_no=(1,0,0), dist=.000015)
    seam_edges = [edge for edge in seam['geom_cut'] if isinstance(edge,bmesh.types.BMEdge)
                  and all(vert.co.z<height+.010 for vert in edge.verts)]
    split = bmesh.ops.split_edges(mesh, edges=seam_edges)
    bmesh.ops.holes_fill(mesh, edges=[edge for edge in split['edges'] if edge.is_boundary],
                        sides=0)

def split_boots(mesh):
    # A narrow slit removes faces in the fused center, including the filled
    # bottom from the sculpt's stand. Seal each boot on its own side.
    center=.021
    half=.0018
    cutoff=.344
    def within(vo):
        return vo.co.z<cutoff and -.173<vo.co.y<.123
    for x in (center-half,center+half):
        band=[v for v in mesh.verts if within(v) and abs(v.co.x-center)<.075]
        edges={edge for vert in band for edge in vert.link_edges}
        faces={face for edge in edges for face in edge.link_faces}
        bmesh.ops.bisect_plane(mesh,geom=band+list(edges)+list(faces),
                              plane_co=(x,0,0),plane_no=(1,0,0),dist=.00001)
    slit=[face for face in mesh.faces if all(within(v) and abs(v.co.x-center)<half+.00002 for v in face.verts)]
    bmesh.ops.delete(mesh,geom=slit,context='FACES_ONLY')
    wires=[edge for edge in mesh.edges if edge.is_wire and
           all(within(v) and abs(v.co.x-center)<half+.00002 for v in edge.verts)]
    bmesh.ops.delete(mesh,geom=wires,context='EDGES')
    for x in (center-half,center+half):
        rim=[e for e in mesh.edges if e.is_boundary and
             all(within(v) and abs(v.co.x-x)<.00003 for v in e.verts)]
        bmesh.ops.holes_fill(mesh,edges=rim,sides=0)

def find_leg_shells(body, role, profile):
    top,_,_ = SHOES[role]
    sole=profile['sole']
    remaining={vertex.index for vertex in body.data.vertices if vertex.co.z<top}
    neighbours={index:[] for index in remaining}
    for edge in body.data.edges:
        a,b=edge.vertices
        if a in remaining and b in remaining:
            neighbours[a].append(b)
            neighbours[b].append(a)
    shells=[]
    while remaining:
        island=[remaining.pop()]
        for index in island:
            for beside in neighbours[index]:
                if beside in remaining:
                    remaining.remove(beside)
                    island.append(beside)
        on_floor=[body.data.vertices[index].co.x for index in island
                  if body.data.vertices[index].co.z<sole+.014]
        if len(on_floor)>=12:
            shells.append((len(on_floor),sum(on_floor)/len(on_floor),island))
    shells=sorted(shells,key=lambda shell:shell[0],reverse=True)[:2]
    if len(shells)!=2 or abs(shells[0][1]-shells[1][1])<.026:
        raise ValueError(f'{role} must have two separate shoes: {[(n,x) for n,x,_ in shells]}')
    shells.sort(key=lambda shell:shell[1])
    print('SHOE_SHELLS',role,[(side,count,round(x,4),len(island))
          for side,(count,x,island) in zip(('R','L'),shells)],flush=True)
    return {index:side for side,(_,_,island) in zip(('R','L'),shells) for index in island}

def smooth(low, high, value):
    t=max(0.0,min(1.0,(value-low)/(high-low)))
    return t*t*(3-2*t)

def shoe_weights(role, side, height, profile):
    _,cuff_start,cuff_end=SHOES[role]
    knee=profile[side]['leg'][1][2]
    shin=smooth(cuff_start,cuff_end,height)
    thigh=smooth(knee-.045,knee+.059,height)
    return [('Foot'+side,1-shin),('Shin'+side,shin*(1-thigh)),('Thigh'+side,shin*thigh)]

def follow_thighs(body, role, shoe_shells):
    # Continue the two connected bands past the shell boundary. A height-only
    # cutoff leaves adjacent calf triangles attached to the skirt or pelvis.
    if role.startswith('hidden_'):
        return {}
    top,_,_=SHOES[role]
    upper=.446 if role=='summer_huajian' else .449
    verts=body.data.vertices
    band={vert.index for vert in verts
          if top-.025<vert.co.z<upper and -.148<vert.co.y<.102 and abs(vert.co.x)<.133}
    neighbours={i:[] for i in band}
    for edge in body.data.edges:
        a,b=edge.vertices
        if a in band and b in band:
            length=(verts[a].co-verts[b].co).length
            neighbours[a].append((b,length))
            neighbours[b].append((a,length))
    distances={}
    for side in ('L','R'):
        dist={index:0.0 for index in band if shoe_shells.get(index)==side}
        queue=[(0.0,index) for index in dist]
        while queue:
            distance,index=heappop(queue)
            if distance>dist[index] or distance>.175:
                continue
            for beside,length in neighbours[index]:
                alternate=distance+length
                if alternate<dist.get(beside,float('inf')):
                    dist[beside]=alternate
                    heappush(queue,(alternate,beside))
        distances[side]=dist
    influence={}
    for index in band-shoe_shells.keys():
        point=verts[index].co
        left=distances['L'].get(index,.25)
        right=distances['R'].get(index,.25)
        close=min(left,right)
        if close>.17:
            continue
        # The legs merge just under the skirt. Blend their shared vertices
        # smoothly and fade the leg contribution into the hips above the thigh.
        amount=(1-smooth(.392,upper,point.z))*(1-smooth(.095,.17,close))
        split=1-smooth(-.036,.036,left-right)
        influence[index]=(amount*split,amount*(1-split))
    return influence
