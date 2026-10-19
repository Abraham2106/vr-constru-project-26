# Decisiones — Agente 3 (Interacciones)

| Tema | Decisión |
|------|----------|
| Puerta estándar | 0.9 × 2.1 m, bisagra 0–110°, cierre magnético ~3° |
| Ventana | **Abatible** (bisagra lateral); corrediza descartada por mallas fusionadas en FBX |
| Interruptor | Select XR (Trigger en Quest); palanca ±18° |
| Luces habitación | Máx. 1 luz realtime por prefab demo; resto baked/off |
| Día/noche | Blend 4.5 s (3–6), sol + ambiente + skybox + luces exteriores; `SetDayNight` + `EnvironmentCycleEvents` |
| Audio | Mixer SFX / Ambiente / UI; parámetro `ExtAmbience` con ventanas abiertas |

Actualizado: 2026-10-18 (Agente 3, pre-bootstrap assets)
