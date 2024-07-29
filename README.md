
## WIP
Just ironing out a few kinks at the moment and have some TODOs left. But feel free to take a look around.

<br/>
<div align="center">
    <img align="center" src=".github/Squash%20Example.webp" alt="A car being squashed by anvils"/>
</div>
<br/>

# Lattice Deformer for Unity

Made with Unity 2021.3.5f1. Should work in other versions, but haven't tested it.  

Adds 3 components `Lattice`, `LatticeModifier` and `SkinnedLatticeModifier` for creating lattice deformations.  
Add lattices to the scene, and add modifier components to meshes to see them react.

### Skinned meshes support

Lattice modifiers can be applied to both static and skinned meshes. When it comes to skinned meshes, lattices can either be applied before or after skinning, or both.

### Compute shader based

Deformations are done using compute shaders, so performance is equivalent to GPU skinning. However, like skinning, meshes affected by lattices do not support instancing.  
**Note:** GPU skinning must be enabled within project settings for this to work.

### Shader support

Deformation is done seperately from rendering, so you are able to use whatever shader/material you'd like. As a bonus, the amount of stretching and squishing can be applied to a vertex channel and read in custom shaders for further dynamic effects.  
**Note:** Stretch vertex channel writing is currently disabled as it breaks skinned modifiers. Skinned meshes store UV info in a different vertex buffer so need to add support for writing to secondary vertex buffer.

<br/>
<div align="center">
    <img align="center" src=".github/Interaction%20Example.webp" alt="A curtain being displaced by a sphere"/>
</div>
<br/>

## Known issues
* Writing the amount of stretch and squish to vertex channel is currently disabled.  
* Meshes are being processed assuming they have position, normal and tangent data. If they don't have all 3 it may not behave correctly.

## Example
There is an example scene `Assets/Lattice/Example/Example Scene.unity` that demonstrates a car getting squashed by 2 anvils.

The models used are:  
Car: https://sketchfab.com/3d-models/pony-cartoon-885d9f60b3a9429bb4077cfac5653cf9 \
Anvil: https://sketchfab.com/3d-models/pbr-anvil-3529b9ef4e2c4a32add55948b5361609
