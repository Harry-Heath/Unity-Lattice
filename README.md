
# Lattice Deformer for Unity
Built and tested with Unity 2021.3.5f1. Should work in other versions, but haven't tested it.

Adds 3 components `Lattice`, `LatticeModifier` and `SkinnedLatticeModifier` for creating lattice deformations.

## Known issues
There is currently some editor only code inside `LatticeFeature.cs`, this will need `#if UNITY_EDITOR` wrapped around it, or have it moved somewhere else.

Meshes are being processed assuming they have position, normal and tangent data. If they don't have all 3 it may throw errors.

## Example
There is an example scene `Assets/Lattice/Example/Example Scene.unity` that demonstrates a car getting squashed by 2 anvils.

The models used were: \
Car: https://sketchfab.com/3d-models/pony-cartoon-885d9f60b3a9429bb4077cfac5653cf9 \
Anvil: https://sketchfab.com/3d-models/pbr-anvil-3529b9ef4e2c4a32add55948b5361609
