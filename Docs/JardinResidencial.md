# Jardin residencial

Escena: `Assets/Scenes/Vivienda_Prueba.unity`. Grupo: `Jardin_Residencial`.

La referencia del cliente se aplica al jardin y las cercas. Se conservan la casa, sus materiales, luces, cristales, colisiones y el rig XR. Las cercas anteriores permanecen desactivadas bajo `Vecindario_Placeholder/Vallas_linderos`.

- Ocho cercas de listones horizontales oscuros, una malla por lindero.
- Setos bajos, helechos y arbustos agrupados en bandas laterales con bordes.
- Copas reforzadas usando exclusivamente hojas del arbol existente; no se duplican troncos.
- Pasto 3D del asset `JardinCliente/Models/grass.fbx`, distribuido cada 30 cm, con matas superpuestas mas grandes y altura uniforme. Alterna las dos mallas del asset y combina 792 matas en 25 sectores. Las hojas reciben luz sin proyectar sombras adicionales. Patio, escaleras y huella de la casa excluidos.
- Cesped base con la textura existente `58_grass-8_diffuse_jpg.jpg`, sin brillo especular.

Reconstruccion en Editor: `Vivienda > Jardin > Aplicar jardin residencial`, fuera de Play. El generador reemplaza solo su propio grupo y conserva los GUID de los materiales/mallas generados. No se ejecuta codigo del generador en la aplicacion.

Procedencia: se reutilizan los assets de jardin entregados por el cliente y sus materiales; se conserva su documentacion de procedencia existente, sin atribuirles CC0. El follaje de Rubberduck tiene atribucion CC0 en `Assets/ThirdParty/RubberduckVegetation/ATTRIBUTION.txt`. La textura base ya formaba parte de los materiales del proyecto; no se descargaron assets nuevos. Las cercas y bandas son geometria simple creada en el editor.

Validacion: captura frontal y lateral de camara sin cuadricula; consola sin errores al aplicar. `Validation/ResidentialGardenRegression.json` comprueba los 4.134 bloques existentes: solo cambian la activacion de las cercas anteriores, el material del suelo exterior y la lista de raices de escena. Los otros 4.131 bloques se conservan. `Validation/ResidentialGarden.txt` registra el conteo de geometria. No se ha medido GPU ni rendimiento fisico en Quest; la agrupacion por sectores limita renderers, pero no certifica el presupuesto del visor.

![Vista frontal](Media/JardinResidencial/frente.png)

![Vista lateral](Media/JardinResidencial/lateral.png)

Para revertir: desactivar `Jardin_Residencial`, reactivar `Vallas_linderos` y reasignar al renderer `Suelo Zacate [693790]` el material `Assets/_Project/Art/Materials/Environment/ExteriorGround_Day.mat`. El commit permite restaurar la escena completa a la version anterior.
