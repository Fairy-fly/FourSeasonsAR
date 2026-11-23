from pathlib import Path
import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
for role in ('hidden_shijian', 'hidden_xingmian'):
    bpy.ops.wm.open_mainfile(filepath=str(root/'derived/blender/hunyuan'/(role+'.blend')))
    scene = bpy.context.scene
    model, = [o for o in scene.objects if o.type == 'MESH']
    for material in model.data.materials:
        tree = material.node_tree
        shader = next(n for n in tree.nodes if n.type == 'BSDF_PRINCIPLED')
        pigment = shader.inputs['Base Color'].links[0].from_socket
        # Mix pigment with a soft diffuse response for the same painted finish as the album viewer.
        emission = tree.nodes.new('ShaderNodeEmission')
        diffuse = tree.nodes.new('ShaderNodeBsdfDiffuse')
        mix = tree.nodes.new('ShaderNodeMixShader')
        mix.inputs[0].default_value = .32
        tree.links.new(pigment,emission.inputs['Color'])
        tree.links.new(pigment,diffuse.inputs['Color'])
        tree.links.new(emission.outputs[0],mix.inputs[1])
        tree.links.new(diffuse.outputs[0],mix.inputs[2])
        output = next(n for n in tree.nodes if n.type == 'OUTPUT_MATERIAL' and n.is_active_output)
        tree.links.new(mix.outputs[0],output.inputs['Surface'])
    corners = [model.matrix_world @ Vector(c) for c in model.bound_box]
    center = sum(corners,Vector()) / 8
    height = max(c.z for c in corners)-min(c.z for c in corners)
    camera_data = bpy.data.cameras.new('Portrait')
    camera = bpy.data.objects.new('Portrait',camera_data)
    scene.collection.objects.link(camera)
    camera.location = center + height * Vector((0,-3,.08))
    camera.rotation_euler = (center-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.camera = camera
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = height * 1.035
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    scene.eevee.taa_render_samples = 24
    scene.world = bpy.data.worlds.new('Porcelain studio')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.8,.82,.9,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = .6
    for name,offset,power,size,tint in (
        ('Window',(-1.8,-2.4,2.1),185,2.2,(1,.92,.84)),
        ('Fill',(1.4,-1.8,.65),105,2.6,(.87,.91,1)),
        ('Rim',(.4,1.1,1.55),140,1.4,(1,.92,.89))):
        data = bpy.data.lights.new(name,'AREA')
        data.energy,data.shape,data.size,data.color = power*height**2,'DISK',size*height,tint
        light = bpy.data.objects.new(name,data)
        scene.collection.objects.link(light)
        light.location = center + height*Vector(offset)
        light.rotation_euler = (center-light.location).to_track_quat('-Z','Y').to_euler()
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.exposure = -.50 if role == 'hidden_shijian' else -.23
    scene.render.film_transparent = True
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.resolution_x,scene.render.resolution_y = 850,1440
    scene.render.resolution_percentage = 100
    preview = root/'derived/blender/portrait-previews'
    preview.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(preview/(role+'.png'))
    bpy.ops.render.render(write_still=True)
