# Escena interior — iluminación (`Vivienda_Prueba`)

**Fecha:** 2026-10-18  
**Escena:** `Assets/Scenes/Vivienda_Prueba.unity`  
**Pipeline:** URP (`URP_Vivienda`)

## Problema

El interior (sala, escaleras, piso superior) se veía negro al entrar: solo luz direccional exterior, sin fuentes dentro del volumen cerrado.

## Cambios (sin tocar CharacterController, spawn, bisagras, colliders)

| Elemento | Detalle |
|----------|---------|
| **Luz_Piso1** | Point Light, centro piso 1 `(≈1.19, 2.15, 1.87)`, intensidad **5**, rango **12 m**, color cálido |
| **Luz_Piso2** | Point Light, centro piso 2 `(≈1.19, 5.15, 1.56)`, intensidad **4.5**, rango **7.5 m** (ajustado para no mezclar pisos en Quest), color cálido |
| **Sombras** | **Apagadas** en ambas (`LightShadows.None`, `m_Shadows.m_Type: 0`) |
| **Modo luz** | Realtime (sin bake) |
| **Ambient** | Flat, intensidad **1.5**, relleno suave en la escena |
| **Techos** | 4 mallas `Cielo Raso` / `Techo` con emisión URP ligera (instancias en escena, bake-free) |

## Presupuesto Quest

- Exterior: **1** direccional (ya existente, sombras soft).
- Interior: **1** point por piso visible con rangos acotados → típicamente **≤2** luces adicionales por vista al estar en un solo piso.

## Ubicación luces

- Centradas en el volumen de cada piso, altura ~2.15 m sobre suelo → **no bloquean puertas** (puertas en perímetro, no en el centro).

## Verificación

- Captura URP (cámara temporal, jugador intacto): `Assets/_Project/Docs/Reports/captures/interior_luz.png`
- Métricas editor (cámara URP de prueba): sala avg ≈ **0.36**, piso 2 avg ≈ **0.53** (escala 0–1 por canal).

## Unity MCP

- `editor.lock`: **no presente** al guardar (sin espera de 30 min).
- Escena guardada vía `EditorSceneManager.SaveOpenScenes()`.

## Estado (ES)

**Luces interiores añadidas:** `Luz_Piso1` y `Luz_Piso2`. **Sombras desactivadas** en ambas. Interior legible con point lights + emisión de techos + ambient reforzado.
