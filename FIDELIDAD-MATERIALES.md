# Fidelidad de materiales Blender → Unity

Se completaron canales PBR de **20 sets correspondientes a 18 materiales fuente**, dentro de `Assets/Vivienda/Fidelity/`. Este alcance excluye Grass 01 —tratado por separado— y los dos materiales constantes autorizados Microcemento mate y Blanco mate. Walnut wood conserva su identidad y las 24 asignaciones autorizadas, ahora con sus canales fuente.

Los 16 clones generales —incluidos vidrio y nogal— se aplicaron y guardaron en `Assets/Scenes/Vivienda_Prueba.unity`. Los cuatro materiales de atlas de los dos suelos quedaron creados, importados y preparados para el ajuste de UV/submeshes, para asignarlos junto con la restauración de sus dos slots. Este informe acredita materiales y canales; la comprobación posterior de esa asignación y la revisión visual global se realizan en la pasada final del proyecto. La escena principal y los materiales originales no fueron modificados por esta pasada.

## Fuente y diagnóstico

Se comprobó la sesión Blender abierta: `C:/Users/solan/Downloads/Untitled.blend`, con cambios sin guardar (`is_dirty=true`). Se utilizó la copia canónica de esa sesión `work/fidelity/source_snapshot.blend`, creada sin sobrescribir la fuente. Las evaluaciones se ejecutaron con Blender 5.2.2 LTS en procesos de segundo plano, con Cycles.

La importación anterior omitía mapas conectados de rugosidad, metalicidad, especular y algunas cadenas normales. Valores de roughness visibles en sockets sin enlazar no representan la salida real cuando hay conexiones. Casos comprobados:

- Acoustic Ceiling Tile usa tres Mix enlazados: su color fuente es gris oscuro, con metallic y roughness variables; no es blanco uniforme.
- Black steel usa metallic y specular por textura e inversión de roughness.
- Brown Polished Oak Wood mezcla dos texturas de imperfecciones para roughness.
- Beige Painted Plaster Wall y MetalPanel05 usan Specular IOR Level 0.10 y 0.14; Standard metallic con F0 fijo no representa esa diferencia.
- Walnut wood combina roughness por Color Ramp, specular por textura y Normal Map + Bump; el material anterior solo tenía albedo y roughness constante.
- Ceramic Tiles (Floor Tiles) evalúa coordenadas Object dentro de un grupo procedural. Sus dos suelos requieren un atlas por objeto; el material secundario de cada suelo también se horneó sobre el mismo atlas, conservando explícitamente el UV automap original como coordenada de muestreo.
- Los dos suelos fuente poseen dos materiales cada uno. Sus polígonos, loops y materiales originales se exportaron para restituir la partición de submeshes sin cambiar posiciones.

## Conversión de canales

Se utilizó el shader integrado disponible y compatible `Standard (Specular setup)`. La salida RGB del mapa SpecGloss representa reflectancia especular; su alpha contiene smoothness, según la [documentación de Unity](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/built-in/shader-built-in-birp/shader-standard-shader/standard-shader-material-parameters).

Se evaluaron las salidas conectadas Base Color, Roughness, Metallic y Specular IOR Level. La conversión técnica aplicada, en espacio lineal, es:

```
F0_dieléctrico = ((IOR - 1) / (IOR + 1))² × 2 × SpecularIORLevel
SpecGloss.rgb = lerp(F0_dieléctrico, BaseColor, Metallic)
SpecGloss.a   = 1 - clamp(Roughness, 0, 1)
Diffuse.rgb  = BaseColor × (1 - Metallic)
```

El factor relativo de Specular IOR Level sigue la [definición de Blender](https://docs.blender.org/manual/en/4.0/render/shader_nodes/shader/principled.html): 0.5 mantiene la reflectancia nominal y 1 la duplica a incidencia normal. Esta conversión conserva parámetros y variaciones espaciales, pero **no demuestra equivalencia exacta entre los BRDF** de Principled y Standard.

Se conservaron **12 albedos originales** donde el nodo Base Color recibía directamente una imagen y metallic era cero. Se generó diffuse evaluado para los otros ocho sets, incluyendo cuatro atlas. Los mapas normales recuperan las operaciones de inversión de canal existentes en los nodos fuente; el importador no vuelve a invertir G. La fuerza de Normal Map se conserva cuando procede. Para cadenas Bump se evaluó una normal tangente: en nogal se aproxima sobre un plano UV; en los suelos se horneó sobre la geometría del objeto con su atlas.

No se añadieron AO ni emisión inventados. Los materiales inspeccionados no mostraron un canal AO independiente efectivo y sus emisiones evaluadas eran negras. La microtextura incorporada a albedos o a nodos originales permanece en su evaluación.

## Sets preparados

Todos tienen mapa SpecGloss. La resolución corresponde a los canales generados; los albedos originales conservados mantienen su resolución de archivo.

| Material / variante | Color | Normal | Resolución |
|---|---|---|---:|
| MetalPanel05 | Albedo original | Sí | 1024 |
| Wood | Albedo original | Sí | 1024 |
| Plastic | Albedo evaluado | No procede | 1024 |
| Black steel | Albedo evaluado | Sí | 1024 |
| Brown Polished Oak Wood | Albedo original | Sí | 1024 |
| Beige Painted Plaster Wall | Albedo original | Sí | 1024 |
| Stone wall.001 | Albedo original | Sí | 1024 |
| Natural Wooden Slat Panel | Albedo original | Sí | 1024 |
| Concrete tiles.001 | Albedo original | Sí | 1024 |
| Acoustic Ceiling Tile | Albedo evaluado | Sí | 1024 |
| Black Ceiling | Albedo original | Sí | 1024 |
| Concrete tiles | Albedo original | Sí | 1024 |
| New asphalt | Albedo original | Sí | 1024 |
| Ceramic Black Tiles English | Albedo original | Sí | 1024 |
| Ceramic Tiles (Floor Tiles) · 172746 / slot 0 | Albedo evaluado | Sí | 2048 |
| Ceramic Tiles (Floor Tiles) · 174640 / slot 0 | Albedo evaluado | Sí | 2048 |
| Glass.001 | Albedo evaluado | No procede | 2 |
| Walnut wood | Albedo original | Sí | 1024 |
| Ceramic Black Tiles English · 172746 / slot 1 | Albedo evaluado | Sí | 2048 |
| Gray Ceramic Tile · 174640 / slot 1 | Albedo evaluado | Sí | 2048 |

## Dos suelos: atlas y pertenencia por material

Se prepararon cuatro materiales:

| Objeto | Slot | Asset en Fidelity/Materials |
|---|---:|---|
| Suelo Hormigón [172746] | 0 | Ceramic Tiles (Floor Tiles)_172746.mat |
| Suelo Hormigón [172746] | 1 | Ceramic Black Tiles English_172746.mat |
| Suelo Piso 1 [174640] | 0 | Ceramic Tiles (Floor Tiles)_174640.mat |
| Suelo Piso 1 [174640] | 1 | Gray Ceramic Tile_174640.mat |

Cada atlas usa 2048 × 2048, padding de horneado de 8 píxeles y margen de islas 0.015. Se verificaron UV dentro de [0,1], no degenerados. Los JSON contienen posición y normal por loop, UV originales, UV atlas, polígonos, triangulación y material fuente. La reconstrucción de los clones Unity a partir de las UV conserva las superficies y la geometría; esta etapa de preparación de materiales no modificó las mallas de Unity.

## Vidrio: aproximación explícita

Glass.001 mezcla Transparent, Glossy y Glass BSDF con lógica Light Path dependiente del tipo de rayo. Se identificó el output activo, distinto del output alternativo inactivo. Standard no reproduce esa lógica ni refracción física.

El clon usa Transparent premultiplicado, alpha 0.5 inspirado en el Mix final de la fuente, tinte RGB lineal (0.05269, 0.05639, 0.07492), IOR 1.45 convertido a F0 ≈ 0.03374 y smoothness 1. El tinte se guarda sin premultiplicar y el shader aplica la premultiplicación una sola vez. Se preservan transparencia y reflejo aproximados, sin afirmar coincidencia exacta con Blender ni transmisión física.

## Importación, precisión y límites

- Se importaron **46 texturas nuevas requeridas**: 20 SpecGloss, 18 normales y 8 diffuse/base. Los albedos tienen sRGB activado; SpecGloss y normales se importan como datos lineales.
- Los PNG generados son RGBA de **16 bits por canal**, verificado en la cabecera IHDR. La importación Unity está sin compresión con selección automática del formato: esto no garantiza 16 bits efectivos por canal en GPU.
- Los albedos evaluados de estos sets tenían máximo ≤ 1; no se detectó pérdida HDR en ellos. Grass 01 tiene un tratamiento HDR independiente.
- Los mapas generales son 1024² y los atlas 2048². El detalle por encima de esas resoluciones puede diferir de las imágenes fuente.
- La conversión no mueve vértices ni reproduce desplazamiento real del material. Las ramas Displacement no se convierten en nueva geometría. El bump tangente no sustituye desplazamiento geométrico.
- Se restauran canales y coordenadas, pero iluminación, gestión de color, entorno, refracción, BRDF y evaluación procedural del motor también influyen en la apariencia. La revisión visual final debe considerar el conjunto.

## Verificación y evidencia

Se verificó el shader instalado mediante `Shader.Find` e `isSupported`, y se consultaron las API reales de TextureImporter. La aplicación general confirmó `updated=16`, `sceneSaved=true`; la creación de los atlas confirmó `createdOrUpdated=4`, `saved=true`. No se guardó la escena principal en esta pasada.

La comprobación reproducible `work/fidelity/verify_channel_contract.ps1` pasó: 20 rutas de materiales distintas, 18 nombres fuente, mapas requeridos presentes, todos los materiales de destino bajo Fidelity y dos atlas con datos de pertenencia y UV válidos.

La evidencia técnica queda en `work/fidelity/`: `channel_nodes.json`, `source_nodes.json`, `channels/manifest.json`, `material_channel_mapping.json`, los scripts de bake, `apply_channel_materials.cs.txt`, `apply_atlas_materials.cs.txt` y `atlas_material_application_evidence.json`. Los sets auxiliares no destinados al proyecto permanecen solo en work y no se importaron.
