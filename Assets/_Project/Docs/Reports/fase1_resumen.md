# Fase 1 — resumen Agente 1
- Escena: `Assets/Scenes/Vivienda_Prueba.unity` guardada.
- Escala: 1 u = 1 m (puertas ~0.96×2.13 m en Blender).
- Jerarquía bajo Vivienda: Estructura/Piso1|Piso2, Apt1, Apt2, Exterior, Colliders.
- Colliders: 158 Box, 14 MeshCollider convex.
- Materiales magenta/null reparados: 0.
- Puertas/ventanas: **fusionadas en FBX** + placeholders en escena.

## Agente 2 — Materiales y texturas
- **Fecha:** 2026-10-18 04:39:13
- **Pipeline:** URP (objetivo URP; ver `Assets/_Project/Settings/URP_Vivienda.asset` cuando el paquete URP termine de importar).
- **Copias:** `Assets/_Project/Art/Materials/Vivienda`, `Assets/_Project/Art/Textures/Vivienda` (originales intactos en `Assets/Vivienda/`).
- **Biblioteca:** `Assets/_Project/Art/Materials/Library/` (Muro_Pintura, Piso_Ceramica, Piso_Madera, Techo, Vidrio, Metal_Manija, Madera_Puerta, Sanitario).
- **Conversión shader:** 0 materiales.
- **Sin albedo:** 33; ver `materials_needing_bake.md`.
- **Magenta pendiente:** 0 (debe ser 0 tras URP activo).
- **Log detallado:** `Assets/_Project/Docs/Logs/agente2_materiales.md`
- **Lightmaps:** habilitar UV2 en import del FBX antes de bake (no ejecutado en fase 1).
