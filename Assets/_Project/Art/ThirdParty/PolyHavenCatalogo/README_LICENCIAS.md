# Recursos Poly Haven — catálogo sección 03

Los siguientes siete modelos y sus mapas son de [Poly Haven](https://polyhaven.com/) y se descargaron mediante su API pública. Su [licencia de assets](https://polyhaven.com/license) es [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/): permite uso comercial y modificaciones sin atribución obligatoria. Se registran autores y enlaces para trazabilidad.

| Modelo | Autor | Fuente |
|---|---|---|
| Modern Wooden Cabinet | Patrik Pangerl | https://polyhaven.com/a/modern_wooden_cabinet |
| Modern Coffee Table 01 | Amin | https://polyhaven.com/a/modern_coffee_table_01 |
| Fern 02 | Rob Tuytel (escaneo), Rico Cilliers (modelado) | https://polyhaven.com/a/fern_02 |
| Wicker Basket 01 | Kuutti Siitonen | https://polyhaven.com/a/wicker_basket_01 |
| Planter Pot Clay | Amal Kumar | https://polyhaven.com/a/planter_pot_clay |
| Wooden Display Shelves 01 | James Ray Cock | https://polyhaven.com/a/wooden_display_shelves_01 |
| Wooden Table 02 | Serhii Khromov | https://polyhaven.com/a/wooden_table_02 |

Se descargó para cada asset el FBX de 1K y las dependencias enumeradas por el proveedor. `manifest.json` registra URLs directas, tamaño y MD5; los archivos fueron verificados al descargar. Estos archivos están en staging: su licencia no garantiza que estén ya importados, visualmente correctos, optimizados ni probados en Quest.

Al importar en Unity URP: revisar escala, orientación, materiales y referencias de texturas; los mapas `_nor_gl` requieren importación como mapa normal y comprobar la orientación, y la rugosidad debe convertirse al canal de suavidad adecuado para el shader. `fern_02` incluye máscara alfa separada: configurar recorte alfa y evaluar overdraw. Comprobar uso de memoria, geometría y rendimiento en el visor antes de distribuir.
