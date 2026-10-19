# Agente 3 — log interacciones

**Última actualización:** 2026-10-18 (continuación)

## Política de lock (actualizada)

- **No** integrar `Assets/Scenes/Vivienda_Prueba.unity` si `Assets/_Project/Docs/.locks/editor.lock` tiene **menos de 30 minutos** sin actividad.
- Autorun (`Assets/_Project/.agente3_pending`) construye **solo sandbox** `Assets/_Project/Scenes/_Sandbox/Interacciones.unity`.
- Integración en casa: menú **Vivienda → Agente 3 → Integrar en Vivienda_Prueba** cuando lock ≥30 min y Agente 3 tome lock.

## Entregables en disco (runtime)

| Componente | Script / asset |
|------------|----------------|
| Puerta | `DoorHinge.cs` → prefab `Puerta_Estandar_PROV` (bootstrap) |
| Ventana abatible | `WindowHinge.cs`, `WindowManager.cs` |
| Interruptor | `LightSwitch.cs`, `RoomLightGroup.cs` |
| Día/noche | `DayNightController.cs` (+ `SetDayNight`, `EnvironmentCycleEvents`) |
| Audio | `ProyectoVrAudioBus.cs`, mixer `ProyectoVR_Main.mixer` |
| Hápticos | `VrHapticsUtility.cs` |
| Habitaciones | `InteractablesRoomCatalog.cs` |

## Bootstrap Editor

- `Assets/_Project/Editor/Agente3/Agente3InteraccionesBootstrap.cs` (asmdef `Vivienda.Agent3.Editor`).
- Unity MCP no disponible; prefabs/escena/mixer se generan al compilar y terminar import (autorun espera `!isUpdating`).
- **No** se cierra Unity ni batchmode sobre proyecto abierto.

## Estado

- Scripts compilados (`Vivienda.Agent3.Editor.dll` OK en última pasada).
- Prefabs interactables: **pendientes** hasta que autorun complete (import largo ~10–20 min).
- Escena principal: **sin tocar** (política lock 30 min).
- Capturas: pendientes post-sandbox en `Assets/_Project/Docs/Reports/captures/`.

## Próximo ciclo

1. Verificar prefabs bajo `Assets/_Project/Prefabs/Interactables/`.
2. Abrir sandbox y validar Grip/Trigger en puerta, ventana, interruptor.
3. Cablear habitaciones del catálogo cuando FBX tenga marcos separados.
