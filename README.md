
# WIP
Just ironing out a few kinks at the moment and have some TODOs left. But feel free to take a look around.

![A car being squashed by anvils](.github/Squash%20Example.webp)

# Lattice Deformer for Unity
Made with Unity 2021.3.5f1. Should work in other versions, but haven't tested it.

Uses compute shaders. About as performant as GPU skinned mesh renderers.

Can also access the stretch/squash amount along UV channels within shaders, allowing for further dynamic effects.

Can be applied to both static or skinned meshes. Lattices can be applied both before and/or after skinning.

Adds 3 components `Lattice`, `LatticeModifier` and `SkinnedLatticeModifier` for creating lattice deformations.

![A curtain being displaced by a sphere](.github/Interaction%20Example.webp)

## Known issues
Builds are funky; mesh appears flat in X direction.

Meshes are being processed assuming they have position, normal and tangent data. If they don't have all 3 it may throw errors.

## Example
There is an example scene `Assets/Lattice/Example/Example Scene.unity` that demonstrates a car getting squashed by 2 anvils.

The models used are: \
Car: https://sketchfab.com/3d-models/pony-cartoon-885d9f60b3a9429bb4077cfac5653cf9 \
Anvil: https://sketchfab.com/3d-models/pbr-anvil-3529b9ef4e2c4a32add55948b5361609
