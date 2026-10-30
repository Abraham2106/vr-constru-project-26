# Run in Blender background on a COPY of the client source. Never saves the .blend.
import argparse, sys
_parser = argparse.ArgumentParser()
_parser.add_argument('--output', required=True, help='Absolute Unity Assets/_Project/Art/BlenderNuevo directory')
_parser.add_argument('--offset', nargs=3, type=float, default=(4.4492,.1941,-2.0265))
_options = _parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
import bpy,json,pathlib
out=pathlib.Path(_options.output).resolve();base=json.loads((out/'manifest.json').read_text(encoding='utf8')); images=base['textures'];result=[]
def texture(node_input):
 if not node_input or not node_input.is_linked:return None
 node=node_input.links[0].from_node
 if node.type=='TEX_IMAGE' and node.image:return images.get(node.image.name)
 if node.type=='NORMAL_MAP':return texture(node.inputs.get('Color'))
 return None
for m in sorted(bpy.data.materials,key=lambda x:x.name):
 if not m.users or not m.use_nodes or not m.node_tree:continue
 ns=list(m.node_tree.nodes);bs=next((n for n in ns if n.type=='BSDF_PRINCIPLED'),None);mp=next((n for n in ns if n.type=='MAPPING'),None)
 if not bs:result.append({'name':m.name,'supported':False,'reason':'No direct principled shader'});continue
 def inp(name):return bs.inputs.get(name)
 def linked_unsupported(name):return bool(inp(name) and inp(name).is_linked and texture(inp(name)) is None)
 def val(name):return float(inp(name).default_value) if inp(name) and not inp(name).is_linked else None
 data={'name':m.name,'supported':not any(linked_unsupported(k) for k in ['Base Color','Roughness','Normal','Metallic']),'base':texture(inp('Base Color')),'normal':texture(inp('Normal')),'rough':texture(inp('Roughness')),'metallic_map':texture(inp('Metallic')),'metallic':val('Metallic'),'roughness':val('Roughness'),'base_color':list(inp('Base Color').default_value),'scale':list(mp.inputs['Scale'].default_value)[:2] if mp else [1,1],'offset':list(mp.inputs['Location'].default_value)[:2] if mp else [0,0],'rotation':list(mp.inputs['Rotation'].default_value) if mp else [0,0,0]}
 result.append(data)
(out/'materiales_grafo.json').write_text(json.dumps({'materials':result},ensure_ascii=False,indent=2),encoding='utf8');print('SUPPORTED',sum(x['supported'] for x in result),'COMPLEX',sum(not x['supported'] for x in result));print('COMPLEX_NAMES',*[x['name'] for x in result if not x['supported']],sep='\n')
