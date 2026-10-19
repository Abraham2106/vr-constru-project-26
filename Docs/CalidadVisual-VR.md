# Calidad visual para una vivienda en realidad virtual

Fecha: 19 octubre 2026. Unity 6.5 / URP / OpenXR / Meta Quest.

## Objetivo

Comunicar escala, materiales e iluminación con claridad, con un presupuesto compatible con VR. La evaluación de GPU y comodidad en un visor físico esta pendiente.

## Referencias y dirección

### Twinmotion [R4]

Leer materiales, luz natural, contexto y proporciones con claridad. Se adapta su criterio de presentacion arquitectónica a la vivienda. Referencia estética; sus ejemplos de escritorio no establecen el coste admisible en Quest.

### Chaos Enscape [R5]

Recorrido a escala, revisión de acabados y comparación de estados. La visualización debe ayudar a comprender el proyecto. Priorizar coherencia y lectura espacial; conservar los controles e interacciones existentes.

### Meta + Unity [R1-R3]

PBR lineal, muestreo estable, reflejos preparados y control del coste por fotograma. La decisión final de calidad requiere medir en el visor objetivo; el simulador no mide su GPU.

## Aplicado

### Cámara y profundidad

Recorte de 0,05 a 80 m. Se mantiene el campo de vision y el seguimiento XR. Menos rango de profundidad innecesario para una vivienda y menor riesgo de conflictos entre superficies cercanas.

### Acabado de imagen

Volume nativo URP: Tonemapping Neutral y Color Adjustments (contraste +4, saturación -3, exposición 0). Acabado moderado. Son decisiones artísticas del proyecto, no valores universales.

### Reflejos locales [R3,R6]

2 sondas, una por nivel; 4 cubemaps HDR de 128 px/cara, separados para día y noche. Box Projection activo. Se hornean en editor. El ciclo cambia las texturas; no captura la escena cada fotograma.

### Plafones e interruptores

12 difusores con variante emisiva habilitada; su PropertyBlock responde al estado de cada habitacion. La luminaria se lee encendida sin sumar otro foco ni un efecto de bloom.

### Texturas y nitidez

61 texturas usadas por la vivienda revisadas con mipmaps, filtro trilineal y anisotropía 4x. Mejor muestreo en suelos, madera y superficies oblicuas. Se conservan los mapas y resoluciones fuente.

### Base conservada

Espacio lineal, materiales PBR, cesped a escala, MSAA 4x y render scale 1. La locomocion nativa y el comportamiento de puertas, medición y pintura se conservan.

Los reflejos preparados son especulares; no sustituyen GI difusa. Los cubemaps no actualizan pintura, muebles o luces en tiempo real. Neutral y los ajustes de color requieren una prueba A/B de GPU en el visor.

## Plan priorizado

### P0 / visor real

Medir CPU, GPU, memoria y estabilidad durante 10 minutos; recorrer ambos niveles, abrir menú y activar el caso más exigente de luces.

Aceptación: Elegir 72 o 90 Hz según visor. Presupuesto total: 13,89 o 11,11 ms/fotograma. Son objetivos propuestos, no resultados medidos.

### P0 / coste del acabado

Comparar el perfil Neutral activado/desactivado con el mismo recorrido. Medir tambien MSAA y resolución.

Aceptación: Conservar la mejora solo si hay margen sostenido. Proponer un 20% de margen inicial y ajustarlo con el perfil real.

### P1 / luz indirecta

Preparar UV de lightmap y un flujo de bake compatible con los estados día/noche. Evaluar lightmaps, Light Probes o APV según soporte y presupuesto.

Aceptación: No dejar un bake diurno iluminando la noche. Validar fugas de luz, contactos suelo-pared y puertas abiertas/cerradas.

### P1 / acabados

Auditar densidad de texel y dirección de veta en muros, puertas y suelos. Mantener canales PBR, UV y tangentes.

Aceptación: Propuesta inicial: 256-512 px/m general; 512-1024 px/m en zonas de inspeccion. Ajustar por visibilidad y memoria.

### P1 / mobiliario y contexto

Completar los espacios vacios con activos a escala y un lenguaje coherente. Desarrollar un entorno próximo ligero.

Aceptación: Usar activos con licencia y LOD adecuados. Mantener recorridos y puertas libres; no improvisar la distribucion arquitectónica.

### P1 / memoria Android

Configurar y comparar ASTC por canal desde los importadores nativos. Revisar las texturas realmente usadas y retirar duplicados demostrados.

Aceptación: Comparar normal y rugosidad tras comprimir; conservar HDR donde sea necesario. Registrar memoria residente en dispositivo.

### P2 / optimización dirigida

Aplicar LOD, occlusion y agrupacion por zonas cuando el perfil muestre un coste. Revisar cristal y solapamiento de luces.

Aceptación: Evitar unir toda la vivienda en una sola malla. Verificar culling y transparencia al abrir puertas y cambiar de planta.

### P2 / presentacion

Preparar puntos de vista, variantes de acabados y capturas repetibles; afinar las indicaciones visuales de seleccion.

Aceptación: Cada vista debe responder una pregunta de diseño. Mantener legibilidad del menú y comodidad de interacción.

## Validación

Errores Play=0
Reflejos día=Piso1_Dia, Piso2_Dia
Reflejos noche=Piso1_Noche, Piso2_Noche
Plafones emisivos encendidos=4
Cámara=0.05..80m
Día/noche exterior e interior capturados.

Compilacion de scripts Android verificada por el editor; no equivale a un APK probado en visor. Se mantiene la locomocion nativa.

## Mantenimiento

Volver a hornear los reflejos al modificar geometría, acabados, mobiliario o cielo. Menú: Vivienda > Graficos > Hornear reflejos de día y noche. Perfil: Aplicar perfil de visualización.

No incorporar iluminación al albedo. Mantener datos PBR en espacio lineal. La luz visual no equivale a un estudio solar o fotométrico sin datos de emplazamiento y luminarias verificadas.

## Fuentes oficiales

- [R1: Meta - Best practices for Quest and PCVR](https://developers.meta.com/vr/documentation/unity/unity-best-practices-intro/)
- [R2: Unity 6 - Set a project color space](https://docs.unity3d.com/6000.0/Documentation/Manual/set-project-color-space.html)
- [R3: Unity 6 / URP - Reflection probes](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lighting/reflection-probes.html)
- [R4: Twinmotion - Architecture rendering and visualization](https://www.twinmotion.com/uses/architecture?lang=en-US)
- [R5: Chaos Enscape - Real-time visualization, VR and materials](https://www.chaos.com/enscape/features)
- [R6: Unity 6 - Lightmapping.BakeReflectionProbe](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Lightmapping.BakeReflectionProbe.html)

Los criterios de adaptacion y prioridades son una propuesta especifica para este proyecto.
