# Regresiones detectadas (Agente 4)

## 2026-10-18 — CS0246 VrHapticsUtility / BaseInteractable

| Campo | Detalle |
|-------|---------|
| Síntoma | `error CS0246: BaseInteractable` bloqueaba compilación de todo `ProyectoVR.Runtime` |
| Causa | XRI 3.6 renombró tipo a `XRBaseInteractable` (namespace `Interactables`) |
| Fix | `Assets/_Project/Scripts/Interaction/VrHapticsUtility.cs` — using + firma actualizada |
| Verificación | pendiente: 0 `error CS` en `Logs/Editor.log` tras recompile |
