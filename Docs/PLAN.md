# Plan — Swarm ViviendaUnity

## Agente 4 (este documento — herramientas y QA)

1. **MeasureTool** — activación menú, raycast gatillo, segmentos LineRenderer + TMP, undo/clear, escala 1 u = 1 m.
2. **WallPaintTool** — paleta arquitectónica, solo `PaintableWall`, restore MPB.
3. **Integración** — `ViviendaToolHost` en XR Origin, hooks estáticos para menú Agente 3.
4. **QA** — escaneo `Editor.log`, regresiones en `Docs/Reports/regresiones.md` si aplica.
5. **Build Quest** — APK IL2CPP ARM64 API 29+ cuando compile 0 errores y SDK Android presente.

## Dependencias cruzadas

| Agente | Entrega | Hook Agente 4 |
|--------|---------|----------------|
| 3 | Menú XR | `ViviendaMenuHooks.*` |
| 1 | Colliders / muros | `PaintableWall` tagger Editor |
| 2 | URP materiales | `_BaseColor` pintura |

## Loop continuo

- Mantener checklist con evidencia rutas archivo.
- Reintentar build tras fix SDK Android.
- Medición puerta ~0,9 m documentada en `medicion_puerta.md`.
