# Correcciones VR

scene=Assets/Scenes/Vivienda_Prueba.unity
compilation_errors=False
locomotion=True
menu_buttons=8/8
tool_host=True
main_light_realtime_shadows=False
selected_build_group=Android
TP_Exterior=MISSING
TP_Piso1=MISSING
TP_Piso2=MISSING
Escaleras_Rampa_PROV=(4.11, 1.45, 6.35)

## Prueba de Play

Play entered; start=(-3.09, 0.00, -12.94)
GravityProvider enabled=False
DynamicMoveProvider enabled=False
SimpleCharacterLocomotion enabled=True
Walk at 1.4m/s for 2s: (0.00, 0.00, 2.80)
Floor grounded=True position=(-3.09, -0.03, -10.14)
End=(-3.09, -0.03, -10.14) grounded=True displacement=(0.00, -0.03, 2.80)
Errors during Play=0


## Uso

WASD para caminar, Shift para correr. M o Y del mando izquierdo abre el menú. Espacio o gatillo derecho usa la herramienta; Ctrl o B cambia el color o deshace la medida.

## Límites

Sombras dinámicas desactivadas por el assert nativo ComputeShadowSplitMasks de Unity 6.5. Puertas y ventanas conservan sus bisagras provisionales. No se generó APK ni se midió rendimiento en un Quest físico; sigue pendiente instalar el módulo Android del editor.
