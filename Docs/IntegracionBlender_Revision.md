# Revision visual de la integracion

Escena: `Assets/Scenes/Vivienda_Integracion_Blender.unity`, rama `integracion/blender-vivienda`.
La escena principal y `main` conservan la version base. Esta pagina presenta el resultado de prueba que debe aprobarse antes de promoverlo.

Las vistas actuales incluyen sombras adicionales nativas URP, suavizado Low y MSAA 4x. Se corrigio la filtracion de luz junto al techo ajustando el bias local del sol. La geometria, los materiales y las colisiones de esta revision se conservan; el cambio respecto a la revision anterior es de iluminacion.

## Fachada

Antes:

![Fachada base](Media/BlenderIntegracion/Antes_Dia/fachada.png)

Despues:

![Fachada con acabados del Blender nuevo](Media/BlenderIntegracion/Despues_Dia/fachada.png)

La columna derecha usa la misma piedra que el muro izquierdo. Los vidrios transparentes, las barandas y el techo negro aprobados se conservan. Los materiales de las casas vecinas no se sustituyeron.

## Interiores

Primer piso:

![Interiores primer piso, dia](Media/BlenderIntegracion/Despues_Dia_Piso1.jpg)

Segundo piso:

![Interiores segundo piso, dia](Media/BlenderIntegracion/Despues_Dia_Piso2.jpg)

Segundo piso al atardecer:

![Interiores segundo piso, atardecer](Media/BlenderIntegracion/Despues_Atardecer_Piso2.jpg)

## Materiales originales y adaptacion

Blender, cocina con luces temporales para comparacion:

![Cocina del Blender recibido](Media/BlenderIntegracion/Fuente_Blender/cocina.png)

Unity, misma posicion y FOV:

![Cocina integrada en Unity](Media/BlenderIntegracion/Despues_Dia/cocina_piso2.png)

Se conservan la teca, el marmol y las asignaciones por cara del archivo recibido. El sofa y otros muebles que solo existen en Unity se preservan. Las luces temporales de Blender y el horneado de Unity son diferentes; estas vistas permiten comprobar la escala y ubicacion del acabado, sin afirmar igualdad entre renderizadores.

## Capturas completas

Las carpetas [antes de dia](Media/BlenderIntegracion/Antes_Dia), [antes al atardecer](Media/BlenderIntegracion/Antes_Atardecer), [despues de dia](Media/BlenderIntegracion/Despues_Dia) y [despues al atardecer](Media/BlenderIntegracion/Despues_Atardecer) incluyen las imagenes completas. `camaras.txt` registra posiciones y objetivos. Seis [renders de Blender](Media/BlenderIntegracion/Fuente_Blender) conservan los materiales originales y registran su configuracion temporal en `comparacion.json`.

| Zona | Dia | Atardecer |
|---|---|---|
| Fachada | [Ver](Media/BlenderIntegracion/Despues_Dia/fachada.png) | [Ver](Media/BlenderIntegracion/Despues_Atardecer/fachada.png) |
| Vista desde ventana | [Ver](Media/BlenderIntegracion/Despues_Dia/ventana.png) | [Ver](Media/BlenderIntegracion/Despues_Atardecer/ventana.png) |
| Trasera | [Ver](Media/BlenderIntegracion/Despues_Dia/trasera.png) | [Ver](Media/BlenderIntegracion/Despues_Atardecer/trasera.png) |
| Escalera | [Ver](Media/BlenderIntegracion/Despues_Dia/escalera.png) | [Ver](Media/BlenderIntegracion/Despues_Atardecer/escalera.png) |
| Techo/balcon | [Ver](Media/BlenderIntegracion/Despues_Dia/balcon_detalle.png) | [Ver](Media/BlenderIntegracion/Despues_Atardecer/balcon_detalle.png) |

## Diferencias que requieren criterio de diseno

- Los 16 componentes de armarios de Blender no se incorporan porque interfieren con el mobiliario o el paso de puertas actual. No se cambia la distribucion para encajarlos.
- Se conserva el techo negro aprobado, aunque el Blender propone madera en algunas de esas superficies. Tambien se conservan los cristales transparentes de Unity.
- El metal negro reproduce color y rugosidad mediante URP Lit; no replica exactamente el microrelieve/coat procedural.
- La lavanderia tiene una pieza sin material en la fuente. Sus caras superiores reciben porcelanato continuo con el suelo adyacente; el cielo inferior permanece blanco.
- Las camas, el lavabo, la lavadora y la cocina descargada del primer piso conservan sus modelos y acabados actuales.

Pruebas funcionales, rendimiento y reexportacion: [guia tecnica](IntegracionBlender.md). El rendimiento en un Meta Quest fisico queda pendiente de disponer del equipo.
