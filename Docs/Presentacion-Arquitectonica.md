# Presentación arquitectónica y recorrido VR

La escena principal incorpora mobiliario de referencia, biseles pequeños, materiales por cara recuperados de Blender e iluminación indirecta diurna y nocturna. Se conservan la arquitectura y la locomoción nativa de XRI.

## Fuente y apariencia

La entrega local incluye `outputs/Blender/Vivienda_Presentacion.blend`, `Fachada_Cycles.png` e `Interior_Cycles.png`. El original `Downloads/Untitled.blend` se conserva. Los renders usan Cycles, AgX, cielo físico, sol y luminarias de sala; no son capturas del visor.

En Blender se corrigieron los 28 objetos sin material, el vidrio de transmisión, los cantos de construcción y las normales del mobiliario. El césped es un dieléctrico rugoso, con el color del bake original atenuado para evitar valores de albedo excesivos. El mobiliario es una propuesta de ambientación que debe validarse con el diseño de interiores.

En Unity se restauraron las particiones de material de doce muros: cada grupo de triángulos conserva la madera, el revestimiento claro o la cerámica asignados en Blender. Las UV de albedo se conservan; el editor genera un canal independiente para lightmaps. Los datos comprimidos de `Assets/_Project/Art/Presentation/Source/` permiten repetir la preparación sin rutas externas de trabajo.

## Iluminación e interacción

- Progressive CPU Lightmapper, lightmaps direccionales, 14 texels/m y cuatro rebotes.
- Atlas de día y noche, índices y transformaciones UV por renderer y coeficientes de Light Probes separados por escenario.
- Sol Mixed / Baked Indirect: luz directa y sombras en tiempo real; rebotes preparados antes de ejecutar.
- Interruptores de habitación en tiempo real. Sus rebotes no se recalculan al encenderlos.
- Reflection Probes locales de día y noche, actualizados con los nuevos acabados y mobiliario.

`BakedDayNightLighting` escucha los eventos ambientales y aplica los datos mediante `LightmapSettings`. El atlas cambia al terminar la transición ambiental; la iluminación indirecta no se mezcla continuamente entre escenarios.

La pintura interactiva cambia el acabado visible, mientras que los rebotes conservan el color usado en el bake. Las puertas y ventanas móviles conservan sus interacciones y reciben luz de sondas; el bake no recalcula la oclusión indirecta al abrirlas.

## Mantenimiento

### Corrección de acabados del mobiliario

La revisión de cocina identificó el mismo uso de madera de contraste excesivo y escala variable en estantes, molduras interiores, mesas, cómodas y bases de cama. Los tapizados, alfombras y ropa de cama también carecían de mapas PBR de tejido en Unity. La encimera tenía un material plano.

Se sustituyeron esos acabados en 62 piezas por cinco materiales nativos URP/Lit: chapa de nogal, lino crema, microcemento, alfombra y ropa de cama. Ocho frentes existentes tienen juntas de 3 mm. Los contornos del mobiliario permanecen dentro de su volumen anterior.

El nuevo mapeado se calcula sobre la geometría final, incluidos los biseles. La primera auditoría detectó estiramiento en las UV interpoladas de bordes de cojines, alfombras y mesas; se reemplazó por proyección métrica por cara final, conservando las normales de sombreado. Las escalas de repetición son 1,8 m en nogal, 0,25 m en textiles y 0,5 m en microcemento. Los desfases varían por pieza para evitar copias idénticas de la misma veta.

Los mapas usan albedo sRGB, normales y smoothness lineales, mipmaps, filtro trilineal y anisotropía 4. La madera es dieléctrica, con relieve fino; el tejido es mate. Los datos comprimidos y mapas viven en `Assets/_Project/Art/Presentation/Finishes/`, junto con [procedencia y licencia](../Assets/_Project/Art/Presentation/Finishes/ATTRIBUTION.md).

Se corrigió además el acabado pulido de dos cielos rasos, que producía reflejos excesivos bajo las luminarias: mantienen su albedo y UV, con smoothness 0,08 y relieve reducido. Los otros dos cielos rasos ya usaban blanco mate.

**Corregir acabados del mobiliario** reaplica la geometría y los materiales desde esos datos. **Auditar acabados del mobiliario** comprueba los 62 objetos, la densidad UV de las superficies visibles, UV2 independiente, mapas/canales e importadores. Guarda `Validation/Furniture-Finish-Audit.txt`; el registro de aplicación está en `Validation/Furniture-Finishes.txt`. La preparación general reaplica esta corrección para que una reconstrucción no recupere los acabados anteriores. Después hay que hornear GI y reflejos.

Las capturas `Captures/Muebles_Dia_Cocina.png` y `Muebles_Noche_Cocina.png` muestran la cocina en Unity; `Presentacion_Dia_Interior.png` permite revisar el sofá y las mesas. La auditoría técnica no sustituye su inspección visual ni garantiza rendimiento en el visor.

1. Salir de Play y guardar la escena principal.
2. Para reconstruir las mallas y sus UV desde la entrega, usar **Vivienda → Graficos → Preparar presentacion arquitectonica**.
3. Usar **Completar vegetacion y materiales** para configurar la vegetación PBR de 1K.
4. Usar **Hornear iluminacion arquitectonica dia y noche** y esperar a que terminen ambos escenarios.
5. Usar **Hornear reflejos de dia y noche** después de cambios visuales relevantes.
6. Ejecutar **Comprobar presentacion en Play** y revisar `Validation/Presentation-Runtime.txt` y `Captures/Presentacion_*.png`.

Los atlas no se actualizan automáticamente al cambiar geometría o materiales. `Validation/Architectural-GI.txt` registra los atlas y las sondas calculados. La prueba en Play comprueba el cambio día/noche y el retorno al día, varias particiones de materiales y que los destinos interiores de teletransporte estén libres de los nuevos colliders.

Estas comprobaciones no certifican fotorealismo ni rendimiento en Quest. Antes de una entrega al cliente hay que recorrer la vivienda en el visor objetivo, medir CPU/GPU/memoria y acordar la ambientación. Cycles sirve como referencia; URP en un visor autónomo tiene un presupuesto distinto.

## Recursos y documentación

- [Potted Plant 02, Poly Haven](https://polyhaven.com/a/potted_plant_02), Rico Cilliers, [CC0](https://polyhaven.com/license). Procedencia en `Assets/_Project/Art/Presentation/Vegetation/ATTRIBUTION.md`.
- [Unity: Lighting window](https://docs.unity.com/en-us/engine/6000.0/manual/lighting-overview/lighting-reference/lighting-window).
- [Unity: LightmapSettings](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/LightmapSettings.html).
- [Blender: Cycles light paths](https://docs.blender.org/manual/en/4.4/render/cycles/render_settings/light_paths.html).

`CalidadVisual-VR.pdf` documenta el diagnóstico y la primera mejora anterior a esta implementación. Este documento describe el estado posterior con iluminación indirecta y ambientación.


## Acabados de fachada

Las UV de madera de 14 objetos se trasladan desde `FacadeFinishes.json.gz`, exportado de Blender, sin cambiar la geometría ni las UV de iluminación. El menú **Trasladar acabados de fachada desde Blender** vuelve a aplicarlas; la preparación completa también conserva este paso. La textura mantiene su color marrón y el mapa normal usa intensidad 0,3.

El vidrio emplea URP Lit transparente en flujo especular, con rugosidad equivalente 0,015 y un acabado reflectante. Dos sondas exteriores de 256 píxeles por cara capturan el entorno de día y noche, además de las sondas interiores. Las exteriores usan proyección de entorno sin caja porque su punto de captura está delante del edificio. Es una aproximación mediante cubemaps horneados; no calcula reflejos planos ni refracción de Cycles.


## Vidrio espejado de referencia

El menu **Aplicar vidrio espejado de referencia** reproduce el acabado exterior verde/dorado de la referencia de Blender mediante URP Lit y el mismo entorno `forest.exr`. Los 22 paneles conservan su geometria y colliders: las caras exteriores usan material espejado y las interiores material transparente. Dos sondas Custom pequenas con anclajes explicitos limitan este entorno a los vidrios; el resto de la arquitectura conserva sus sondas originales. El controlador cambia esas sondas a las capturas nocturnas al pasar a noche.

Este acabado refleja un entorno de referencia fijo, no todos los objetos actuales de la escena. No utiliza ray tracing ni un shader personalizado. `Validation/Mirror-Glazing.txt` registra la aplicacion; `Captures/Vidrio_Interior_Dia.png` permite comprobar la vista hacia fuera. Procedencia del entorno en `Assets/_Project/Art/Presentation/Reflections/ATTRIBUTION.md`.
