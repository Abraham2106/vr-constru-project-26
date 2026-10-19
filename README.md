# Visualización arquitectónica en realidad virtual

Proyecto desarrollado en Unity para explorar una vivienda de dos niveles mediante un recorrido interactivo en realidad virtual. Permite comprender la distribución de los espacios, observar los acabados y evaluar distintas condiciones de iluminación a escala real.

## Funcionalidades

- Recorrido por ambos niveles y teletransporte entre zonas.
- Apertura y cierre de puertas y ventanas.
- Control de luces por habitación y modos de día y noche.
- Medición de distancias y selección de colores para las paredes.
- Menú interactivo y vista de escritorio para pruebas.

## Tecnologías

Unity 6.5 (`6000.5.9f1`), Universal Render Pipeline, OpenXR y XR Interaction Toolkit. Plataforma objetivo: Meta Quest.

La navegación utiliza los componentes nativos de XRI: Locomotion Mediator, XR Body Transformer y proveedores de movimiento, gravedad, giro y teletransporte. Los controles se configuran en `Assets/_Project/Input/Vivienda.inputactions`.

## Inicio

1. Abrir el proyecto con la versión indicada de Unity.
2. Abrir `Assets/Scenes/Vivienda_Prueba.unity`.
3. Pulsar **Play** y seleccionar **Continuar recorrido**.

En escritorio: **WASD** para caminar, **botón derecho mantenido** para mirar, **E** para interactuar, **M** para abrir el menú y **Espacio** para utilizar la herramienta seleccionada.

Para probar con mandos simulados, seleccionar **Vivienda → XR → Modo simulador XR** antes de Play. **WASD** camina mediante locomoción nativa y **Esc** abre o cierra el menú de la vivienda. **Shift + IJKL** simula el joystick izquierdo; **G/T** simulan agarre/gatillo del mando derecho. **Y** despliega el selector de dispositivos y **X** muestra las acciones fuera del modo FPS. Para volver al recorrido con teclado y ratón, seleccionar **Modo escritorio**. El simulador oficial se excluye de las builds.

Para probar un visor conectado al PC, seleccionar **Modo visor conectado**. La versión Android inicia XR automáticamente.

La configuración Android utiliza ARM64, IL2CPP y OpenXR Single Pass Instanced. Las comprobaciones del proyecto están disponibles en **Vivienda → XR** y guardan sus resultados en `Validation/`.

La validación con un visor Meta Quest está pendiente.

Consultar el [plan de calidad visual](Docs/CalidadVisual-VR.md) y su [versión PDF](Docs/CalidadVisual-VR.pdf), con referencias profesionales y mejoras propuestas.

## Licencias

Consultar [licencias de audio](Docs/Licenses.md).
