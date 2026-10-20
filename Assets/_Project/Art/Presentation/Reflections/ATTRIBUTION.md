# Entorno de reflexion del vidrio

`Blender_Forest.exr` es una copia del entorno `forest.exr` incluido con Blender 5.2, usado para reproducir la vista de materiales de la referencia proporcionada. Archivo original: `release/datafiles/studiolights/world/forest.exr` del proyecto Blender: https://github.com/blender/blender/blob/main/release/datafiles/studiolights/world/forest.exr.

El entorno corresponde a Ninomaru Teien, Greg Zaal, disponible bajo CC0 en Poly Haven: https://polyhaven.com/a/ninomaru_teien. Licencia: https://polyhaven.com/license.

Unity lo importa como cubemap HDR con convolucion especular. Solo los renderers de vidrio emplean las sondas de referencia mediante su `probeAnchor`; la iluminacion general y las sondas de la arquitectura mantienen el entorno de la escena.
