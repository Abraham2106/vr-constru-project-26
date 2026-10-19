# Verificación de interacción — puertas y ventanas

**Escena:** `Assets/Scenes/Vivienda_Prueba.unity`  
**Fecha:** 2026-10-18  
**Agente:** verificación (solo lectura; sin Play, sin guardar escena, sin modificar objetos)  
**Unity MCP:** auditoría en editor (`Unity_RunCommand`)  
**Bloqueo editor:** `Assets/_Project/Docs/.locks/editor.lock` **no presente** (sin restricción de 30 min).

---

## Veredicto: **FALLA**

La escena contiene mallas de puertas y placeholders de puerta/ventana, pero **ningún** `DoorHinge`, **ningún** `WindowHinge` y **ningún** `XRGrabInteractable` en esos placeholders. No hay interacción funcional cableada en el estado actual.

---

## 1. Puertas: bisagra / agarre vs mallas

| Métrica | Cantidad |
|--------|----------|
| Mallas `door-trustile` en escena | **9** |
| Objetos con `DoorHinge` | **0** |
| Objetos con `XRGrabInteractable` (manija) | **0** |
| Placeholders `Puerta_Auto_*` con componentes de interacción | **0 / 9** |

**Relación esperada tras cableado (editor `EscenaInteraccionesWire`):** cada `Puerta_Auto_XX` recibe `DoorHinge` + `XRGrabInteractable` en `Manija_PROV`.

**Mallas de puerta aún estáticas (sin bisagra ni grab):**

1. `door-trustile_doors-tmir6000 30" x 80" [521723].001`
2. `door-trustile_doors-tmir6000 30" x 80" [666085].001`
3. `door-trustile_doors-tmir6000 32" x 84" [521548].001`
4. `door-trustile_doors-tmir6000 32" x 84" [666355].001`
5. `door-trustile_doors-tmir6000 32" x 84" [666523].001`
6. `door-trustile_doors-tmir6000 32" x 84" [695748].001`
7. `door-trustile_doors-tmir6000 32" x 84" [695850].001`
8. `door-trustile_doors-tmir6000 32" x 84" [696442].001`
9. `door-trustile_doors-tmir6000 32" x 84" 2 [521856].001`

**Raíces placeholder sin cablear (solo contenedor, sin `DoorHinge`):**

- `Puerta_Auto_01_PLACEHOLDER` … `Puerta_Auto_09_PLACEHOLDER` (9)

---

## 2. Ventanas interactivas

| Métrica | Cantidad |
|--------|----------|
| Placeholders `Ventana_Auto_*` | **2** |
| Objetos con `WindowHinge` | **0** |
| Ventanas interactivas | **0 / 2** |

**Ventanas / contenedores aún estáticos:**

- `Ventana_Auto_01_PLACEHOLDER`
- `Ventana_Auto_02_PLACEHOLDER`
- `Ventanas_PLACEHOLDER` (agrupador; sin `WindowHinge`)

---

## 3. Entrada XR / XR Device Simulator (según componentes cableados)

### Estado en escena

- **No hay** `XRGrabInteractable` en manijas de puerta/ventana → **ninguna tecla o botón del simulador activará puertas/ventanas hoy.**

### Cadena definida por código y stack XR ya presente

| Capa | Configuración observada |
|------|-------------------------|
| `DoorHinge` / `WindowHinge` | Escuchan `selectEntered` / `selectExited` del campo `handleGrab` (`XRGrabInteractable`). |
| Interactores en escena | `Near-Far Interactor` (izq/der) con **Select** = `XRI Left Interaction/Select` y `XRI Right Interaction/Select` (asset `XRI Default Input Actions.inputactions`). |
| Acción **Select** (XRI) | Enlazada a `<XRController>{LeftHand\|RightHand}/{GripButton}` (no a Trigger). |
| **XR Device Simulator** | El control simulado **Grip** del mapa `XR Device Controller Controls` → tecla **`G`** y botón **Grip** en el panel del simulador; alimenta el `GripButton` del controlador XR virtual que dispara **Select**. |

**Respuesta directa para QA:** cuando exista `XRGrabInteractable` en la manija, el usuario debe usar **Select = Grip** (botón **Grip** del *XR Device Simulator* o tecla **`G`**), o selección por rayo Near-Far con la misma acción **Select**.  
**Hoy:** entrada **no aplicable** — grabs **no cableados**.

*(Nota: `DoorHinge.cs` documenta “Grip” en comentario; el binding real en proyecto es la acción **Select** de XRI → `{GripButton}`.)*

---

## 4. Audio de pestillo (`sfx_latch.wav`)

| Comprobación | Resultado |
|--------------|-----------|
| Asset en proyecto | **Sí** — `Assets/_Project/Audio/Generated/sfx_latch.wav` |
| Asignado en algún `DoorHinge.latchClip` en escena | **No** — no hay instancias de `DoorHinge` |

Tras cableado, el pipeline de editor asigna este clip al campo `latchClip` de cada puerta.

---

## Resumen ejecutivo

| Criterio | Esperado (objetivo) | Actual | OK |
|----------|---------------------|--------|-----|
| Puertas interactivas | 9 | 0 | No |
| Ventanas interactivas | 2 | 0 | No |
| Grab + bisagra en escena | Sí | No | No |
| Clip de pestillo en instancias | Asignado | N/A (sin puertas cableadas) | No |
| Entrada Grip/Select documentada | Según XRI | Definida en interactores; **sin target grab** | No |

**Acción sugerida (fuera de alcance de este agente):** ejecutar cableado (`Vivienda/Agente3/Cablear puertas y ventanas en escena` o agente de interacción) y repetir esta verificación.
