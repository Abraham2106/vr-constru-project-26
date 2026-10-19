# Materiales que requieren bake / re-export desde Blender

Los nodos procedurales de Blender **no** viajan en FBX estándar. Opciones:

## Export recomendado (FBX)
- **Path Mode:** Copy + Embed
- **Apply Transform**, **Apply Modifiers**
- **Forward:** -Z Forward, **Up:** Y Up, **Scale:** 1.0
- Bakear texturas PBR (Base Color, Normal, Roughness, Metallic) antes de export, o usar **glTF 2.0** con texturas embebidas.

## Materiales procedurales detectados en Unity
- `Procedural Grass` — sustituto temporal desde material donor en pipeline Agente 2.
- `Procedural Brown Leather` — sustituto temporal desde material donor en pipeline Agente 2.
- `Procedural pearl blue painted plastic` — sustituto temporal desde material donor en pipeline Agente 2.

## Otros materiales sin mapa albedo en import
- Acoustic Ceiling Tile (Assets/_Project/Art/Materials/Library/Techo.mat)
- Acoustic Ceiling Tile (Assets/_Project/Art/Materials/Vivienda/Acoustic Ceiling Tile.mat)
- AR3DMat Architectural Clear Glass (Assets/_Project/Art/Materials/Vivienda/AR3DMat Architectural Clear Glass.mat)
- Blanco mate (Assets/_Project/Art/Materials/Vivienda/Blanco mate.mat)
- Blanco mate (Assets/_Project/Art/Materials/Vivienda/Fidelity/Blanco mate.mat)
- Brown Leather Fabric (Assets/_Project/Art/Materials/Vivienda/Brown Leather Fabric.mat)
- Ceramic Tiles (Floor Tiles) (Assets/_Project/Art/Materials/Vivienda/Ceramic Tiles (Floor Tiles).mat)
- Ceramic Tiles (Floor Tiles) (Assets/_Project/Art/Materials/Vivienda/Fidelity/Ceramic Tiles (Floor Tiles).mat)
- Fallback_MissingTexture_PROV (Assets/_Project/Art/Materials/Vivienda/Fallback_MissingTexture_PROV.mat)
- Frosted Glass (Assets/_Project/Art/Materials/Vivienda/Frosted Glass.mat)
- Glass (Assets/_Project/Art/Materials/Library/Vidrio.mat)
- Glass (Assets/_Project/Art/Materials/Vivienda/Glass.mat)
- Glass.001 (Assets/_Project/Art/Materials/Vivienda/Glass.001.mat)
- Grass-Paving-Linear (Assets/_Project/Art/Materials/Vivienda/Grass-Paving-Linear.mat)
- Light (Assets/_Project/Art/Materials/Vivienda/Light.mat)
- Light Maple Wood Floor (Assets/_Project/Art/Materials/Vivienda/Light Maple Wood Floor.mat)
- Microcemento mate (Assets/_Project/Art/Materials/Vivienda/Fidelity/Microcemento mate.mat)
- Microcemento mate (Assets/_Project/Art/Materials/Vivienda/Microcemento mate.mat)
- Natural Stone Wall (Assets/_Project/Art/Materials/Vivienda/Natural Stone Wall.mat)
- Pine Wood Planks (Assets/_Project/Art/Materials/Vivienda/Pine Wood Planks.mat)
- Plastic (Assets/_Project/Art/Materials/Vivienda/Plastic.mat)
- Procedural Brown Leather (Assets/_Project/Art/Materials/Vivienda/Procedural Brown Leather.mat)
- Realistic ceramic with imperfections (Assets/_Project/Art/Materials/Vivienda/Realistic ceramic with imperfections.mat)
- Scaned Stone Wall (Assets/_Project/Art/Materials/Vivienda/Scaned Stone Wall.mat)
- Stone Bricks (Assets/_Project/Art/Materials/Vivienda/Stone Bricks.mat)
- Stylized Wood Planks (Assets/_Project/Art/Materials/Vivienda/Stylized Wood Planks.mat)
- Wall made of large natural stones (Assets/_Project/Art/Materials/Vivienda/Wall made of large natural stones.mat)
- Walnut Wood Plank Floor (Assets/_Project/Art/Materials/Vivienda/Walnut Wood Plank Floor.mat)
- White Painted Wood.001 (Assets/_Project/Art/Materials/Vivienda/White Painted Wood.001.mat)
- Wood Floor Faded Pine (Assets/_Project/Art/Materials/Vivienda/Wood Floor Faded Pine.mat)
- Wood Fluted Panel (Assets/_Project/Art/Materials/Vivienda/Wood Fluted Panel.mat)
- Wood Planks Light (Assets/_Project/Art/Materials/Vivienda/Wood Planks Light.mat)
- Wood Planks.001 (Assets/_Project/Art/Materials/Vivienda/Wood Planks.001.mat)

## Fuente Blender
- `C:\Users\solan\Downloads\Untitled.blend`
