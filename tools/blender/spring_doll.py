"""Author the layered spring figurine with Blender 4.5. Run with Blender --background --python."""
import math
import sys
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Vector, Quaternion

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "derived/blender"
MOBILE = ROOT / "UnityProject/Assets/Resources/Models"
PROOF = ROOT / "reports/evidence/blender-spring"
for folder in (ART, MOBILE, PROOF):
    folder.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = .05
TAU = math.tau
sin, cos, exp, pi = math.sin, math.cos, math.exp, math.pi
parts, groups, palette = [], {}, []
bone = 'Chest'
section = 'Dress'


def group(name):
    if name not in groups:
        groups[name] = bpy.data.collections.new(name)
        scene.collection.children.link(groups[name])
    return groups[name]


def srgb(v):
    return v/12.92 if v < .04045 else ((v+.055)/1.055)**2.4


def mat(name, hexcolor, rough=.37, metal=0, pearl=0):
    color = tuple(srgb(int(hexcolor[i:i+2],16)/255) for i in (0,2,4))
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color,1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get("Principled BSDF")
    bs.inputs['Base Color'].default_value = (*color,1)
    bs.inputs['Roughness'].default_value = rough
    bs.inputs['Metallic'].default_value = metal
    bs.inputs['Coat Weight'].default_value = pearl
    bs.inputs['Coat Roughness'].default_value = .25
    m['atlas_index'] = len(palette)
    palette.append(m)
    return m


skin=mat('01 · warm porcelain','F6D9C7',.49,0,.06)
inner=mat('02 · ear blush','EBACAE',.52)
hair=mat('03 · sakura silk','EDABB4',.33,0,.24)
hair_light=mat('04 · illuminated rose','F8C8CA',.32,0,.25)
hair_shadow=mat('05 · dusty rose','CA8495',.40)
hair_mid=mat('06 · soft rose','E49EA9',.36,0,.20)
ivory=mat('07 · inner silk','FFEADD',.44,0,.12)
blush=mat('08 · outer petals','F0A2AA',.39,0,.16)
silk=mat('09 · shell pink','F9C9C6',.41,0,.14)
rose=mat('10 · garnet piping','BE717F',.43)
gold=mat('11 · brushed gold','DBB06D',.29,.58,.18)
stamen=mat('12 · pollen gold','F0C265',.35,.23)
white=mat('13 · petal tips','FFF0EA',.48,0,.1)
green=mat('14 · celadon sash','CAD1AB',.42,.07)
sole=mat('15 · rose nude sole','C98F87',.45)
stone=mat('16 · pink alabaster','E4ACAB',.33,.09,.28)
cloud=mat('17 · glazed cloud','F9D4CD',.30,.04,.30)
face=mat('18 · original painted eyes and lips','F6D9C7',.48,0,.08)
thread=mat('19 · antique gold stitch','B38C5E',.38,.34)
stage_mat=mat('20 · studio backdrop','EDE1DD',.7)
lash=mat('21 · cocoa lash','854C46',.39)
pinkgold=mat('22 · rose-gold filigree','D49C88',.30,.44)
bud=mat('23 · petal blush','EBA8AD',.43)


def link(name,data,material):
    obj=bpy.data.objects.new(name,data)
    group(section).objects.link(obj)
    data.materials.append(material)
    obj['joint']=bone
    parts.append(obj)
    return obj


def mesh(name,vertices,faces,material,orient=True):
    data=bpy.data.meshes.new(name)
    data.from_pydata(vertices,[],faces)
    data.update()
    if orient:
        bm=bmesh.new(); bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(data); bm.free()
    for f in data.polygons: f.use_smooth=True
    return link(name,data,material)


def rings(name,rows,material,caps=True):
    n=len(rows[0])
    faces=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
           for i in range(len(rows)-1) for j in range(n)]
    if caps:
        faces.extend([tuple(reversed(range(n))),tuple(range((len(rows)-1)*n,len(rows)*n))])
    return mesh(name,[p for row in rows for p in row],faces,material)


def orb(name,center,scale,material,rotation=None,res=24):
    center=Vector(center)
    rows=[]
    for i in range(1,max(7,res//2)):
        v=pi*i/max(7,res//2)
        ring=[]
        for j in range(res):
            a=TAU*j/res
            p=Vector((scale[0]*sin(v)*cos(a),scale[1]*sin(v)*sin(a),scale[2]*cos(v)))
            ring.append(center+(rotation@p if rotation else p))
        rows.append(ring)
    # True shared poles give clean smooth normals at the tip of every pearl and blossom.
    verts=[q for row in rows for q in row]
    faces=[(i*res+j,i*res+(j+1)%res,(i+1)*res+(j+1)%res,(i+1)*res+j)
           for i in range(len(rows)-1) for j in range(res)]
    for index,z in [(0,1),(len(rows)-1,-1)]:
        p=Vector((0,0,z*scale[2])); tip=len(verts)
        verts.append(center+(rotation@p if rotation else p))
        faces.extend([(tip,index*res+j,index*res+(j+1)%res) for j in range(res)])
    return mesh(name,verts,faces,material)


def spline(points,steps):
    a=np.asarray(points,float)
    result=[]
    for i in range(len(a)-1):
        p,q,r,s=a[max(0,i-1)],a[i],a[i+1],a[min(len(a)-1,i+2)]
        for j in range(steps):
            t=j/steps
            result.append(.5*(2*q+(-p+r)*t+(2*p-5*q+4*r-s)*t*t+(-p+3*q-3*r+s)*t**3))
    return np.array(result+[a[-1]])


def sweep(name,points,material,segments=14,steps=5,guide=(0,-1,0)):
    samples=spline(points,steps); rows=[]
    for i,p in enumerate(samples):
        tangent=Vector(samples[min(i+1,len(samples)-1),:3]-samples[max(0,i-1),:3]).normalized()
        width=tangent.cross(Vector(guide))
        if width.length<.001: width=tangent.cross(Vector((1,0,0)))
        width.normalize(); deep=tangent.cross(width).normalized()
        rows.append([Vector(p[:3])+width*cos(TAU*j/segments)*max(.0006,p[3])
                     +deep*sin(TAU*j/segments)*max(.0006,p[4]) for j in range(segments)])
    return rings(name,rows,material)


def cord(name,points,radius,material,cyclic=False,radii=None):
    cr=bpy.data.curves.new(name,'CURVE'); cr.dimensions='3D'
    cr.resolution_u=5; cr.bevel_depth=radius; cr.bevel_resolution=2; cr.use_fill_caps=True
    line=cr.splines.new('BEZIER'); line.bezier_points.add(len(points)-1)
    for i,(bp,p) in enumerate(zip(line.bezier_points,points)):
        bp.co=p; bp.handle_left_type=bp.handle_right_type='AUTO'
        if radii is not None: bp.radius=radii[i]
    line.use_cyclic_u=cyclic
    return link(name,cr,material)


def lathe(name,profile,material,segments=64,scallops=0):
    rows=[]
    for h,rx,ry,wave in profile:
        rows.append([(rx*(1+wave*.15*cos(10*a))*sin(a),-ry*(1+wave*.15*cos(10*a))*cos(a),
                      h+wave*scallops*cos(10*a)) for a in (TAU*j/segments for j in range(segments))])
    return rings(name,rows,material)


def flower(name,center,radius,normal,phase=0):
    origin=Vector(center); up=Vector(normal).normalized()
    axis=up.cross(Vector((0,0,1)))
    if axis.length<.05: axis=Vector((1,0,0))
    axis.normalize(); side=up.cross(axis)
    for k in range(5):
        a=TAU*k/5+phase
        radial=axis*cos(a)+side*sin(a); lateral=up.cross(radial)
        verts=[]; faces=[]
        for i in range(8):
            t=i/7
            for j in range(9):
                u=(j-4)/4
                w=(sin(pi*(t*.89+.025))**.72)*.38
                notch=.12*exp(-(u/.24)**2)*t**5
                length=(.12+t*.92-.16*u*u*t**4-notch)*radius
                lift=(.09+.20*t+.18*u*u*sin(pi*t)-.07*sin(pi*t))*radius
                verts.append(origin+radial*length+lateral*(u*w*radius)+up*lift)
        for i in range(7):
            for j in range(8):
                h=i*9+j; faces.append((h,h+9,h+10,h+1))
        petal=mesh(name+' · petal '+str(k+1),verts,faces,white if k%3 else silk,False)
        petal.data.materials.append(blush)
        for poly in petal.data.polygons:
            if poly.index//8<2: poly.material_index=1
        thick=petal.modifiers.new('Petal thickness','SOLIDIFY'); thick.thickness=radius*.035
    orb(name+' · pollen',origin+up*radius*.13,(radius*.18,)*3,stamen,res=12)
    for k in range(7):
        a=TAU*k/7+phase; tip=origin+(axis*cos(a)+side*sin(a))*.21*radius+up*.30*radius
        orb(name+' · anther',tip,(radius*.052,)*3,gold,res=10)


def build_head():
    global section,bone
    section,bone='01 · Face and ears','Head'
    rows=[]
    for i in range(1,63):
        v=pi*i/64; z=2.71+.54*cos(v)
        taper=1-.23*max(0,-cos(v))**1.2
        row=[]
        for j in range(100):
            a=TAU*j/100
            x=.53*sin(v)*sin(a)*taper
            y=.012-.453*sin(v)*cos(a)
            if cos(a)>0:
                y-=.044*exp(-(x/.067)**2-((z-2.48)/.085)**2)*cos(a)**3
                y-=.020*exp(-(x/.042)**2-((z-2.57)/.14)**2)*cos(a)**3
            row.append((x,y,z))
        rows.append(row)
    head=rings('Face sculpt · cheeks, bridge and chin',rows,skin)
    head.data.materials.append(face)
    uv=head.data.uv_layers.new(name='Paint')
    for poly in head.data.polygons:
        if poly.center.y<-.015: poly.material_index=1
        for loop in poly.loop_indices:
            v=head.data.vertices[head.data.loops[loop].vertex_index].co
            uv.data[loop].uv=((v.x+.60)/1.20,(v.z-2.12)/1.20)
    def facepoint(px,py,lift=0):
        x=(px-159)/175
        z=2.19+(211-py)/173
        cv=(z-2.71)/.54; sv=math.sqrt(max(.0001,1-cv*cv))
        taper=1-.23*max(0,-cv)**1.2
        ca=math.sqrt(max(.001,1-(x/(.53*sv*taper))**2))
        y=.012-.453*sv*ca-.044*exp(-(x/.067)**2-((z-2.48)/.085)**2)*ca**3
        y-=.020*exp(-(x/.042)**2-((z-2.57)/.14)**2)*ca**3
        return Vector((x,y-lift,z))
    # The almond shells rise from the sculpted face. Their UVs continue the original eye painting.
    for side,(cx,cy,tilt) in enumerate([(128,168,.14),(195,158,-.26)]):
        verts=[]; faces=[]
        for i in range(25):
            t=i/24
            for j in range(9):
                u=(j-4)/4; px=cx+(t-.5)*39
                py=cy+(px-cx)*tilt+u*6.6*sin(pi*t)
                verts.append(facepoint(px,py,.0015+.012*sin(pi*t)*(1-u*u)))
        for i in range(24):
            for j in range(8):
                h=i*9+j; faces.append((h,h+1,h+10,h+9))
        eye=mesh('Painted almond eye · domed',verts,faces,face,False)
        uvs=eye.data.uv_layers.new(name='Paint')
        for loop in eye.data.loops:
            v=eye.data.vertices[loop.vertex_index].co
            uvs.data[loop.index].uv=((v.x+.6)/1.2,(v.z-2.12)/1.2)
        for j in range(3):
            x=(108+2*j) if side==0 else (212-2*j)
            y=(164-.3*j) if side==0 else (148+.9*j)
            s=-1 if side==0 else 1
            cord('Outer silk eyelash',[facepoint(x,y,.003),facepoint(x+s*1.5,y-1.2,.004),
                 facepoint(x+s*(2.8+.7*j),y-(1.9+.6*j),.004)],.0018,lash,radii=[1,.66,.04])
    orb('Neck',(0,.018,2.19),(.112,.107,.18),skin)
    for s in (-1,1):
        sweep('Pointed ear',[(s*.40,-.02,2.53,.119,.08),(s*.50,-.045,2.55,.119,.067),
              (s*.61,-.052,2.61,.083,.045),(s*.756,-.048,2.687,.003,.004)],skin)
        sweep('Inner ear · recessed rose',[(s*.465,-.10,2.54,.018,.008),(s*.54,-.103,2.565,.05,.012),
              (s*.65,-.081,2.625,.016,.008),(s*.712,-.06,2.66,.001,.001)],inner)
        orb('Pierced ear pearl',(s*.50,-.094,2.47),(.023,.019,.027),gold,res=16)


def build_hair():
    global section,bone
    section,bone='02 · Hair, parting and curls','Head'
    def scalp(a,t,offset=0):
        front=(1+cos(a))/2
        v=.015+t*(2.40-1.12*front**2)
        groove=.005*cos(27*a+.7*v)*sin(v)
        return Vector(((.548+groove+offset)*sin(v)*sin(a),
            .035-(.465+groove+offset)*sin(v)*cos(a),2.76+(.55+offset)*cos(v)))
    rings('Hair cap · fluted crown',[[scalp(TAU*j/128,i/42) for j in range(128)] for i in range(43)],hair)
    for j in range(36):
        a=TAU*j/36
        pts=[scalp(a+.15*(1-t),t,.003) for t in (.16,.32,.50,.69,.85,.98)]
        cord('Combed crown strand',pts,.0036,hair_light if j%3 else hair_shadow,radii=[.1,.7,1,1,.8,.05])
    for s in (-1,1):
        for k in range(5):
            t=k/4; end=.075+.377*t
            points=[(s*(.015+.15*t),-.13+.035*t,3.27-.025*t,.024,.025),
                    (s*(.025+.25*t),-.275+.04*t,3.19,.08,.047),
                    (s*(.08+.31*t),-.372+.09*t,3.05,.098,.050),
                    (s*(.15+.315*t),-.442+.14*t,2.865-.06*t,.076,.040),
                    (s*(end+.022*sin(t*pi)),-.458+.16*t,2.765-.178*t,.004,.003)]
            sweep('Parted fringe · '+str(k+1),points,hair_light if k%3==0 else hair,18,6)
            for shift in (-.018,.018):
                strand=[(x+shift,y-d*.92,z) for x,y,z,w,d in points]
                cord('Fringe silk filament',strand,.0028,hair_light,radii=[.05,.85,1,.85,.03])
            cord('Fringe etched part',[(x+s*.043*(w/.10),y-d*.96-.004,z) for x,y,z,w,d in points],
                 .0031,hair_mid,radii=[.01,.8,1,.75,.01])
        temple=[(s*.365,-.206,3.11,.057,.049),(s*.437,-.30,2.93,.073,.054),
                (s*.448,-.296,2.73,.059,.042),(s*.405,-.326,2.57,.044,.031),
                (s*.361,-.33,2.53,.024,.021),(s*.365,-.35,2.585,.002,.002)]
        sweep('Cheek-framing curled lock',temple,hair,18)
        orb('Coiled rose chignon',(s*.493,.115,2.95),(.224,.217,.237),hair_mid,res=38)
        for j in range(8):
            pts=[]
            for n in range(13):
                t=n/12; a=TAU*j/8+1.3*t
                v=.14+(pi-.28)*t
                pts.append((s*(.493+.225*sin(v)*cos(a)),.115+.221*sin(v)*sin(a),2.95+.240*cos(v)))
            cord('Chignon coil',pts,.013,hair if j%3 else hair_light,radii=[.18]+[1]*11+[.1])
        bone='Curl.L' if s>0 else 'Curl.R'
        curl=[(s*.487,.145,2.89,.10,.103),(s*.515,.19,2.67,.135,.116),
              (s*.604,.17,2.45,.132,.11),(s*.714,.11,2.28,.113,.09),
              (s*.681,.074,2.17,.103,.082),(s*.515,.05,2.045,.08,.060),
              (s*.532,.079,1.945,.075,.055),(s*.658,.085,1.85,.06,.047),
              (s*.658,.061,1.76,.038,.035),(s*.557,.053,1.655,.021,.018),
              (s*.598,.049,1.616,.002,.002)]
        sweep('Hanging S curl · main lock',curl,hair,22,6)
        for k in range(4):
            shift=(k-1.5)*.027
            cord('Hanging lock · silk ridge',[(x+shift*w/.13,y-d*(.83+.09*(k%2)),z) for x,y,z,w,d in curl],
                 .005,hair_light if k!=1 else hair_mid,radii=[.1,.85,1,1,1,1,1,1,.75,.3,.02])
        sweep('Hanging curl · companion',[(x+s*.045,y+.073,z+.031,w*.57,d*.58) for x,y,z,w,d in curl],hair_mid,14,5)
        outer=[(x+s*(.067+.037*sin(i*.95)),y+.020+.043*cos(i*.76),z-.035*sin(i*.79),
                w*.45,d*.43) for i,(x,y,z,w,d) in enumerate(curl)]
        sweep('Separate floating curl',outer,hair_light,14,6)
        for k in range(3):
            pts=[(x+s*(.068+.014*k+.021*sin(i*.87)),y-.044+.028*cos(i*.63+k),z-.032+.022*k)
                 for i,(x,y,z,w,d) in enumerate(curl)]
            cord('Loose fine hair',pts,.0033,hair_mid if k%2 else hair_light,radii=[.02,.6,.8,1,1,1,.85,.75,.64,.32,.02])
        bone='Head'
        cord('Chignon gold chain',[(s*.40,-.151,3.15),(s*.49,-.114,3.04),(s*.555,-.080,2.90),
             (s*.58,-.105,2.77)],.009,gold)
        for k in range(3):
            flower('Hairpin blossom',(s*(.413+.043*k),-.16+.024*k,3.11-.075*k),.045-.006*k,(0,-1,.20),k*.65)
        a=(s*.559,-.082,2.77)
        cord('Ear pendant · silk cord',[a,(s*.55,-.082,2.59),(s*.558,-.072,2.38)],.006,gold)
        orb('Ear pendant · bud',(s*.558,-.072,2.36),(.022,.020,.035),gold,res=16)
        for k in range(6):
            off=.014*cos(TAU*k/6)
            cord('Ear pendant · tassel',[(s*.558+off*.3,-.07,2.35),(s*.56+off,-.07+.010*sin(k),2.28),
                 (s*.565+off*1.2,-.075+.012*sin(k),2.20+.012*cos(k))],.0038,stamen)
    flower('Crown · open sakura',(.145,-.29,3.30),.192,(.13,-.92,.42),.24)
    flower('Crown · small companion',(.306,-.238,3.233),.079,(.28,-.83,.40),1.2)
    for s in (-1,1):
        for j in range(3):
            p=(s*(.420+.039*j),-.193+.019*j,3.09-.053*j)
            orb('Chignon pin · pearl',p,(.018,.016,.019),ivory,res=14)


def build_costume():
    global bone,section
    section,bone='03 · Petal dress and embroidery','Chest'
    lathe('Crossed satin bodice',[(1.68,.23,.173,0),(1.77,.237,.172,0),(1.88,.246,.177,0),
        (1.98,.252,.178,0),(2.06,.207,.14,0),(2.11,.11,.098,0)],silk)
    for s in (-1,1):
        panel=[(s*.12,-.071,2.12,.044,.014),(s*.103,-.144,2.05,.051,.017),
               (s*.03,-.182,1.98,.050,.014),(-s*.12,-.146,1.86,.045,.012)]
        sweep('Cross-over ivory collar',panel,ivory,12)
        cord('Collar gold whipstitch',[(x+s*w*.64,y-d,z) for x,y,z,w,d in panel],.005,gold)
    bone='Pelvis'
    lathe('Underskirt · scalloped lining',[(1.75,.218,.165,0),(1.64,.28,.215,0),
        (1.48,.382,.292,.07),(1.33,.454,.354,.14),(1.19,.489,.398,.24),
        (1.16,.469,.379,.24),(1.19,.421,.336,.22)],ivory,100,.065)
    def petalpoint(a,t,u,tier):
        # Petals cup outwards and overlap at the waist; a shallow cleft shapes each hem.
        lower=tier==0
        spread=(.12+.245*sin(pi*t*.83))*(1 if lower else 1.06)
        angle=a+u*spread+.045*sin(pi*t)
        end=(1.15 if lower else 1.27) + .025*cos(3*a)
        z=1.78+(end-1.78)*t+.125*u*u*t**3+.022*exp(-(u/.18)**2)*t**5
        r=.207+.32*t**.82 +(.030 if not lower else 0)*sin(pi*t*.8)
        r+=.024*(1-u*u)*sin(pi*t)+.015*cos(3*a)*t
        return Vector((r*sin(angle),-.78*r*cos(angle),z))
    for tier in range(2):
        for k in range(10):
            a=TAU*(k+.5*tier)/10
            verts=[petalpoint(a,i/13,(j-5)/5,tier) for i in range(14) for j in range(11)]
            faces=[(i*11+j,(i+1)*11+j,(i+1)*11+j+1,i*11+j+1) for i in range(13) for j in range(10)]
            m=mesh('Silk skirt · '+('under' if not tier else 'outer')+' petal '+str(k+1),
                verts,faces,(white if k%2 else ivory) if not tier else (blush if k%3==0 else silk),False)
            shell=m.modifiers.new('Folded petal edge','SOLIDIFY'); shell.thickness=.009
            if tier:
                path=[petalpoint(a,t,.03,1) for t in (.20,.34,.49,.64,.78,.9)]
                path=[v+Vector((.005*sin(a),-.005*cos(a),0)) for v in path]
                cord('Raised petal midrib',path,.0032,ivory,radii=[.1,.8,1,1,.65,.05])
                if k%2==0:
                    loc=petalpoint(a,.68,.29,1)
                    flower('Skirt embroidered sakura',loc,.027,(sin(a),-cos(a),.45),k)
                if k%2==0 or k in (1,9):
                    radial=Vector((sin(a),-cos(a),0))
                    vine=[petalpoint(a,t,.22*sin(9*t+k),1)+radial*.006 for t in (.43,.52,.61,.70,.79,.86)]
                    cord('Skirt · embroidered vine',vine,.0031,pinkgold,radii=[.14,.8,1,1,.68,.04])
                    for h in range(3):
                        t=.53+h*.105; u=.22*sin(9*t+k); direction=(-1)**h
                        tips=[petalpoint(a,t,u,1),petalpoint(a,t+.023,u+.12*direction,1),
                              petalpoint(a,t+.04,u+.19*direction,1)]
                        sweep('Skirt · brocade leaf',[(*(v+radial*.008),w,w*.44) for v,w in zip(tips,(.002,.011,.001))],
                              gold,8,3)
    bone='Chest'
    lathe('Celadon obi',[(1.713,.244,.178,0),(1.729,.251,.187,0),(1.79,.246,.183,0),(1.812,.234,.177,0)],green)
    for z in (1.729,1.798):
        cord('Obi · gold cord',[(.249*sin(a),-.184*cos(a),z) for a in np.linspace(0,TAU,20,endpoint=False)],.007,gold,True)
    flower('Obi front clasp',(0,-.188,1.77),.075,(0,-1,.15))
    for s in (-1,1):
        sweep('Front ribbon',[(s*.02,-.19,1.75,.03,.012),(s*.059,-.237,1.65,.033,.012),
              (s*.113,-.282,1.55,.027,.010),(s*.16,-.305,1.47,.021,.008)],gold)
        loop=[(s*.016,.203,1.766,.021,.008),(s*.100,.242,1.831,.039,.010),
              (s*.192,.262,1.848,.047,.012),(s*.210,.236,1.794,.043,.010),
              (s*.124,.235,1.755,.035,.009),(s*.018,.214,1.761,.016,.008)]
        sweep('Back obi · open folded bow loop',loop,green,12,6)
        cord('Back obi · gilt selvedge',[(x,y+d,z+w*.77) for x,y,z,w,d in loop],.0047,gold)
        tail=[(s*.03,.21,1.755,.046,.018),(s*.08,.234,1.657,.048,.021),
              (s*.165,.290,1.52,.044,.018),(s*.208,.33,1.41,.031,.013)]
        sweep('Back obi · brocade tail',tail,gold)
    flower('Back obi · blossom clasp',(0,.237,1.773),.070,(0,1,.19),.3)


def build_limbs():
    global section,bone
    section='04 · Arms, hands and slippers'
    for s in (-1,1):
        suffix='.L' if s>0 else '.R'
        high=s<0
        shoulder=(s*.201,-.005,2.022)
        elbow=(s*(.315 if high else .327),-.162 if high else -.023,1.79 if high else 1.719)
        wrist=(s*(.159 if high else .476),-.309 if high else -.099,2.086 if high else 1.547)
        palm=(s*(.146 if high else .49),-.336 if high else -.108,2.127 if high else 1.522)
        bone='UpperArm'+suffix
        orb('Bare porcelain shoulder',shoulder,(.100,.091,.103),skin,res=24)
        sleeve_top=Vector(shoulder).lerp(Vector(elbow),.20)
        sweep('Silk upper sleeve',[(*sleeve_top,.096,.091),(*((Vector(shoulder)*.48+Vector(elbow)*.52)),.088,.080),
             (*elbow,.071,.067)],ivory,18,6)
        flower('Sleeve shoulder blossom',Vector(shoulder)+Vector((s*.065,-.057,-.017)),.052,(s*.45,-.84,.25))
        bone='Forearm'+suffix
        mid=Vector(elbow).lerp(Vector(wrist),.69)
        sweep('Forearm and wrist',[(*elbow,.069,.063),(*mid,.051,.048),(*wrist,.038,.034)],skin,18)
        cuff=Vector(elbow).lerp(Vector(wrist),.78)
        sleeve=[(*elbow,.075,.07),(*Vector(elbow).lerp(cuff,.58),.085,.081),(*cuff,.096,.086)]
        sweep('Translucent-look bell cuff',sleeve,silk,22)
        v=(Vector(wrist)-Vector(elbow)).normalized()
        side=v.cross(Vector((0,-1,0))).normalized(); up=v.cross(side).normalized()
        cord('Cuff embroidered rim',[cuff+side*.096*cos(a)+up*.086*sin(a) for a in np.linspace(0,TAU,20,endpoint=False)],
             .005,ivory,True)
        bone='Hand'+suffix
        orb('Palm',palm,(.051,.032,.060),skin,res=20)
        for k in range(4):
            offset=(k-1.5)*.018
            if high:
                base=(palm[0]+offset,-.341,2.145)
                middle=(base[0]+.019,-.347,2.18+.012*(2-abs(k-1)))
                tip=(base[0]+.034,-.33,2.19+.012*(2-abs(k-1)))
            else:
                base=(palm[0]+offset,-.119,1.506)
                middle=(base[0]+.024+offset*.18,-.135,1.478)
                tip=(middle[0]+.016,-.130,1.461+.008*abs(k-1))
            sweep('Finger '+str(k+1),[(*base,.014,.013),(*middle,.012,.011),(*tip,.006,.007)],skin,10,3)
        sweep('Thumb',[(palm[0]-.029,-.13 if not high else -.36,palm[2],.020,.018),
              (palm[0]-.053,-.15 if not high else -.37,palm[2]+.005,.014,.013),
              (palm[0]-.06,-.142 if not high else -.36,palm[2]+.013,.008,.007)],skin,10)
        bone='Leg'+suffix
        hip=s*.132; knee=s*.111; ankle=s*.119
        yoff=.064 if s>0 else -.025
        sweep('Porcelain leg',[(hip,.012,1.27,.103,.097),(s*.132,.007,1.084,.106,.097),
              (knee,-.016+yoff,.86,.079,.078),(s*.105,.026+yoff,.66,.087,.081),
              (ankle,.052+yoff,.405,.053,.055),(ankle,.025+yoff,.355,.058,.060)],skin,22,6)
        foot=(ankle,-.045+yoff,.327)
        orb('Slipper · blush sole',(ankle,-.070+yoff,.273),(.094,.166,.032),sole,res=28)
        orb('Slipper · satin upper',foot,(.093,.153,.062),ivory,res=28)
        orb('Slipper · instep',(ankle,-.02+yoff,.355),(.071,.097,.052),skin,res=22)
        for z in (.377,.441):
            cord('Anklet chain',[(ankle+.062*cos(a),.04+yoff+.061*sin(a),z+.013*sin(a+.3))
                  for a in np.linspace(0,TAU,16,endpoint=False)],.007,gold,True)
        for k in range(3):
            flower('Anklet blossom',(ankle+s*.054,.02+yoff+.030*k,.405+.026*k),.033,
                   (s*.77,-.46,.33),k*.7)
        cord('Slipper band',[(ankle-.077,-.100+yoff,.339),(ankle-.034,-.080+yoff,.387),
              (ankle+.034,-.071+yoff,.39),(ankle+.080,-.08+yoff,.337)],.009,gold)
        flower('Toe rosette',(ankle,-.136+yoff,.370),.029,(0,-.6,.8),.5)


def build_plinth():
    global section,bone
    section,bone='05 · Cloud plinth and fallen flowers','Root'
    lathe('Rose quartz foot',[(.022,.545,.397,0),(.034,.592,.433,0),(.060,.604,.442,0),
          (.094,.613,.448,0),(.14,.598,.435,0),(.171,.565,.402,0),(.187,.51,.37,0)],stone,96)
    for h,rx,ry in [(.054,.603,.442),(.072,.611,.447)]:
        cord('Fine gilt foot ring',[(rx*sin(a),-ry*cos(a),h) for a in np.linspace(0,TAU,36,endpoint=False)],.005,gold,True)
    orb('Cloud cushion',(0,.01,.15),(.565,.402,.103),cloud,res=42)
    for k in range(10):
        a=TAU*k/10
        center=Vector((.466*sin(a),-.318*cos(a),.162+.016*sin(a*3)))
        orb('Carved cloud lobe',center,(.124,.116,.076),cloud if k%2 else silk,res=20)
        tangent=Vector((cos(a),sin(a),0))
        normal=Vector((sin(a),-cos(a),0))
        pts=[]
        for j in range(15):
            t=j/14; theta=t*1.9*pi
            pts.append(center+normal*.091+tangent*(.065*(1-.83*t)*cos(theta))+Vector((0,0,.04*(1-.75*t)*sin(theta))))
        cord('Incised cloud spiral',pts,.007,ivory,radii=[.2]+[1]*12+[.55,.1])
    for i,(x,y,z,r) in enumerate([(-.34,-.13,.23,.133),(.37,-.05,.24,.147),(-.22,.235,.22,.107),
            (.253,.24,.224,.093),(.075,-.306,.217,.054),(-.479,.053,.196,.062)]):
        flower('Fallen sakura '+str(i+1),(x,y,z),r,(x*.7,-.44,.9),.4*i)


def paint_face():
    # Sample only the facial features of the supplied portrait; the head, ears and
    # nose retain their own modeled depth. The atlas also serves the mobile mesh.
    original=bpy.data.images.load(str(ROOT/'UnityProject/Assets/Art/spring_front_hd.png'))
    ow,oh=original.size
    pix=np.empty(ow*oh*4,np.float32); original.pixels.foreach_get(pix)
    pix=pix.reshape(oh,ow,4)
    n=1024
    yy,xx=np.mgrid[:n,:n]
    xx=(xx+.5)/n*1.2-.60; height=(yy+.5)/n*1.2+2.12
    px=159+xx*175
    py=211-(height-2.19)*173
    ox=np.clip(px/334*ow-.5,0,ow-1.001); oy=np.clip((1-py/670)*oh-.5,0,oh-1.001)
    xi,yi=ox.astype(int),oy.astype(int)
    dx,dy=(ox-xi)[:,:,None],(oy-yi)[:,:,None]
    rgb=(pix[yi,xi,:3]*(1-dx)+pix[yi,xi+1,:3]*dx)*(1-dy)+(pix[yi+1,xi,:3]*(1-dx)+pix[yi+1,xi+1,:3]*dx)*dy
    def smooth(value):
        t=np.clip(value,0,1)
        return t*t*(3-2*t)
    # Feather the edge on clean skin, away from lashes and the forehead blossom.
    edge=((px-160)/66)**2+((py-161)/52)**2
    blend=smooth((1.09-edge)/.36)*smooth((py-(120-.125*(px-158)))/10)
    def encoded(color):
        c=np.array(color[:3])
        return np.where(c<=.0031308,12.92*c,1.055*c**(1/2.4)-.055)
    base=encoded(skin.diffuse_color)
    # Byte images expose sRGB channel values in bpy; keep neutral skin continuous
    # across the image-backed front and the unpainted ears and rear of the head.
    eyes=np.maximum(np.exp(-(((px-127)/26)**4+((py-165)/18)**4)),
                    np.exp(-(((px-196)/26)**4+((py-157)/17)**4)))
    brow=np.exp(-(((px-160)/64)**4+((py-140+.13*(px-160))/13)**4))
    lips=np.exp(-(((px-166)/23)**4+((py-192)/12)**4))
    mark=np.exp(-(((px-153)/12)**4+((py-130)/12)**4))
    blend*=np.maximum.reduce([eyes,brow,lips,mark,np.full_like(eyes,.13)])
    # Retain the delicate iris/line contrast under soft studio lighting.
    tones=np.clip((.81-rgb.max(axis=2))/.42,0,1)*eyes
    rgb*=1-.15*tones[:,:,None]
    rgba=np.ones((n,n,4),np.float32)
    rgba[:,:,:3]=base+(rgb-base)*blend[:,:,None]
    image=bpy.data.images.new('Spring · painted face',width=n,height=n,alpha=False)
    image.pixels.foreach_set(rgba.ravel()); image.update(); image.pack()
    bs=face.node_tree.nodes.get('Principled BSDF')
    tex=face.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=image
    face.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    # A single square sheet holds the face and material colors for the Android shader.
    sheet=rgba.copy(); face_rows=832
    sample=np.minimum(n-1,(np.arange(face_rows)+.5)*n/face_rows).astype(int)
    sheet[:face_rows]=rgba[sample]
    sheet[face_rows:,:,:3]=base
    for i,m in enumerate(palette):
        y=896+(i//16)*64; x=(i%16)*64
        sheet[y:y+64,x:x+64,:3]=encoded(m.diffuse_color)
    atlas=bpy.data.images.new('Mobile · porcelain palette',width=n,height=n,alpha=False)
    atlas.pixels.foreach_set(sheet.ravel()); atlas.update()
    atlas.filepath_raw=str(MOBILE/'SpringBlenderPalette.png'); atlas.file_format='PNG'; atlas.save()
    bpy.data.images.remove(atlas)
    bpy.data.images.remove(original)


def make_rig():
    arm=bpy.data.armatures.new('Spring · pose skeleton')
    rig=bpy.data.objects.new('Spring · pose controls',arm)
    group('06 · Pose rig').objects.link(rig)
    bpy.context.view_layer.objects.active=rig
    rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
    def joint(name,head,tail,parent=None):
        b=arm.edit_bones.new(name); b.head=head; b.tail=tail
        if parent: b.parent=arm.edit_bones[parent]
    joint('Root',(0,0,.04),(0,0,.32))
    joint('Pelvis',(0,0,1.29),(0,0,1.73),'Root')
    joint('Chest',(0,0,1.73),(0,0,2.15),'Pelvis')
    joint('Head',(0,0,2.15),(0,0,3.10),'Chest')
    for s in (-1,1):
        suffix='.L' if s>0 else '.R'; raised=s<0
        shoulder=(s*.201,-.005,2.022)
        elbow=(s*(.315 if raised else .327),-.162 if raised else -.023,1.79 if raised else 1.719)
        wrist=(s*(.159 if raised else .476),-.309 if raised else -.099,2.086 if raised else 1.547)
        joint('UpperArm'+suffix,shoulder,elbow,'Chest')
        joint('Forearm'+suffix,elbow,wrist,'UpperArm'+suffix)
        joint('Hand'+suffix,wrist,(wrist[0]+.035,wrist[1]-.021,wrist[2]+(.077 if raised else -.077)),'Forearm'+suffix)
        joint('Leg'+suffix,(s*.132,.012,1.27),(s*.119,.04,.37),'Pelvis')
        joint('Curl'+suffix,(s*.51,.14,2.89),(s*.59,.055,1.65),'Head')
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.update()
    for obj in parts:
        # Geometry is authored in common world coordinates. Bone parenting keeps
        # every petal and editable Bezier filament intact in the authoring file.
        transform=obj.matrix_world.copy()
        obj.parent=rig; obj.parent_type='BONE'; obj.parent_bone=obj['joint']
        b=arm.bones[obj.parent_bone]
        rest=b.matrix_local.copy()
        rest.translation=b.tail_local
        obj.matrix_parent_inverse=(rig.matrix_world@rest).inverted()
        obj.matrix_basis=transform
    for frame,factor in [(1,0),(33,1),(65,0),(97,-.7),(129,0)]:
        for name,rotation in [('Chest',(.7,0,.4)),('Head',(-1.1,1.5,.8)),
                ('Curl.L',(1.1,-2.0,.7)),('Curl.R',(-.7,1.6,-1.0))]:
            b=rig.pose.bones[name]; b.rotation_mode='XYZ'
            b.rotation_euler=[math.radians(a*factor) for a in rotation]
            b.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
    rig.animation_data.action.name='Spring breeze · breathing and hair sway'
    scene.frame_start=1; scene.frame_end=128; scene.render.fps=24; scene.frame_set(1)
    arm.display_type='STICK'; rig.show_in_front=False
    rig.hide_set(True)
    return rig


def export_mobile():
    """Evaluate the same Blender objects in their rest pose into a one-material FBX."""
    deps=bpy.context.evaluated_depsgraph_get()
    verts=[]; faces=[]; uv=[]; normals=[]
    for obj in parts:
        evaluated=obj.evaluated_get(deps)
        geo=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=deps)
        geo.calc_loop_triangles()
        start=len(verts)
        mx=evaluated.matrix_world; ns=mx.to_3x3().inverted().transposed()
        verts.extend(tuple(mx@v.co) for v in geo.vertices)
        uv_layer=geo.uv_layers.active
        for tri in geo.loop_triangles:
            m=geo.materials[tri.material_index]; idx=m['atlas_index']
            faces.append(tuple(start+v for v in tri.vertices))
            for li in tri.loops:
                if m==face and uv_layer:
                    u,v=uv_layer.data[li].uv
                    uv.append((u,max(0,min(.811,v*832/1024))))
                else:
                    uv.append((((idx%16)*64+32)/1024,(896+(idx//16)*64+32)/1024))
                normals.append(tuple((ns@geo.corner_normals[li].vector).normalized()))
        evaluated.to_mesh_clear()
    data=bpy.data.meshes.new('SpringZhiyi · gallery mesh')
    data.from_pydata(verts,[],faces); data.update()
    for poly in data.polygons: poly.use_smooth=True
    layer=data.uv_layers.new(name='Palette')
    layer.data.foreach_set('uv',np.asarray(uv,np.float32).ravel())
    data.normals_split_custom_set(normals)
    obj=bpy.data.objects.new('SpringZhiyi',data)
    scene.collection.objects.link(obj)
    empty=bpy.data.materials.new('Gallery palette'); data.materials.append(empty)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active=obj
    bpy.ops.export_scene.fbx(filepath=str(MOBILE/'SpringZhiyi.fbx'),use_selection=True,
        object_types={'MESH'},apply_unit_scale=False,apply_scale_options='FBX_SCALE_NONE',
        axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False,
        use_mesh_modifiers=False,mesh_smooth_type='OFF',add_leaf_bones=False)
    print('BLENDER_MOBILE',len(verts),'vertices',len(faces),'triangles',flush=True)
    bpy.data.objects.remove(obj,do_unlink=True); bpy.data.meshes.remove(data)
    bpy.data.materials.remove(empty)


def studio():
    coll=group('07 · Portrait studio')
    floor_data=bpy.data.meshes.new('Cyclorama')
    floor_data.from_pydata([(-100,-100,0),(100,-100,0),(100,100,0),(-100,100,0)],[],[(0,1,2,3)])
    floor=bpy.data.objects.new('Warm seamless floor',floor_data); coll.objects.link(floor)
    floor.data.materials.append(stage_mat)
    def aim(obj,target):
        obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    for name,loc,power,size,color in [
            ('Softbox · silk',(-3.6,-4.6,5.7),375,4.2,(1,.90,.84)),
            ('Fill · pearl',(3.2,-2.4,3.7),185,3.5,(.86,.91,1)),
            ('Rim · sakura',(1.6,2.8,5.1),410,3.2,(1,.77,.69)),
            ('Eye catchlight',(-.3,-3.6,3.6),35,1.8,(1,.95,.91))]:
        light=bpy.data.lights.new(name,'AREA'); light.energy=power; light.shape='DISK'; light.size=size; light.color=color
        ob=bpy.data.objects.new(name,light); coll.objects.link(ob); ob.location=loc; aim(ob,(0,0,1.7))
    for name,angle,size,targetz,elev in [('Portrait',12,3.82,1.69,.12),('Front',0,3.78,1.69,.07),
            ('Profile',90,3.78,1.69,.09),('Reverse',174,3.78,1.69,.11),('Face detail',-10,1.82,2.71,.07),
            ('Obi detail',157,1.57,1.76,.10)]:
        a=math.radians(angle)
        cam=bpy.data.cameras.new(name); cam.type='ORTHO'; cam.ortho_scale=size
        ob=bpy.data.objects.new(name,cam); coll.objects.link(ob)
        ob.location=(5*sin(a),-5*cos(a),targetz+5*elev); aim(ob,(0,0,targetz))
    scene.camera=bpy.data.objects['Portrait']
    world=bpy.data.worlds.new('Soft linen daylight'); world.use_nodes=True
    world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.57,.54,1)
    world.node_tree.nodes['Background'].inputs[1].default_value=.48; scene.world=world
    scene.render.engine='CYCLES'; scene.cycles.samples=28; scene.cycles.use_denoising=True
    scene.render.resolution_x=840; scene.render.resolution_y=1024; scene.render.resolution_percentage=100
    scene.view_settings.view_transform='AgX'
    scene.render.image_settings.file_format='PNG'
    # Frame the model on opening the editable file, independent of the default UI zoom.
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                space=area.spaces.active
                space.region_3d.view_distance=4.8
                space.region_3d.view_location=(0,0,1.65)
                space.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
                space.region_3d.view_perspective='ORTHO'
                space.overlay.show_overlays=False
                space.shading.color_type='MATERIAL'


build_head()
build_hair()
build_costume()
build_limbs()
build_plinth()
paint_face()
rig=make_rig()
studio()
export_mobile()
scene['Character']='春知意 · 四时花旅'
scene['Design']='Sakura twin buns, split petal skirt, celadon obi and pink cloud plinth.'
scene['Controls']='Unhide Spring · pose controls to edit the 14-bone rigid ornament rig. Play frames 1–128.'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'春知意.blend'),compress=True)
print('BLENDER_SOURCE',len(parts),'editable parts',len(rig.data.bones),'bones',flush=True)
