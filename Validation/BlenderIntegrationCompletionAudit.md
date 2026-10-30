# Estado contra el plan acordado

Fecha: 2026-10-30. Alcance: casa principal.
Plan: integracion por etapas del Blender del cliente. Base Git `4f4ba04`.

| Requisito | Estado | Evidencia |
|---|---|---|
| Worktree/rama independientes y escena con el mismo XR/TP | Comprobado | `integracion/blender-vivienda`, `Vivienda_Integracion_Blender.unity`; overrides del rig y los tres TP conservados en ProtectedObjects/Regression. |
| Capturas antes de intervenir y correccion del cielo blanco | Comprobado | Capturas base originales y comparaciones de `Docs/Media/BlenderIntegracion`; sunset clone independiente con exposicion 0,2. |
| Mapa por pieza, geometria, UV y material | Comprobado | PieceMap: 253 decisiones explicitas, 168 nombres comunes y 85 nuevos; Bindings TSV de la escena. Excepciones de diseno en la guia. |
| Trabajar sobre copia, exportar por partes y extraer texturas | Comprobado | SourceIntegrity, manifest con SHA256, Models/Textures; cuatro scripts reproducibles en Tools/Blender. La copia conserva su hash original. |
| Revisar normales/UV y superficies duplicadas | Comprobado dentro del alcance indicado | MeshData: 131 mallas con normales/UV validos; CoincidentFaces: 0 coincidencias exactas entre triangulos opacos con igual normal, 13.102 triangulos examinados. Complementado con vistas; no prueba ausencia de toda interseccion parcial. |
| Bake solo donde requiere conversion | Comprobado | Ocho molduras procedurales y cuatro superficies de jardineras; mapas de color sin iluminacion incrustada. Metal negro adaptado a URP Lit, con diferencia de microrelieve/coat declarada para revision. |
| Materiales URP separados, reutilizados y sin alterar vecinos | Comprobado | 38 combinaciones de biblioteca para 44 nombres, materiales adicionales horneados; importadores sRGB/lineales y roughness invertida. 82 archivos del vecindario coinciden con HEAD. |
| Compresion y tamanos Android | Comprobado en importadores | ASTC 6x6; albedo hasta 2K, normales/datos hasta 1K. AndroidTextures: 51 texturas activas nuevas, 46,97 MiB estimados con mipmaps. No es memoria medida en visor. |
| Integracion por zonas preservando interaccion/muebles | Comprobado con excepciones declaradas | 131 mallas fuente, 11 piezas interactivas con acabado nuevo. Puertas, vidrios aprobados, mobiliario, entorno y techo negro conservados. Armarios que interfieren excluidos; no se redisenan dormitorios. |
| GI, UV de lightmap, probes y luces con interruptores | Comprobado | Dos atlas direccionales/125 probes por escenario; 12 cubemaps; LightmapImporters, Bake.report, Reflections y Switches. |
| Ciclo dia-atardecer-dia | Comprobado tras retirar optimizacion incompatible | Cycle: mapas, seis reflejos, sol y coeficientes restaurados. BatchingStatic descartado, SRP Batcher activo. El puente MCP se reconecto; consola final sin errores y editor fuera de Play. |
| Comparar con Blender desde encuadres equivalentes | Comprobado con limite explicito | Seis renders Cycles, mismos puntos/FOV; fuente sin luces, por lo que usa iluminacion temporal documentada. Compara materiales/geometria, sin afirmar identidad de renderizadores. |
| Simulador: escaleras, puertas, luces, menu, TP y herramientas | Comprobado con alcance por informe | Stairs: entrada W nativa, ascenso/descenso. Doors: 11 hojas. Switches: 12 selecciones XRI. SwitchVisibility: 12 aproximaciones sin obstruccion. Teleport/Tools: menu y APIs. No prueba fisica de gatillo. |
| Regresion: vecinos, techo, mobiliario y rig | Comprobado | ProtectedObjects/Regression, fotos antes/despues. Cuatro luces existentes del jardin reciben metadatos nativos URP; sus propiedades originales y las plantas se conservan. |
| Medir draw calls, triangulos, texturas y costo de luces | Comprobado en editor, condicionado en Quest | Performance, AssetsAudit y AndroidTextures. El ensayo de static batching se documento y retiro; no acredita mejora de tiempo ni rendimiento de un visor. |
| Escena principal solo despues de aprobar la prueba | **Pendiente: aprobacion visual del usuario** | La escena principal y `main` conservan la base. La rama de integracion contiene la prueba revisable. No se promueve automaticamente. |
| Entrega organizada, guia, procedencia, capturas y commits separados | Preparada para revision | Docs/IntegracionBlender.md, IntegracionBlender_Revision.md, ORIGEN_Y_LICENCIAS, Media y commits de importacion, materiales, iluminacion e integracion. |
| Perfil y mandos en Meta Quest fisico | Condicionado a disponer del visor | El usuario dispone del simulador; no se afirma haber medido el dispositivo. |

La integracion de prueba esta preparada. La promocion a la escena principal queda pendiente de la aprobacion visual. La procedencia de texturas del cliente esta registrada; las licencias no adjuntadas no se inventan ni se presumen libres.
