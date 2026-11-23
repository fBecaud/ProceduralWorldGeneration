# Procedural World Generation

Unity version : 6000.3.6f1

## Controls
Camera rotates around the planet.
Keys (in editor): arrows = rotate, I / O = zoom in / out.
Arrows and Page keys are used on purpose: letter keys move around between QWERTY and AZERTY layouts.

## Distribution
Vegetation distribution uses Poisson disk sampling (Bridson's algorithm). Candidates can be rejected along height and slope conditions. Its density can be adjusted by a noisemap.  Then it randomizes the scale and an offset to the planet's normal.