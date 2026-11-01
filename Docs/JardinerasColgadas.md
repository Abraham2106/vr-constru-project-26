# Jardineras exteriores del balcon

Las dos jardineras del segundo piso cuelgan por fuera de la baranda, siguiendo la referencia aprobada. Se conservan los vidrios, la baranda, la estructura y el resto de la vivienda.

Grupo: `Jardin_Cliente/Jardineras_Balcon`. Las cajas tienen centro Y=3,87 m y Z=4,45 m; su borde trasero queda a 6 cm del plano exterior del vidrio. Los soportes pasan sobre el pasamanos y tienen apoyo inferior. El paso por el interior del balcon queda libre. Se reutilizan los helechos existentes y se agregan seis instancias orientadas hacia abajo en el frente exterior.

Comprobado desde camara exterior y desde el balcon, sin errores de consola. La regresion serializada verifica que solo cambian ocho transformaciones de jardineras/plantas y la lista de hijos de su grupo; los componentes existentes de casa, vidrio, luces y XR se conservan. Ver `Validation/HangingPlanters.txt` y `HangingPlantersRegression.json`.

![Jardineras exteriores](Media/JardinerasColgadas/exterior.png)

![Desde el balcon](Media/JardinerasColgadas/desde_balcon.png)

Revision posterior: se retiran las 12 instancias anteriores y se sustituyen por 28 helechos fern_02 de Poly Haven ya presentes en el proyecto, 14 por caja. Dos filas por jardinera, cuatro variantes de malla, mayor volumen vertical y follaje exterior sin atravesar el vidrio (Z minimo 4,25 m). Se conserva la atribucion del asset en su carpeta original. Las capturas corresponden a esta revision; el informe de regresion original documenta solo la primera colocacion.
