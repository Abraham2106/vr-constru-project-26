# Fase 4 — QA Herramientas y Build (Agente 4)

| Campo | Valor |
|-------|--------|
| Unity | 6000.5.9f1 |
| Escena | `Assets/Scenes/Vivienda_Prueba.unity` |
| Agente | Herramientas, QA, build Quest |

## Scripts entregados

- `Assets/_Project/Scripts/Tools/` — MeasureTool, WallPaintTool, PaintableWall, ViviendaToolHost, hooks menú.
- `Assets/_Project/Editor/Agente4ToolsPipeline.cs` — setup prefabs, instalación escena, QA consola, reporte puerta.
- `Assets/_Project/Editor/Agente4AndroidBuild.cs` — APK IL2CPP ARM64 (API 29+).

## Unity MCP

- **Estado:** namespace `user-unityMCP` sin respuesta (timeout auth / discovery error). Trabajo vía scripts Editor + filesystem.

## Consola / compilación

- Escaneo inicial: ver `Assets/_Project/Docs/Logs/agente4_qa.md`.
- **Runtime (`ProyectoVR.Runtime.dll`):** compila tras fix `VrHapticsUtility` (solo warnings CS0618 deprecations).
- **Editor:** scripts Agente 4 en assembly `Vivienda.Agent4.Editor` (`Editor/Agente4/`). Otros editores (Agente 2/3) pueden seguir con errores hasta actualizar referencias asmdef.
- XR en editor: `XR runtime unavailable` esperado sin HMD.

## Build Android (SideQuest)

## Build Android — 2026-10-18 (inicial)

- **BLOQUEADO:** Unity Editor.log contiene `Native extension for Android target not found` — instalar **Android Build Support** (SDK, NDK, OpenJDK) desde Unity Hub. No se generó `Builds/ViviendaUnity.apk` (no simular éxito).

## Rendimiento Quest (objetivo 72 FPS)

| Métrica | Valor | Notas |
|---------|-------|--------|
| Draw calls (escena) | pendiente captura Stats | Agente 4 no alteró meshes; revisar tras bake Agente 2 |
| Luces realtime | pendiente | Preferir baked + 0–1 realtime en interiores |
| Texturas ASTC | pendiente | Aplicar en import Android (Agente 2 / Quality) |

## Medición puerta

- Referencia bounds Blender: **0,96 m** ancho — ver `medicion_puerta.md`.
- MeasureTool: 1 unidad = 1 m, etiqueta `F2` metros.
