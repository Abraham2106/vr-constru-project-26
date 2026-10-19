# Verificación — locomoción a pie (caminar)

**Proyecto:** `ViviendaUnity`  
**Escena:** `Assets/Scenes/Vivienda_Prueba.unity`  
**Fecha (local):** 2026-10-18 ~12:35  
**Agente:** solo verificación (sin cambios en escena/código)

## Condiciones de la revisión

| Comprobación | Resultado |
|--------------|-----------|
| `Assets/_Project/Docs/.locks/editor.lock` | **No existe** → permitido inspeccionar escena abierta |
| Unity Importing/Compiling | **No** (`isCompiling=False` en MCP) |
| Play Mode | **No entrado** (validación en editor + scripts; prueba runtime de WASD/caída pendiente de aprobación Play Mode) |
| Guardado de escena | **No guardado** por este agente |

## 1. Spawn / XR Origin

### Posiciones en mundo (Inspector vía Unity MCP, escena en editor)

| Objeto | Posición mundo (m) |
|--------|-------------------|
| `PlayerSpawn` | **(-3.09, 0.00, -12.94)** |
| `ProyectoVR_XR_Rig` (prefab instancia) | **(-3.09, 0.00, -12.94)** |
| `XROrigin` → `Origin` (`ProyectoVR_XR_Rig`) | **(-3.09, 0.00, -12.94)** |

La instancia del rig en escena coincide con el marcador `PlayerSpawn` (misma XZ que en el YAML de escena: `x: -3.085558`, `z: -12.937635`).

### ¿Hay suelo caminable bajo el spawn?

Raycasts desde `PlayerSpawn` (origen Y+40 m, distancia 120 m):

| Máscara | Resultado |
|---------|-----------|
| `WalkableLayerMask` (Default + Estructura, bits 0 y 6 = **65**) | **SIN IMPACTO** |
| Solo Default / solo Estructura | **SIN IMPACTO** |
| Todas las capas excepto Ignore Raycast | **SIN IMPACTO** |
| Todas las capas | Impacto en **CharacterController del propio rig** (~Y **1.53**), capa *Ignore Raycast* — no es suelo de la vivienda |

Barrido en rejilla ±20 m alrededor del spawn: los primeros impactos caminables aparecen hacia la casa (ΔZ positivo) en **muros/techo (~Y 3–7 m)**, no en un piso exterior a Y≈0.

**Conclusión spawn:** el punto está colocado **delante de la casa en planta** (coherente con intención de fachada/patio), pero **no hay collider caminable bajo esa XZ**. En runtime, `PlayerSpawnBootstrap.ResolveGroundPosition` también fallaría el raycast walkable y dejaría Y=0 → con gravedad el jugador **caería al vacío**, no quedaría apoyado en suelo.

## 2. CharacterController y velocidad de caminata

Componentes en **`ProyectoVR_XR_Rig`** (valores leídos en editor):

### `CharacterController` (en GameObject Origin del rig)

| Campo | Valor |
|-------|-------|
| enabled | **true** |
| height | **1.7** |
| radius | **0.3** |
| skinWidth | **0.02** |
| stepOffset | **0.4** |
| slopeLimit | **45** |
| center | **(0, 0.85, 0)** |
| isGrounded (editor, sin Play) | **false** |

### `ContinuousMoveProvider` (GameObject **`Move`**)

| Campo | Valor en editor |
|-------|-----------------|
| moveSpeed | **2.5** |
| enableFly | **false** |

Nota: **2.5 m/s** coincide con la constante de sprint en `GroundedXrSprintModifier`; la caminata esperada **1.4 m/s** se aplica en **`Update()`** en Play Mode (`WalkSpeed = 1.4f`). En Awake, `GroundedXrLocomotionProfile` también fuerza `moveSpeed = 1.4` y `enableFly = false`.

### Scripts de locomoción presentes y activos

| Componente | GameObject | enabled |
|------------|------------|---------|
| `ProyectoVR.Core.PlayerSpawnBootstrap` | ProyectoVR_XR_Rig | true |
| `ProyectoVR.Core.GroundedXrLocomotionProfile` | ProyectoVR_XR_Rig | true |
| `ProyectoVR.Core.GroundedXrSprintModifier` | ProyectoVR_XR_Rig | true |
| `ProyectoVR.Core.XrDeviceSimulatorGroundedEnforcer` | **XrLocomotionServices** (raíz escena) | true |

`PlayerSpawnBootstrap`: referencias serializadas `m_Origin` / `m_PlayerSpawn` en **0** → resolución en runtime vía `GetComponent` / `GameObject.Find("PlayerSpawn")`.

### ¿WASD mueve cámara voladora (simulador)?

**`XR Device Simulator`** (objeto `XR Device Simulator`):

| Campo | Valor |
|-------|-------|
| keyboardXTranslateSpeed | **0** |
| keyboardYTranslateSpeed | **0** |
| keyboardZTranslateSpeed | **0** |
| mouseTransformationMode | **Rotate** |

Configuración **anti-vuelo libre** coherente con `GroundedXrLocomotionProfile` / `XrDeviceSimulatorGroundedEnforcer`. **No se probó input en Play Mode** en esta pasada.

## 3. Gravedad, escalones y pendiente

### `GravityProvider` (GameObject **`Gravity`**)

| Campo | Valor |
|-------|-------|
| useGravity | **true** |
| sphereCastLayerMask | **51** (coincide con máscara walkable **65** en bits activos para detección de suelo del provider) |

Escalones/pendiente vía **CharacterController**: **stepOffset 0.4 m**, **slopeLimit 45°** — cumplen criterio (~0.4 m / ~45°) **si** hay geometría walkable bajo el personaje.

### `XROrigin`

| Campo | Valor en editor |
|-------|-----------------|
| CameraYOffset | **1.36144** (perfil objetivo en código: **1.65** en Awake de `GroundedXrLocomotionProfile`) |

## 4. Evidencia visual

| Archivo | Estado |
|---------|--------|
| `Assets/_Project/Docs/Reports/captures/verif_spawn.png` | **No creado** — `Unity_Camera_Capture` falló; `ScreenCapture` requiere Play Mode (no usado). Vista multiangulo MCP en sesión confirma rig/patio amarillo; encuadre dedicado de `PlayerSpawn` pendiente. |

## Veredicto

| Criterio | Resultado |
|----------|-----------|
| Spawn sobre suelo caminable (collider walkable) | **FALLA** |
| Caminata anclada (no vuelo libre WASD en simulador) — configuración | **PASA** (inspector; sin Play Mode) |
| Velocidad caminata ~1.4 m/s | **PENDIENTE runtime** (serializado 2.5 hasta `Update`/Awake; diseño en código = 1.4) |
| Gravedad + step/slope para escaleras | **PASA** (valores CC + `useGravity=true`; falta suelo bajo spawn) |

### Veredicto global: **FALLA**

Motivo principal: **no hay collider en capas caminables bajo `PlayerSpawn` / rig**, por lo que el spawn no queda apoyado en el suelo de la vivienda.

## Qué falta (para agente de locomoción / escena — no corregido aquí)

1. **Collider de suelo exterior/patio** (capa Default o Estructura) bajo **(-3.09, 0, -12.94)** o **mover `PlayerSpawn`** a XZ con raycast walkable exitoso.
2. **Prueba en Play Mode**: confirmar `PlayerSpawnBootstrap.LastSpawnWorldPosition`, `CharacterController.isGrounded`, `moveSpeed == 1.4` al caminar, y que WASD no traslada el rig en el simulador.
3. **Captura** `verif_spawn.png` con Scene View encuadrado en `PlayerSpawn` tras suelo válido.
4. Opcional: alinear **CameraYOffset** persistido con **1.65** si se desea consistencia editor/runtime.

---

### Log MCP (extracto) — inspección principal

```
activeScene=Vivienda_Prueba
PlayerSpawn.world=(-3.09, 0.00, -12.94)
Walkable ray => NO_HIT
CharacterController: height=1.7 stepOffset=0.4 slopeLimit=45
ContinuousMoveProvider Move: moveSpeed=2.5 enableFly=False
GravityProvider Gravity: useGravity=True layerMask=51
XRDeviceSimulator: kbdXYZ=0,0,0 mouseMode=Rotate
```
