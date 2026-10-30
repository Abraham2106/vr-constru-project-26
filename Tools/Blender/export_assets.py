# Run in Blender background on a COPY of the client source. Never saves the .blend.
import argparse, sys
_parser = argparse.ArgumentParser()
_parser.add_argument('--output', required=True, help='Absolute Unity Assets/_Project/Art/BlenderNuevo directory')
_parser.add_argument('--offset', nargs=3, type=float, default=(4.4492,.1941,-2.0265))
_options = _parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
import bpy,json,hashlib,re,pathlib,shutil
from mathutils import Vector
from bpy_extras.node_shader_utils import PrincipledBSDFWrapper
out=pathlib.Path(_options.output).resolve();(out/'Textures').mkdir(parents=True,exist_ok=True);(out/'Models').mkdir(exist_ok=True)
images={}; errors=[]
used={n.image for m in bpy.data.materials if m.users and m.use_nodes and m.node_tree for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image}
for im in sorted(used,key=lambda x:x.name):
 try:
  src=pathlib.Path(bpy.path.abspath(im.filepath));data=bytes(im.packed_file.data) if im.packed_file else src.read_bytes()
  sha=hashlib.sha256(data).hexdigest();ext=src.suffix.lower();ext=ext if ext in ('.png','.jpg','.jpeg','.tga','.tif','.tiff','.exr') else '.bin';stem=re.sub(r'[^a-zA-Z0-9_-]','_',src.stem or im.name)[:48];dest=out/'Textures'/(sha[:12]+'_'+stem+ext)
  if not dest.exists():dest.write_bytes(data)
  images[im.name]={'path':'Assets/_Project/Art/BlenderNuevo/Textures/'+dest.name,'sha256':sha,'size':list(im.size),'original_name':im.name,'color_space':im.colorspace_settings.name}
 except Exception as e:errors.append({'image':im.name,'error':str(e)})
mats=[]
for mat in sorted(bpy.data.materials,key=lambda x:x.name):
 if not mat.users:continue
 try:
  p=PrincipledBSDFWrapper(mat,is_readonly=True)
  def tex(t):return images.get(t.image.name) if t and t.image else None
  nodes=list(mat.node_tree.nodes) if mat.use_nodes and mat.node_tree else []
  mats.append({'name':mat.name,'base_color':list(p.base_color),'alpha':p.alpha,'metallic':p.metallic,'roughness':p.roughness,'base_color_texture':tex(p.base_color_texture),'normal_texture':tex(p.normalmap_texture),'all_images':[images[n.image.name] for n in nodes if n.type=='TEX_IMAGE' and n.image and n.image.name in images],'node_types':[n.type for n in nodes]})
 except Exception as e:errors.append({'material':mat.name,'error':str(e)})
objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.hide_render]
offset=Vector(_options.offset)
for o in objects:
 slots=[slot.material for slot in o.material_slots]
 indices=[p.material_index for p in o.data.polygons]
 o.data=o.data.copy();o.data.materials.clear()
 for mat in slots:o.data.materials.append(mat)
 for p,index in zip(o.data.polygons,indices):p.material_index=index
 for slot in o.material_slots:slot.link="DATA"
 mat=o.matrix_world.copy();mat.translation+=offset;o.matrix_world=mat
for group in ['Exterior','Piso1','Piso2']:
 chosen=[]
 for o in objects:
  n=o.name.lower();z=o.matrix_world.translation.z
  exterior=any(k in n for k in ('escaleras','cubierta','muro cortina','zacate','precintas','cielo raso'))
  g='Exterior' if exterior else ('Piso2' if z>=3.0 else 'Piso1')
  if g==group:chosen.append(o)
 bpy.ops.object.select_all(action='DESELECT')
 for o in chosen:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/'Models'/('ViviendaNuevo_'+group+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
 print('EXPORT',group,len(chosen))
manifest={'source':'faaaah.blend','source_sha256':hashlib.sha256(pathlib.Path(bpy.data.filepath).read_bytes()).hexdigest(),'blender_version':bpy.app.version_string,'source_to_original_offset':list(offset),'textures':images,'materials':mats,'errors':errors,'note':'Exportacion de trabajo: clasificacion por tipo y altura; verificar correspondencias antes de usar en escena.'}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
print('RESULT',len(objects),'objects',len(mats),'materials',len(images),'textures',len(errors),'errors');print(json.dumps(errors,ensure_ascii=False))
