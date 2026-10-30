"""Bake the four new planter inserts on a COPY; never save the client .blend."""
import argparse
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
out = Path(args.output).resolve()
ids = ('757827', '758007', '760649', '760736')
objects = [next(o for o in bpy.context.scene.objects if f'[{i}]' in o.name) for i in ids]
assert all(o.type == 'MESH' and len(o.data.materials) == 1 for o in objects)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 1
scene.render.bake.use_pass_direct = False
scene.render.bake.use_pass_indirect = False
scene.render.bake.use_pass_color = True
image = bpy.data.images.new('PlanterGrass_Atlas', 1024, 1024, alpha=False)
image.generated_color = (0.02, 0.04, 0.01, 1)


def pin_uv(tree, uv_name, seen):
    if tree.name in seen:
        return
    seen.add(tree.name)
    original = list(tree.nodes)
    uv = tree.nodes.new('ShaderNodeUVMap')
    uv.uv_map = uv_name
    for node in original:
        if node.type == 'GROUP':
            node.node_tree = node.node_tree.copy()
            pin_uv(node.node_tree, uv_name, seen)
        elif node.type == 'TEX_COORD':
            for link in list(node.outputs['UV'].links):
                tree.links.new(uv.outputs['UV'], link.to_socket)
        elif node.type == 'TEX_IMAGE' and not node.inputs['Vector'].is_linked:
            tree.links.new(uv.outputs['UV'], node.inputs['Vector'])


for i, obj in enumerate(objects):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.data = obj.data.copy()
    original_uv = obj.data.uv_layers.active.name
    mat = obj.data.materials[0].copy()
    obj.data.materials[0] = mat
    pin_uv(mat.node_tree, original_uv, set())
    uv = obj.data.uv_layers.new(name='BakedUV')
    obj.data.uv_layers.active = uv
    uv.active_render = True
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=0.025)
    bpy.ops.object.mode_set(mode='OBJECT')
    uv = obj.data.uv_layers['BakedUV']
    for loop in uv.data:
        loop.uv = (loop.uv.x * 0.48 + (i % 2) * 0.5 + 0.01,
                   loop.uv.y * 0.48 + (i // 2) * 0.5 + 0.01)
    tex = mat.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image = image
    mat.node_tree.nodes.active = tex
    bpy.ops.object.bake(type='DIFFUSE', margin=4, use_clear=False, uv_layer='BakedUV')
    print('BAKED', obj.name, [(min(p.uv[a] for p in uv.data), max(p.uv[a] for p in uv.data)) for a in (0, 1)], flush=True)
    obj.data.uv_layers.remove(obj.data.uv_layers.get(original_uv))
    obj.matrix_world.translation += Vector((4.4492, 0.1941, -2.0265))

(out / 'Bakes').mkdir(exist_ok=True)
image.filepath_raw = str(out / 'Bakes' / 'planter_grass.png')
image.file_format = 'PNG'
image.save()
simple = bpy.data.materials.new('PlanterGrass_Baked')
simple.use_nodes = True
tex = simple.node_tree.nodes.new('ShaderNodeTexImage')
tex.image = image
bsdf = next(n for n in simple.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
simple.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
for obj in objects:
    obj.data.materials.clear()
    obj.data.materials.append(simple)
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
bpy.ops.export_scene.fbx(filepath=str(out / 'Models' / 'ViviendaNuevo_JardinerasBaked.fbx'),
                         use_selection=True, object_types={'MESH'}, axis_forward='-Z',
                         axis_up='Y', apply_unit_scale=True, bake_anim=False, path_mode='STRIP')
(out / 'Bakes' / 'planters_manifest.json').write_text(json.dumps({
    'source_material': 'Grass 01 casa', 'objects': [o.name for o in objects],
    'albedo': 'planter_grass.png', 'size': 1024, 'triangles': 64,
    'method': 'Cycles diffuse color only; no direct/indirect illumination baked into albedo'
}, indent=2), encoding='utf-8')
print('DONE: four planter inserts, one 1K albedo atlas', flush=True)
