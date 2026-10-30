# Integracion del Blender nuevo

Estado: escena de prueba en revision; no promovida a la escena principal.
Base: `4f4ba04`, etiqueta `checkpoint-base-2026-10-29`.
Rama: `integracion/blender-vivienda`.
Escena: `Assets/Scenes/Vivienda_Integracion_Blender.unity`.

## Criterio de integracion

Blender define acabados y asignaciones por cara. Unity conserva la experiencia XR, puertas, interruptores, colisiones y mobiliario incorporado. No usar los materiales antiguos como sustitutos de los nuevos. La columna derecha comparte la piedra del muro izquierdo. El cielo negro del balcon se conserva.

Los modelos en `Assets/_Project/Art/BlenderNuevo/Models` se exportan en coordenadas compatibles con la casa existente, con desplazamiento Blender `(4.4492, 0.1941, -2.0265)` antes de convertir ejes a Unity. Los nombres de grupos Piso1/Piso2 son una clasificacion inicial por origen del objeto, no una garantia de altura de todos sus vertices.

Las piezas que cambian asignaciones por cara usan la malla del FBX nuevo. Los elementos obsoletos permanecen ocultos para permitir comparacion. Los componentes interactivos se conservan; en puertas se trasladan materiales a las hojas y marcos existentes. Los muebles descargados, vegetacion, vecindario y rig permanecen en sus raices originales. El inventario completo de los 253 objetos, con una decision para cada uno, esta en `Validation/BlenderIntegrationPieceMap.json`; las asignaciones efectivas de la escena estan en `Validation/BlenderIntegrationBindings.tsv`.

Se incorporaron 131 mallas de arquitectura y acabados. Otras 11 piezas interactivas conservan su geometria y reciben el acabado nuevo; sus marcos y paneles separados suman 22 renderers dinamicos. Se conservan vidrios transparentes, marcos negros, cubiertas/techo negro, barandas, rig y mobiliario aprobados. Los dos paneles nuevos de ventana ya estan cubiertos por el vidrio existente: se incorporaron sus marcos, sin duplicar cristales. La puerta nueva coincide con una puerta interactiva existente y no se duplica.

Los 16 componentes de armarios nuevos se excluyeron tras comprobar interferencias con el cabecero y el acceso/giro de una puerta. No se redisenaron dormitorios para hacerlos caber. Las cuatro superficies pequenas de jardineras usan un atlas de color 1K horneado desde `Grass 01 casa`, sin luz incrustada; suman 64 triangulos. El pasto del entorno se conserva.

La pieza `Suelo Cielo Raso [696129].001` no contiene material en el archivo nuevo. El override de lavanderia aplica el porcelanato adyacente solo a sus caras superiores, con continuidad UV; conserva blanco el cielo inferior y no cambia su collider.

## Reexportar

Usar una copia del Blender del cliente. No guardar sobre el original. Desde la raiz del proyecto, ejecutar con Blender 5.2:

```powershell
$blender = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'
$source = 'RUTA_ABSOLUTA/A_LA_COPIA.blend'
$out = (Resolve-Path 'Assets/_Project/Art/BlenderNuevo').Path
& $blender -b $source --python Tools/Blender/export_assets.py -- --output $out
& $blender -b $source --python Tools/Blender/inspect_materials.py -- --output $out
& $blender -b $source --python Tools/Blender/bake_wood.py -- --output $out
& $blender -b $source --python Tools/Blender/bake_planters.py -- --output $out
```

Exportar no sustituye automaticamente las piezas de la escena. Revisar nombres, UV, transformaciones, limites, materiales por cara y colisiones antes de integrar cambios de geometria. Mantener los .meta existentes. Al cambiar un acabado compartido, usar exclusivamente materiales de esta carpeta para proteger las otras casas.

`materiales_grafo.json` identifica shaders que requieren conversion; `materiales_preparados.json` registra 38 combinaciones reutilizables para 44 nombres de Blender. Las texturas de rugosidad se convierten a smoothness en el alpha del mapa metallic. Los mapas de color usan sRGB; normales y datos son lineales. Los importadores Android usan ASTC 6x6: albedo hasta 2K, normales y datos hasta 1K. Los mapas de iluminacion conservan los tipos Lightmap y DirectionalLightmap y el espacio lineal de las direcciones; no se importan como albedo. Las molduras procedurales tienen bake de color 1K y UV propias.

## Iluminacion

Se han recalculado dia y atardecer con Progressive CPU, 14 texeles/m, 4 rebotes, 64 muestras directas y 256 indirectas/ambiente. El relleno direccional sin sombras se desactiva. El sol diurno usa elevacion de 30 grados y sombras suaves para que la luz entre por los ventanales bajo los aleros. Las 12 luces de habitaciones usan focos de 120 grados orientados abajo, intensidad 1.4, sombras suaves y siguen siendo Realtime para los interruptores.

Menus nativos: `Vivienda > Integracion Blender > Hornear dia y atardecer` y `Actualizar reflejos dia y atardecer`. Actuan solo sobre la escena de prueba. Los resultados se guardan en `BlenderNuevo/Lighting`; no reemplazan los mapas de la escena base. Los informes se escriben en `Validation/BlenderIntegrationBake.report.txt` y `Validation/BlenderIntegrationReflections.txt`.

El metal negro procedural se adapta con los colores y la rugosidad del original en URP Lit. No reproduce exactamente su microrelieve/coat procedural. Es una diferencia de fidelidad documentada para la revision visual; no se presenta como una traduccion exacta de todos los nodos Blender.

## Comprobaciones realizadas

| Revision | Evidencia y alcance |
|---|---|
| Acabados y encuadres | Capturas antes/despues de sala, cocina, dos dormitorios, bano y lavanderia de cada piso; fachada, ventana y trasera, de dia y atardecer. Seis renders de Blender con las mismas posiciones/FOV. Blender usa luces temporales porque la fuente no trae iluminacion; sirven para comparar acabados/geometria, no para exigir identidad entre renderizadores. Ver [revision visual](IntegracionBlender_Revision.md). |
| Mallas/materiales | 321 renderers visibles de vivienda, 22.779 triangulos, 39 materiales y 62 texturas referenciadas. 427 GUID de la escena resueltos, ninguno ausente. Informes `BlenderIntegrationAssetsAudit.txt` y `BlenderIntegrationDependencies.txt`; 131 mallas con normales y UV0/UV2 validas en `BlenderIntegrationMeshData.txt`. |
| Superficies | No se detectaron triangulos opacos exactamente coincidentes con igual normal entre las piezas nuevas y las existentes: 13.102 triangulos examinados, tolerancia 0,1 mm. Este detector no demuestra ausencia de todo solape parcial; se complementa con las vistas interiores y exteriores. `BlenderIntegrationCoincidentFaces.txt`. |
| Iluminacion | 131 renderers nuevos estaticos tienen ambos lightmaps; 22 renderers de puertas/paneles usan Light Probes. Dos atlas direccionales y 125 probes por escenario; 12 cubemaps actualizados. Ciclo dia-atardecer-dia restaura mapas, sol y los seis reflejos. `BlenderIntegrationCycle.txt`, `BlenderIntegrationBake.report.txt`, `BlenderIntegrationReflections.txt`. |
| Escaleras | Ascenso y descenso con W del teclado simulado, procesado por el ContinuousMoveProvider nativo. Termina en Y=3 y Y=0 respectivamente, sin caida. `BlenderIntegrationStairs.txt`. |
| Puertas y luces | Seleccion mediante XRInteractionManager/NearFarInteractor: 11 hojas abren y cierran; los 12 interruptores alternan y restauran su grupo. `BlenderIntegrationDoors.txt`, `BlenderIntegrationSwitches.txt`. Los 12 interruptores tienen al menos un rayo de aproximacion sin obstruccion desde 0,8 m (`BlenderIntegrationSwitchVisibility.txt`). |
| Menu y herramientas | Los tres TP alcanzan su destino. Medicion de 1 m/deshacer y pintura sobre malla nueva/restaurar comprobadas por API de la aplicacion. No acredita precision del gatillo de un visor fisico. `BlenderIntegrationTeleport.txt`, `BlenderIntegrationTools.txt`. |
| Regresion | Overrides del rig, TP, 416 objetos serializados del vecindario y mobiliario existente conservados. Jardin conserva transformaciones/materiales; URP anade datos nativos a cuatro luces existentes, sin cambiar sus propiedades Light. 82 archivos del vecindario verificados contra Git, incluidos hashes LFS. `BlenderIntegrationProtectedObjects.json`, `BlenderIntegrationRegression.json`. |
| Rendimiento | SRP Batcher nativo activo. Se descarto BatchingStatic: el controlador reasigna UV de lightmap al cambiar de escenario y Unity no admite esa operacion en un lote estatico. Aunque los atlas actuales coinciden y el ensayo bajo draw calls 1269 a 1149, no mejoro el tiempo de cuadro (7,34 a 7,44 ms) y genero avisos; no forma parte de la entrega. Las mediciones incluyen vecindario y sobrecarga del editor. Ver `BlenderIntegrationBatchingDecision.txt`. 51 texturas nuevas: estimacion ASTC con mipmaps de 46,97 MiB, excluyendo muebles existentes, lightmaps y cubemaps. `BlenderIntegrationPerformance*.txt`, `BlenderIntegrationAndroidTextures.txt`. |

## Puerta de entrega

La escena principal `Vivienda_Prueba.unity` y la rama `main` no se han sustituido. Falta la aprobacion visual de la escena de prueba para promoverla, como exige el plan acordado. Tambien quedan condicionados a disponer de un visor el perfil de GPU/memoria en Quest y la prueba fisica de mandos; el simulador no certifica esos resultados.

La procedencia de los recursos y las licencias conocidas se registran en `Assets/_Project/Art/BlenderNuevo/ORIGEN_Y_LICENCIAS.md`. El cliente no adjunto el detalle de autores/licencias de todas las texturas; no se presume CC0 ni permiso de redistribucion. No se descargaron assets adicionales.

## Mapa de materiales

Las variantes con el mismo acabado y diferente escala/desplazamiento UV se registran explicitamente para conservar las asignaciones recibidas.

| Material Blender | Asset URP |
|---|---|
| AR3DMat PBR Black Plaster casa | `M_68df538ba732.mat` |
| Black metal | `M_ca26157a3189.mat` |
| Black stone floor | `M_8722e5331482.mat` |
| Black wood | `M_cacf9cffe3a3.mat` |
| Brown teak laminate casa | `M_db29baeb2193.mat` |
| Concrete | `M_8c24f4af1fb8.mat` |
| Concrete look porcelain tile casa | `M_c8c5842f00be.mat` |
| Concrete look porcelain tile casa.001 | `M_037f4f01ba66.mat` |
| Concrete look porcelain tile casa.002 | `M_9dc0a2a81a5a.mat` |
| Concrete look porcelain tile casa.003 | `M_9368112da26c.mat` |
| Concrete look porcelain tile.001 casa | `M_a7c1ba6cc4ce.mat` |
| Concrete look porcelain tile.002 casa | `M_2c541aa26011.mat` |
| Concrete look porcelain tile.003 casa | `M_e78815a514a3.mat` |
| Concrete look porcelain tile.004 casa | `M_94c8e5c9d49e.mat` |
| Concrete look porcelain tile.005 casa | `M_c8c5842f00be.mat` |
| Concrete look porcelain tile.006 casa | `M_a2e207f1c3f5.mat` |
| Concrete look porcelain tile.007 casa | `M_5a36cd62a2f8.mat` |
| Concrete look porcelain tile.008 casa | `M_dd53dccad9c2.mat` |
| Concrete look porcelain tile.009 | `M_6c00d9575416.mat` |
| Concrete look porcelain tile.010 casa | `M_dd53dccad9c2.mat` |
| Concrete.001 | `M_8c24f4af1fb8.mat` |
| Dark Grey Concrete casa | `M_8ea14b294a35.mat` |
| Dark Walnut Wood PBR 4K | `M_d380b38fe15e.mat` |
| Decorative plaster | `M_73b1ab3b22c0.mat` |
| Decorative plaster casa | `M_73b1ab3b22c0.mat` |
| Decorative plaster.001 | `M_73b1ab3b22c0.mat` |
| Grey Veined Marble casa | `M_70a1c4a4f0ec.mat` |
| New asphalt casa | `M_aad139a7106e.mat` |
| Off White Painted Plaster casa | `M_e4005378b7ae.mat` |
| Off White Painted Plaster.001 | `M_e9658b89add4.mat` |
| Off White Painted Plaster.001 casa | `M_4c12492b73b7.mat` |
| Off White Painted Plaster.002 | `M_e9658b89add4.mat` |
| Porcelain tiles casa | `M_8e0058efa6e0.mat` |
| Porcelain tiles casa.002 | `M_1d5e9e0d4c41.mat` |
| Porcelain tiles.001 casa | `M_47ba2edad8db.mat` |
| Stone wall.001 | `M_3bdfc1550a67.mat` |
| Stone-effect porcelain tile casa | `M_ae40bc3c41dd.mat` |
| Stone-effect porcelain tile casa.001 | `M_fc7aefca91bb.mat` |
| Vertical wood slat | `M_20c5ca5397d7.mat` |
| Weathered Concrete casa | `M_aad3b1ba0620.mat` |
| Wood casa | `M_cb767ed34471.mat` |
| japanese_stone_wall casa 2 | `M_8a338e4b6dc7.mat` |
| medieval_blocks_03 casa | `M_85811fe51428.mat` |
| medieval_blocks_03 casa.001 | `M_54ff1601102b.mat` |

## Referencias tecnicas

- Configuracion de iluminacion y GI de Unity: https://docs.unity.com/en-us/engine/6000.0/manual/lighting-overview/lighting-configuration-workflow
- Light Probes para objetos dinamicos: https://docs.unity.com/en-us/engine/6000.0/manual/lighting-overview/direct-and-indirect-lighting/light-probes
- Costo de sombras en movil y XR: https://docs.unity.com/en-us/engine/6000.5/manual/lighting-overview/lighting/shadows-in-urp/shadows-optimization
- Optimizacion para XR independiente: https://docs.unity.com/en-us/engine/6000.5/manual/xr/graphics/untethered-device-optimization

Las sombras suaves y las luces dinamicas necesitan medicion en el visor; las capturas del editor no validan el presupuesto de GPU de Quest. No se agregaron SSAO, profundidad de campo ni efectos volumetricos.
