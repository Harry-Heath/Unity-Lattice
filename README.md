
## WIP
Just ironing out a few kinks at the moment and have some TODOs left. But feel free to take a look around. I will likely convert this to a package structure rather than project soon and separate examples and tests into another repository.

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
**Note:** GPU skinning must be enabled within project settings for skinned mesh support.

### Compute shader based

Deformations are done using compute shaders, so performance is about on par with GPU skinning. The more lattices you add though, the more performance will be hit. Like with skinning, meshes with lattice modifiers do not support instancing.

### Material support

Deformation is done before rendering, so you can use whatever material you'd like. As a bonus, the amount of stretching and squishing along UVs can be applied to a vertex channel and read in your own shaders for further dynamic effects.  

<br/>
<div align="center">
    <img align="center" src=".github/Interaction%20Example.webp" alt="A curtain being displaced by a sphere"/>
</div>
<br/>

## Known issues
* Meshes are being processed assuming they have position, normal and tangent data. If they don't have all 3 it may not behave correctly.

## Example
There is an example scene `Assets/Lattice/Example/Example Scene.unity` that demonstrates a car getting squashed by 2 anvils.

The models used are:  
Car: https://sketchfab.com/3d-models/pony-cartoon-885d9f60b3a9429bb4077cfac5653cf9 \
Anvil: https://sketchfab.com/3d-models/pbr-anvil-3529b9ef4e2c4a32add55948b5361609
