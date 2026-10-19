# Escena visual — exterior (`Vivienda_Prueba`)

**Fecha:** 2026-10-18  
**Alcance:** cielo, sol, material del suelo exterior visible. Sin cambios en locomoción, puertas ni collider `ExteriorWalkableFloor_PROV`.

## Cambios

### Cielo
- Se reemplazó el fondo plano (sin skybox) por **`Vivienda_DaySky`** (`Assets/_Project/Art/Materials/Environment/Vivienda_DaySky.mat`): skybox procedural URP con gradiente azul diurno, horizonte cálido y disco solar suave.
- `RenderSettings`: modo ambiente **Trilight**, colores cielo/ecuador/suelo suaves e intensidad **1.05**; reflexiones **0.65**.
- `RenderSettings.sun` enlazado al sol direccional.

### Sol (y relleno)
- **`Directional Light`**: intensidad **1.35**, color cálido, sombras **Soft** al **72%**, orientación **~(40°, 185°, 0°)** para iluminar la fachada vista desde `PlayerSpawn` (entrada al patio), no de frente en contraluz.
- **`Exterior Fill Light`** (nuevo): luz direccional sin sombras, intensidad **0.22**, tono azulado, para suavizar sombras duras (Quest-friendly: solo 2 luces en tiempo real).
- Lightmaps horneados **desactivados** en la escena; sol en modo **Realtime** (`m_Lightmapping: 0`).

### Plano amarillo (suelo exterior)
- El plano grande visible era **`Suelo Zacate [693790]`** con `Grass 01` (se veía amarillo/neón en escena).
- Material visual sustituido por **`ExteriorGround_Day`** (`Assets/_Project/Art/Materials/Environment/ExteriorGround_Day.mat`): verde césped/tierra apagado, textura de hierba con tinte `(0.34, 0.46, 0.28)`, bajo brillo.
- **`ExteriorWalkableFloor_PROV`**: sin renderer; collider intacto.

## Captura
- `Assets/_Project/Docs/Reports/captures/exterior_mejorado.png` — cámara desde posición de spawn (~1.65 m), mirando al patio/fachada.

## Notas
- Coste Quest: 1 sol con sombras + 1 fill sin sombras; sin lightmaps ni luces extra.
- Si Unity tenía la escena abierta, recargar o guardar de nuevo para fusionar cambios en disco con el estado del editor.
