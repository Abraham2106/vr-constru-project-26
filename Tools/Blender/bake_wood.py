# Run in Blender background on a COPY of the client source. Never saves the .blend.
import argparse, sys
_parser = argparse.ArgumentParser()
_parser.add_argument('--output', required=True, help='Absolute Unity Assets/_Project/Art/BlenderNuevo directory')
_parser.add_argument('--offset', nargs=3, type=float, default=(4.4492,.1941,-2.0265))
_options = _parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
import bpy,json,pathlib,re,hashlib
from mathutils import Vector
base=pathlib.Path(_options.output).resolve();out=base/'Bakes';out.mkdir(parents=True,exist_ok=True)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=1;s.cycles.device='CPU';s.render.bake.margin=8;s.view_settings.view_transform='Standard'
selected=[o for o in s.objects if o.type=='MESH' and any(slot.material and slot.material.name=='Wood Floor Golden Oak.003 casa' for slot in o.material_slots)]
results=[]
for idx,obj in enumerate(selected):
 try:
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;obj.data=obj.data.copy();old_uv=obj.data.uv_layers.active.name
  for i,slot in enumerate(obj.material_slots):
   if not slot.material:continue
   mat=slot.material.copy();obj.data.materials[i]=mat;slot.link='DATA';nodes=mat.node_tree.nodes;links=mat.node_tree.links;uv=nodes.new('ShaderNodeUVMap');uv.uv_map=old_uv
   for n in list(nodes):
    if n.type=='TEX_COORD':
     for l in list(n.outputs['UV'].links):links.new(uv.outputs['UV'],l.to_socket)
    elif n.type=='TEX_IMAGE' and not n.inputs['Vector'].is_linked:links.new(uv.outputs['UV'],n.inputs['Vector'])
   bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');surf=next(n for n in nodes if n.type=='OUTPUT_MATERIAL' and n.is_active_output);em=nodes.new('ShaderNodeEmission');base_color=bs.inputs['Base Color']
   if base_color.is_linked:links.new(base_color.links[0].from_socket,em.inputs['Color'])
   else:em.inputs['Color'].default_value=base_color.default_value
   links.new(em.outputs[0],surf.inputs['Surface'])
  new_uv=obj.data.uv_layers.new(name='BakedUV');obj.data.uv_layers.active=new_uv;new_uv.active_render=True
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
  im=bpy.data.images.new('BakedWood_'+str(idx),width=1024,height=1024,alpha=False)
  for slot in obj.material_slots:
   if not slot.material:continue
   nodes=slot.material.node_tree.nodes;node=nodes.new('ShaderNodeTexImage');node.image=im;nodes.active=node
  bpy.ops.object.bake(type='EMIT',margin=8)
  dst=out/('wood_'+str(idx).zfill(2)+'.png');im.filepath_raw=str(dst);im.file_format='PNG';im.save()
  obj.data.uv_layers.remove(obj.data.uv_layers.get(old_uv))
  simple=bpy.data.materials.new('BakedWood_'+str(idx));simple.use_nodes=True;nodes=simple.node_tree.nodes;bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');tex=nodes.new('ShaderNodeTexImage');tex.image=im;simple.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);obj.data.materials.clear();obj.data.materials.append(simple)
  mat=obj.matrix_world.copy();mat.translation+=Vector(_options.offset);obj.matrix_world=mat
  results.append({'object':obj.name,'image':'Assets/_Project/Art/BlenderNuevo/Bakes/'+dst.name,'material':simple.name,'vertices':len(obj.data.vertices),'faces':len(obj.data.polygons)})
  print('BAKED',obj.name,str(dst),flush=True)
 except Exception as e:print('BAKE_ERROR',obj.name,repr(e),flush=True);raise
bpy.ops.object.select_all(action='DESELECT')
for obj in selected:obj.select_set(True)
bpy.context.view_layer.objects.active=selected[0]
bpy.ops.export_scene.fbx(filepath=str(base/'Models'/'ViviendaNuevo_MoldurasBaked.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
(out/'bakes_manifest.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf8');print('BAKED_TOTAL',len(results),flush=True)

