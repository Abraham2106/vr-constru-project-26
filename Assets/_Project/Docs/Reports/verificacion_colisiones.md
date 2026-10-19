# Verificación de colisiones — Vivienda_Prueba

| Campo | Valor |
| --- | --- |
| Fecha | 2026-10-18 |
| Escena | `Assets/Scenes/Vivienda_Prueba.unity` |
| Modo | Solo inspección (Editor). **No** Play, **no** guardar escena. |
| `editor.lock` | No presente en `Assets/_Project/Docs/.locks/editor.lock` |
| Herramienta | Unity MCP (`Unity_RunCommand`, `Unity_GetConsoleLogs`) |

## Resumen ejecutivo

| Criterio | Veredicto |
| --- | --- |
| **walls-block** (muros bloquean cuerpo del jugador) | **PASA** |
| **stairs-climbable** (rampa escaleras subible con `CharacterController`) | **PASA** (con observaciones de capa/gravedad) |

---

## 1. Colliders en suelos, muros exteriores y rampa

### Contenedor y conteos (bajo `Vivienda`)

- Contenedor `Colliders`: **OK**
- Totales: **170** `BoxCollider`, **14** `MeshCollider` convexos, **0** triggers deshabilitados para física (`isTrigger=false` en todos los colliders auditados)
- Distribución por capa (colliders bajo `Vivienda`):
  - **Estructura** (6): 126
  - **Teleport** (8): 38
  - **Default** (0): 11
  - **Interactuable** (7): 9

### Suelos y muros (heurística por nombre)

- Objetos con nombre tipo Piso/Suelo/Floor/Losa: **37** con collider en el GO, **2** contenedores (`Piso1`, `Piso2`) sin collider propio pero **con colliders en hijos**
- Objetos tipo Muro/Wall/Tabique: **77** con collider, **0** sin collider

### Exterior

- Contenedor `Exterior`: **5** colliders
  - **4** muros exteriores: `BoxCollider`, capa **Estructura**, no trigger
  - **1** suelo exterior (`Suelo Zacate [693790]`): `BoxCollider`, capa **Teleport**, no trigger

### Rampa escaleras

- Objeto: `Vivienda/Colliders/Escaleras_Rampa_PROV`
- **BoxCollider** presente, **enabled**, **no trigger**
- Capa: **Teleport** (8)
- Tamaño: `(1.10, 0.15, 4.20)` m; inclinación local ~**25°** en X (límite CC **45°**)

### Capa del cuerpo del jugador y matriz de colisión

- `CharacterController` en escena (prefab instanciado): **`ProyectoVR_XR_Rig`**, capa **`Ignore Raycast`** (2)
- `GroundedXrLocomotionProfile`: presente en el mismo rig
- Matriz de física del proyecto: **todas las capas colisionan entre sí** (`LayerCollisionMatrix` completamente activa)
- Comprobación explícita capa jugador (`Ignore Raycast`) vs:
  - **Default**: COLISIONA
  - **Estructura**: COLISIONA
  - **Teleport**: COLISIONA
  - **Interactuable**: COLISIONA

**Conclusión:** suelos interiores y muros de carga/tabique están mayormente en **Estructura**; el cuerpo del jugador **sí** choca con esa capa. La rampa está en **Teleport**, también colisiona con el jugador a nivel de `CharacterController`.

**Nota locomoción / gravedad XRI:** `WalkableLayerMask` solo incluye **Default + Estructura** (bits `65`), **no Teleport**. Eso afecta raycasts de suelo de `GravityProvider` / `PlayerSpawnBootstrap`, **no** la tabla de colisiones del `CharacterController`.

---

## 2. Cápsula del jugador (anti-túnel)

| Parámetro | Valor en escena | Referencia código |
| --- | --- | --- |
| `radius` | **0.30** m | `GroundedXrLocomotionProfile`: 0.3 |
| `height` | **1.70** m | 1.7 |
| `skinWidth` | 0.02 m | 0.02 |
| `stepOffset` | 0.40 m | 0.4 |
| `slopeLimit` | 45° | 45 |
| ¿Trigger? | No (es `CharacterController`, no trigger) | — |

**Conclusión:** radio dentro del rango esperado (0.2–0.4 m); no es un trigger minúsculo. Adecuado para evitar túnel a través de muros con colliders sólidos.

---

## 3. Rampa escaleras (`Escaleras_Rampa_PROV`)

| Requisito | Estado |
| --- | --- |
| `BoxCollider` | OK |
| No trigger | OK |
| Pendiente subible (~25° < 45°) | OK |
| Raycast de prueba sobre capa Teleport | OK (impacta la rampa) |

**Observación:** al estar en **Teleport**, la rampa **no** entra en `WalkableLayerMask`; el jugador puede **subir físicamente** por colisión, pero la detección de “suelo” para gravedad/teleport puede no tratarla como walkable interior.

---

## 4. Huecos y riesgos evidentes (sin corregir)

1. **Entrada / `PlayerSpawn`** (`(-3.09, 0.00, -12.94)`):
   - Raycast vertical en **Default**, **Estructura** y **Teleport**: **sin impacto**
   - Grid ±8 m en XZ: **ningún** suelo en capas Default+Estructura (`WalkableLayerMask`) ni Teleport
   - Collider caminable más cercano en XZ: muro/moldura **Estructura** a ~**5.4 m**
   - **Riesgo:** hueco bajo el punto de spawn; al iniciar, `PlayerSpawnBootstrap` no encontrará suelo walkable y dejará Y=0 (caída / levitación según gravedad)

2. **Suelo exterior vs spawn:** `Suelo Zacate` tiene collider en **Teleport**, pero está **lejos** del `PlayerSpawn` (no cubre la zona de entrada probada)

3. **Colliders solo en mallas visuales:** la mayoría de colliders están en los objetos de malla bajo `Estructura` / `Exterior`; no se detectaron triggers sustitutos de geometría sólida

4. **Conteos vs Fase 1:** el informe histórico citaba 158 box + 14 convex; en escena cargada hoy: **170 box + 14 convex** (posible drift por re-auditoría, no verificado en este informe)

---

## Veredictos finales

### walls-block — **PASA**

- Muros (incl. exterior **Estructura**) con colliders sólidos
- Matriz de colisión permite choque jugador ↔ **Estructura** / **Default**
- `CharacterController` con radio 0.3 m

### stairs-climbable — **PASA**

- Rampa con `BoxCollider` sólido, pendiente ~25° (< `slopeLimit` 45°)
- Colisión jugador ↔ capa **Teleport** activa

**Observaciones que no cambian el veredicto pero conviene registrar:** exclusión de **Teleport** en `WalkableLayerMask`; hueco de suelo en **PlayerSpawn** (ítem 4).

---

## Acciones

Ninguna (agente de verificación únicamente). Fallos/huecos listados arriba para otro agente o intervención manual.
