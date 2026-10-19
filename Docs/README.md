# ViviendaUnity — README

Proyecto VR (OpenXR + XR Interaction Toolkit 3.6.1) para recorrer la vivienda en Meta Quest vía SideQuest (APK Android).

## Requisitos

- Unity **6000.5.9f1**
- Escena principal: `Assets/Scenes/Vivienda_Prueba.unity`
- Modelo: `Assets/Vivienda/Vivienda.fbx`

## Play Mode con XR Device Simulator

1. Abrir `Vivienda_Prueba.unity`.
2. Asegurar **XR Device Simulator** en escena (sample XRI 3.6.1).
3. Pulsar **Play**.
4. Controles simulador:
   - **Ratón derecho** — rotar vista (look).
   - **WASD** — mover cuerpo / rig.
   - Panel simulador — botones gatillo/menú según sample.

### Herramientas (Agente 4)

Tras instalar el host: menú **Vivienda → Agente 4 → Instalar ToolHost en escena activa**.

| Herramienta | Menú (hooks) | Uso |
|-------------|--------------|-----|
| Medir | `ViviendaMenuHooks.SelectMeasureTool()` | Gatillo: punto A → punto B; línea + texto `X.XX m`. Secundario: deshacer última. |
| Pintar muros | `ViviendaMenuHooks.SelectWallPaintTool()` | Gatillo en muro con `PaintableWall`; secundario: siguiente color; restore vía hook. |

Teclado en editor (fallback): **Espacio** = gatillo, **Ctrl izq** = secundario.

## SideQuest / APK

1. Build (cuando Android SDK esté OK): **Vivienda → Agente 4 → Build Android APK (Quest)** → salida `Builds/ViviendaUnity.apk`.
2. SideQuest: conectar Quest (USB / wireless debugging), **Install APK** → seleccionar `Builds/ViviendaUnity.apk`.
3. En el visor: biblioteca → Fuentes desconocidas → ViviendaUnity.

**Bloqueo actual:** ver `Assets/_Project/Docs/Reports/fase4_qa.md` si falta módulo Android.

## Placeholders pendientes (otros agentes)

- Puertas / ventanas: placeholders `*_PLACEHOLDER` hasta re-export Blender.
- Menú XR radial: wiring a `ViviendaMenuHooks`.
- Prefabs luz / puerta interactiva.

## Documentación viva

- Checklist: `Assets/_Project/Docs/CHECKLIST.md`
- Plan: `Docs/PLAN.md`
- Decisiones: `Assets/_Project/Docs/DECISIONES.md` (append)
