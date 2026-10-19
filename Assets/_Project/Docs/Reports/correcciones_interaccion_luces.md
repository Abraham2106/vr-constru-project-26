# Interacciones e iluminacion

Escena: Assets/Scenes/Vivienda_Prueba.unity

## Controles

- Quest: apuntar a la puerta o ventana con el rayo y pulsar Grip; pulsar de nuevo para cerrar. No hace falta arrastrar la manija.
- Simulador: mirar a la puerta o ventana a menos de 3 metros y pulsar E.
- M o Y abre el menu. Los botones del menu se seleccionan con el gatillo.

## Cambios

Rayos near/far activos en ambos mandos; el modo de teletransporte ya no los desactiva. Objetivos XRSimpleInteractable sobre toda la hoja, con los antiguos agarres desactivados. Giro animado con pose de cierre conservada y audio de corto alcance. Iluminacion de dia con un sol y ambiente del cielo; luces de relleno apagadas. Dos puntos interiores de intensidad 0.85 y alcance 4.5 m, solo de noche.

## Comprobacion de Play

Near/far activos=2
Puertas registradas XR=9/9 rayos fisicos=9/9
Ventanas con objetivo XR=2/2
Luces dia=1
Puertas abiertas por seleccion XR=9/9
Ventanas abiertas=2/2
Puertas cerradas por seleccion XR=9/9
Ventanas cerradas=2/2
Luces noche=3
Luces al volver al dia=1
Errores Play=0

La seleccion se comprobo mediante el gestor XR del editor; no se probo en un Quest fisico.
