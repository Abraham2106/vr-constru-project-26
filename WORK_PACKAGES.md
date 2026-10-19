# Vivienda: paquetes implementados

Escena de trabajo e inicio del build: `Assets/Scenes/Vivienda_Prueba.unity`.

| Paquete | Implementación | Comprobación |
|---|---|---|
| Navegación | Inicio exterior frente al acceso, altura de ojos 1,65 m, cuerpo con colisión, escalera y teletransporte a ambos apartamentos | CharacterController recorre las 12 habitaciones con puertas abiertas y cristales cerrados |
| Interacción | 11 puertas con hojas móviles y marcos estáticos; ventanas reales, manijas, audio y cambio de ambiente exterior | Selección XR, apertura/cierre y permanencia de marcos |
| Menú y herramientas | Menú inicial, destinos, día/noche, medición A/B en metros, paleta de 12 colores y restauración de paredes | Clic UGUI, medición de 1 m, selección de paleta y teletransporte |
| Iluminación | 12 grupos independientes con interruptores y plafones; dos luces de acceso nocturnas | Activación de cada interruptor y transición día/noche |

## Simulador de escritorio

Pulsa Play. Haz clic en el menú; **Continuar recorrido** lo cierra.

- **WASD**: caminar. **Shift**: caminar más rápido.
- **Mantener botón derecho del ratón**: mirar.
- **E**: abrir/cerrar la puerta o ventana, o accionar el interruptor al que miras, a menos de 3 m.
- **M / Escape**: abrir/cerrar menú.
- **Espacio**: marcar puntos de medición o pintar la pared apuntada, después de elegir la herramienta.
- **Ctrl izquierdo**: deshacer medida o cambiar color de pintura.

## Controles XR preparados

Stick izquierdo para caminar; Y/Menu abre el panel; gatillo para botones, interruptores y herramientas. Grip sobre una hoja permite arrastrar cerca de la manija o alternar apertura a distancia. La comprobación actual se realiza en Unity; queda pendiente probar con un visor físico.

Los informes reproducibles se generan desde **Vivienda > Journey > Comprobar rutas** y **Comprobar experiencia** y se escriben en `Temp/JourneyNavigation.txt` y `Temp/JourneyAcceptance.txt`. La carpeta `Validation` conserva los resultados de esta entrega.

La escena anterior `Assets/Scenes/Vivienda.unity` se conserva; el trabajo actual está en `Vivienda_Prueba.unity`.
