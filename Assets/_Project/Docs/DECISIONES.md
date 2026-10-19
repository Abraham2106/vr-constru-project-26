# Decisiones de proyecto

## Agente 2 XR (2026-10-18)

- Rig basado en **XRI Starter Assets** (`XR Origin (XR Rig)`) guardado como `Assets/_Project/Prefabs/Rig/ProyectoVR_XR_Rig.prefab`.
- Locomoción: movimiento continuo (stick izquierdo), snap turn (derecho) y teletransporte XRI del sample; gravedad vía **CharacterController** del rig.
- Menú mundo ~1 m, toggle **menuButton / Y** (`Assets/_Project/Settings/ProyectoVR_MenuToggle.inputactions`).
- Día/noche: `ProyectoVR.Core.EnvironmentCycleEvents.RequestNightMode(bool)` + evento; reflexión a `SetDayNight(bool)` si otro agente lo publica.
- Medición y paleta: eventos estáticos (`MeasureToolEvents`, `WallPaletteEvents`) para Agentes 3/4.
- `PlayerSpawn` / `TP_Exterior|TP_Piso1|TP_Piso2`: posiciones provisionales frente a fachada hasta colliders finales (Agente 1).
- Escena de prueba: `Assets/_Project/Scenes/_Sandbox/XR.unity` (suelo simple). **No escribir** `Vivienda_Prueba` ni tomar `editor.lock` mientras el lock tenga actividad &lt; 30 min; Agente 2 trabaja prefabs + sandbox.
- **XR Device Simulator** se conserva en sandbox y escena main.

## Agente 1 modelo / materiales (2026-10-18)

- Escala **1 Blender unit = 1 m**; import FBX `globalScale: 1`, Lightmap UVs ON, **`isReadable: 1`** para MeshCollider.
- Puertas/ventanas fusionadas: placeholders en escena + lista Blender (`Reports/tareas_para_blender.md`).
- Prefab `Vivienda` se desempaqueta en Fase 1 para jerarquía/colliders; backup en `Art/Models/_Backup/Vivienda.fbx`.
- Layers: `Estructura`, `Interactuable`, `Teleport`. Rampa provisional `Escaleras_Rampa_PROV`.
- Pipeline Fase 1: asmdef `Vivienda.Agent1.Editor` en `Editor/Agente1/` (independiente de scripts XR rotos).
- Fallback material: `Art/Materials/Vivienda/Fallback_MissingTexture_PROV.mat`.
- **2026-10-18 04:28 UTC** — Agente 1: jerarquía Piso1/Piso2+Apt1/Apt2+Exterior; colliders Box=158 MeshConvex=14; missingMesh=0; placeholders puertas=9 ventanas=2; mats reparados=0. Prefab desempaquetado para editar escena.


## Agente 2 XR (2026-10-18 04:28)
- Rig basado en **XRI Starter Assets** (`XR Origin (XR Rig)`) guardado como prefab propio.
- Locomoción: movimiento continuo (stick izquierdo), snap turn (derecho) y teletransporte XRI ya incluidos en el sample.
- Menú mundo a ~1 m, toggle **menuButton / Y** (`ProyectoVR_MenuToggle.inputactions`).
- Día/noche vía `EnvironmentCycleEvents.RequestNightMode` + reflexión a `SetDayNight(bool)` si otro agente lo expone.
- `PlayerSpawn` / TP_* en main usan posiciones provisionales frente a fachada hasta colliders finales (Agente 1).
- Main scene: **no se escribe** mientras `editor.lock` tenga actividad < 30 min; Agente 2 no adquiere el lock.
- Main scene **no** modificada en esta pasada.
