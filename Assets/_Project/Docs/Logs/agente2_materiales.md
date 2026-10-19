# Agente 2 — Materiales y texturas

**Proyecto:** `C:\Users\solan\Documents\Codex\2026-10-18\c\outputs\ViviendaUnity`  
**Escena:** `Assets/Scenes/Vivienda_Prueba.unity`  
**Modelo:** `Assets/Vivienda/Vivienda.fbx` (Blender: `C:\Users\solan\Downloads\Untitled.blend`)

## Pipeline (verificado)

| Item | Valor |
|------|--------|
| **Activo hoy** | Built-in (`ProjectSettings/GraphicsSettings.asset` → `m_CustomRenderPipeline: none`) |
| **Objetivo** | URP |
| **Paquete** | `com.unity.render-pipelines.universal` **17.1.0** en `Packages/manifest.json` (importado en `Library/PackageCache`) |
| **Magenta** | Típico con URP activo + shader Standard; con Built-in actual los ~112 materiales usan Standard/Specular (válidos) |
| **Gris** | ~31 materiales sin `_MainTex` (color sólido, vidrio, procedurales, o texturas no embebidas en FBX) |

## FBX / import

- Materiales **externos** (`materialLocation: 1`) remapeados en `Vivienda.fbx.meta` → `Assets/Vivienda/materials/`.
- Texturas en `Assets/Vivienda/textures/` y `Assets/Vivienda/Fidelity/`.
- **UV lightmap:** `generateSecondaryUV: 0` — activar en import antes de bake (no bake en fase 1).

## Copias (_Project, originales intactos)

| Origen | Destino |
|--------|---------|
| `Assets/Vivienda/materials/` | `Assets/_Project/Art/Materials/Vivienda/` (**112** `.mat`) |
| `Assets/Vivienda/textures/` | `Assets/_Project/Art/Textures/Vivienda/` (**740** archivos) |
| Fidelity Materials/Textures | `.../Vivienda/Fidelity/` |

Unity reasignó GUID en texturas duplicadas (conflictos esperados vs originales).

## Biblioteca mínima

`Assets/_Project/Art/Materials/Library/`:

- `Muro_Pintura.mat`, `Piso_Ceramica.mat`, `Piso_Madera.mat`, `Techo.mat`, `Vidrio.mat`, `Metal_Manija.mat`, `Madera_Puerta.mat`, `Sanitario.mat`

## Materiales pintables (documentación; escena = Agente 1)

Materiales con nombre tipo muro/pintura (asignar `PaintableWall` en runtime o fase posterior):

- `Blanco mate`, `Microcemento mate`, `Beige Painted Plaster Wall`, `Painted Plaster Wall`, `Stone wall*`, `Plaster*`, etc.

Script: `Assets/_Project/Scripts/PaintableWall.cs`

## Procedurales / bake

Ver `Assets/_Project/Docs/Reports/materials_needing_bake.md`

- `Procedural Grass`, `Procedural Brown Leather`, `Procedural pearl blue painted plastic`
- Parche manual: `Procedural Grass` en copia `_Project` enlazado a texturas de `Grass 01`

## Automatización Editor

- **Menú:** `Vivienda → Agente 2 → Ejecutar pipeline materiales`
- **Auto-run:** `Assets/_Project/Docs/Logs/agente2_autorun.flag` o `Assets/_Project/.agente2_pending`
- **Script:** `Assets/_Project/Editor/Agente2MaterialPipeline.cs`  
  - Remapea FBX a materiales `_Project`  
  - Convierte a URP Lit cuando `GraphicsSettings.defaultRenderPipeline` esté asignado  
  - Ajusta import de normales/data maps  
  - Regenera reportes  

> **Estado auto-run:** pendiente si el flag sigue presente; el Editor estaba compilando/importando URP + texturas. Ejecutar menú si no se borró el flag.

## Unity MCP

- `user-unityMCP`: **no disponible** (error de conexión / timeout). Sin capturas de consola ni GameView en esta pasada.

## Consola (objetivo)

- 0 materiales magenta tras URP + conversión.
- 0 missing shader en materiales `_Project` tras pipeline.
- Revisar habitaciones en `Vivienda_Prueba` tras conversión URP.


---

# Agente 2 — Materiales y texturas

**Ejecutado:** 2026-10-18 04:16:59

- Lock editor adquirido (Agente2-Materiales).
## Pipeline
- **Actual:** UniversalRenderPipelineAsset (`Assets/_Project/Settings/URP_Vivienda.asset`)
## Pipeline
- **Actual:** UniversalRenderPipelineAsset (`Assets/_Project/Settings/URP_Vivienda.asset`)
- Copia de assets: **omitida** (ya presente en `_Project/Art`).
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Beige Painted Plaster Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Blanco mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Beige Painted Plaster Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Blanco mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Microcemento mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Stone wall.001.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Microcemento mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Natural Stone Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Nature wall with gras.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Painted Plaster Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Procedural pearl blue painted plastic.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Rough Granite Stone Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Scaned Stone Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone Brick Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone Wall Natural.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone wall.001.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Wall made of large natural stones.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/White Painted Wood.001.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/White Painted Wood.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Library/Muro_Pintura.mat` — usar `_BaseColor` / `_Color`
## Biblioteca `_Project/Art/Materials/Library`
- `Assets/_Project/Art/Materials/Library/Muro_Pintura.mat` ← donor `Beige Painted Plaster Wall`
- `Assets/_Project/Art/Materials/Library/Piso_Ceramica.mat` ← donor `Gray Ceramic Tile`
- `Assets/_Project/Art/Materials/Library/Piso_Madera.mat` ← donor `Oak Wood`
- `Assets/_Project/Art/Materials/Library/Techo.mat` ← donor `Acoustic Ceiling Tile`
- `Assets/_Project/Art/Materials/Library/Vidrio.mat` ← donor `Glass`
- `Assets/_Project/Art/Materials/Library/Metal_Manija.mat` ← donor `Black steel`
- `Assets/_Project/Art/Materials/Library/Madera_Puerta.mat` ← donor `Walnut wood`
- `Assets/_Project/Art/Materials/Library/Sanitario.mat` ← donor `White Ceramic Tiles`
- Captura: `Assets/_Project/Docs/Reports/captures\exterior_fachada.png`
- Captura: `Assets/_Project/Docs/Reports/captures\apt1_sala.png`

## Resumen técnico
- Pipeline detectado: **URP**
- Materiales copiados/remapeados FBX: **112** entradas en mapa GUID
- Materiales convertidos de shader: **112**
- Materiales con texturas prestadas: **8**
- Materiales sin albedo (solo color o vacío): **33**
- Shaders error (magenta): **0**

## Nota lightmaps
- `Vivienda.fbx` importa con `generateSecondaryUV: 0`. Para bake de luz, habilitar **Generate Lightmap UVs** en el mesh import o unwrap en DCC.


---

# Agente 2 — Materiales y texturas

**Ejecutado:** 2026-10-18 04:30:17

- Lock editor adquirido (Agente2-Materiales).
## Pipeline
- **Actual:** UniversalRenderPipelineAsset (`Assets/_Project/Settings/URP_Vivienda.asset`)
## Pipeline
- **Actual:** UniversalRenderPipelineAsset (`Assets/_Project/Settings/URP_Vivienda.asset`)
- Copia de assets: **omitida** (ya presente en `_Project/Art`).
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Beige Painted Plaster Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Blanco mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Beige Painted Plaster Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Blanco mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Microcemento mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Fidelity/Stone wall.001.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Microcemento mate.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Natural Stone Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Nature wall with gras.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Painted Plaster Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Procedural pearl blue painted plastic.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Rough Granite Stone Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Scaned Stone Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone Brick Wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone Wall Natural.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone wall.001.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Stone wall.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/Wall made of large natural stones.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/White Painted Wood.001.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Vivienda/White Painted Wood.mat` — usar `_BaseColor` / `_Color`
- **Pintable (material):** `Assets/_Project/Art/Materials/Library/Muro_Pintura.mat` — usar `_BaseColor` / `_Color`
## Biblioteca `_Project/Art/Materials/Library`
- `Assets/_Project/Art/Materials/Library/Muro_Pintura.mat` ← donor `Beige Painted Plaster Wall`
- `Assets/_Project/Art/Materials/Library/Piso_Ceramica.mat` ← donor `Gray Ceramic Tile`
- `Assets/_Project/Art/Materials/Library/Piso_Madera.mat` ← donor `Oak Wood`
- `Assets/_Project/Art/Materials/Library/Techo.mat` ← donor `Acoustic Ceiling Tile`
- `Assets/_Project/Art/Materials/Library/Vidrio.mat` ← donor `Glass`
- `Assets/_Project/Art/Materials/Library/Metal_Manija.mat` ← donor `Black steel`
- `Assets/_Project/Art/Materials/Library/Madera_Puerta.mat` ← donor `Walnut wood`
- `Assets/_Project/Art/Materials/Library/Sanitario.mat` ← donor `White Ceramic Tiles`
- Captura: `Assets/_Project/Docs/Reports/captures\exterior_fachada.png`
- Captura: `Assets/_Project/Docs/Reports/captures\apt1_sala.png`

## Resumen técnico
- Pipeline detectado: **URP**
- Materiales copiados/remapeados FBX: **112** entradas en mapa GUID
- Materiales convertidos de shader: **0**
- Materiales con texturas prestadas: **8**
- Materiales sin albedo (solo color o vacío): **33**
- Shaders error (magenta): **0**

## Nota lightmaps
- `Vivienda.fbx` importa con `generateSecondaryUV: 0`. Para bake de luz, habilitar **Generate Lightmap UVs** en el mesh import o unwrap en DCC.
