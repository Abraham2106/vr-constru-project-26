# Locomoción grounded (Vivienda_Prueba)

- **Spawn mundo (pies):** `-3.086, -0.05, -12.938`
- **Yaw spawn:** `0.0`°
- **Velocidad caminata:** `1.4 m/s` (sprint Left Shift `2.5 m/s`)
- **CharacterController:** bloquea muros; step `0.4 m`, slope `45°`
- **Simulator:** sin vuelo libre; movimiento por CC + stick simulado
- **Actualizado:** `2026-10-18 12:40`

## Fix caída en spawn (2026-10-18)

- **WalkableLayerMask:** Default (0) + Estructura (6) + Teleport (8) — bits `321`.
- **Suelo collider:** `ExteriorWalkableFloor_PROV` (hijo de `Exterior`), capa **Estructura**, `BoxCollider` sólido `(43.11, 0.20, 36.50)` m, tope alineado con `Suelo Zacate` (~Y `-0.05`).
- **Spawn pies (bootstrap):** `(-3.086, -0.05, -12.938)`, yaw `0°` hacia la fachada.
- Eliminado `SpawnGround_Prov` mal ubicado (Y ~12.5).

## Polish caminata grounded (2026-10-18 12:40)

- **ContinuousMoveProvider (`Move`):** `m_MoveSpeed` serializado **1.4 m/s** en `ProyectoVR_XR_Rig.prefab` (antes 2.5 del sample XRI).
- **Sprint:** `GroundedXrSprintModifier` — **2.5 m/s** solo con **Left Shift**; caminata por defecto 1.4.
- **Verificación spawn:** raycast descendente desde `(-3.086, 1.95, -12.938)` con máscara walkable (321) → **`ExteriorWalkableFloor_PROV`**, Y impacto **≈ -0.05**.
- Escena `Vivienda_Prueba.unity` guardada.

## Fix IsNormalized Play Mode (2026-10-18 13:00)

- **Origen assert:** `DoorHinge.cs` ~112 — `Vector3.SignedAngle` con `toHand.normalized` degenerado; 9 puertas en escena → spam por frame. Locomoción: `ContinuousMoveProvider` / `Quaternion.FromToRotation` sin forward proyectado válido.
- **Guardas:** `DirectionUtil` (umbral `1e-6`); ángulo en XZ sin `SignedAngle`; `GroundedDynamicMoveProvider` sustituye move en runtime; latch solo al cerrar desde >3° (una vez, `PlayOneShot`).

## Fix IsNormalized Play Mode (2026-10-18 13:22)

- **Log consola (13:21:56):** `Assertion failed on expression: 'IsNormalized(dir, 0.0001f)'` sin stack C# (assert nativo).
- **Marco inferido:** `ContinuousMoveProvider.cs` **línea 300** — `Quaternion.FromToRotation(originTransform.forward, inputForwardProjectedInWorldSpace)` cuando la proyección horizontal del forward es cero; persiste si el dominio seguía con `DoorHinge` antiguo (~112 `SignedAngle`).
- **Cambios:** `GroundedDynamicMoveProvider` bloquea `base.ComputeDesiredMove` si `DirectionUtil.TryGetGroundedMoveFrameRotation` falla; upgrade del move provider en `Start`; `enableFly=false`; simulador sin traslación WASD/Q/E (`keyboardBodyTranslateMultiplier=0`); `DoorHinge`/`WindowHinge` no-op en `Update` si la mano no aporta dirección horizontal.
- **WASD:** stick simulado → `DynamicMoveProvider` / `GroundedDynamicMoveProvider` → `CharacterController` a **1.4 m/s** (`ExteriorWalkableFloor_PROV` sin cambios).
