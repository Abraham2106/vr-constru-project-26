# Correcciones comprobadas de apariencia Blender → Unity

Se conservaron el `.blend` fuente, el FBX y la geometría. No se cambiaron el render pipeline ni el espacio de color Gamma del proyecto.

- **Grass 01:** el exportador solo inspeccionaba Principled BSDF del nivel superior y omitió el grupo Grass completo. Se hornearon Base Color, Roughness y Metallic del Principled interno con las entradas reales del grupo; solo se neutralizó el mapping para producir un tile. Se preservó el albedo sRGB sin iluminación. Se reconstruyó la transformación UV exacta evaluando el grafo original: `u'=4.74722199u-1.56967620v`, `v'=1.56967620u+4.74722199v`, equivalente a escala5 y giro18.3°. Mesh clon con UV corregidas y tangentes recalculadas, normal original con fuerza0.75. Metallic/smoothness empaquetado en R/A. Adaptación visual explícita: tint del material `(0.4,0.5,0.4,1)` para moderar saturación bajo la iluminación Gamma existente; no es un dato PBR recuperado del fuente.
- **Stone wall.001:** el objeto678379 usa `automap` como UV activa/render en Blender, que llega como segundo canal de Unity; el Standard estaba leyendo el primero. Un mesh clon copia UV1→UV0 y recalcula tangentes. Se añadió mapa metallic/smoothness R/A desde roughness original. La escala de piedra de esa pared coincide ahora con el fuente.
- **MetalPanel05:** se recuperó el normal omitido (el fuente invierte Y antes del nodo Normal Map) usando NormalMap importer, `flipGreenChannel=true`, fuerza2. Se añadió mapa smoothness a partir de roughness. Albedo original y UV sin cambio.
- **Vidrio:** los materiales relevantes ya estaban en Standard Transparent. No se modificaron por falta de un defecto acotado adicional demostrado. La refracción/reflexión Cycles y shaders mixtos no se reproducen exactamente con Standard.

## Artefactos

- `Assets/Vivienda/CorrectedTextures/`:4PNG (albedo césped y3metallic/smoothness), metadata con rangos y affine.
- `Assets/Vivienda/CorrectedMeshes/Grass01_UV.asset`
- `Assets/Vivienda/CorrectedMeshes/StoneWall_AutomapUV.asset`
- Materiales existentes: `Grass 01.mat`, `Stone wall.001.mat`, `MetalPanel05.mat`.
- Escena guardada: `Assets/Scenes/Vivienda.unity`.
- Capturas: `Assets/Screenshots/apariencia_antes.png`, `Assets/Screenshots/apariencia_corregida-1.png`, `Captures/apariencia_corregida_1920.png`.

## Verificación

178renderers en raíz Vivienda,0slots nulos/Default-Material después de estas correcciones. Mapas y keywords `_NORMALMAP`/`_METALLICGLOSSMAP` comprobados en3materiales. Consola:0errores/advertencias. Revisión de captura: tamaño piedra recuperado, líneas/relieve del techo visibles, césped verde con textura original (detalle fino limitado por distancia/mipmapping en vista general). No se persiguió igualdad de iluminación o color management con Cycles/AgX.

## Evidencia y reproducción

En `../../work/`: `appearance_inspect.json`, `appearance_deep.json`, `prepare_appearance_assets.py`, `appearance_assets/appearance_asset_metadata.json`. El script de preparación usa Blender background y no guarda cambios al fuente. La variante de codificación lineal fue explorada como diagnóstico y movida fuera de Assets, no asignada.

## Documentación primaria consultada

- [Blender: Render Baking](https://docs.blender.org/manual/en/4.4/render/cycles/baking.html): bake a textura requiere UV y un nodo imagen destino activo; canales de bake separan color de iluminación.
- [Unity6: normal maps](https://docs.unity3d.com/6000.0/Documentation/Manual/StandardShaderMaterialParameterNormalMap.html): Unity usa normalesY+ (OpenGL); normal maps simulan relieve sin aumentar geometría.
- [Unity6: Metallic property](https://docs.unity3d.com/6000.0/Documentation/Manual/StandardShaderMaterialParameterMetallic.html): Standard empaqueta metallic y smoothness, con roughness invertida para smoothness.
- [Unity6: Rendering Mode](https://docs.unity3d.com/6000.0/Documentation/Manual/StandardShaderMaterialParameterRenderingMode.html): Transparent conserva reflejos especulares para vidrio.
