# Striked3d

A 3D Game Engine with Vulkan and DirectX12 Rendering Backend

## Current Platform support

| Rendering 	| Windows 	| Linux 	| MacOS 	| Android 	| iOS 	|
|-----------	|---------	|-------	|-------	|---------	|-----	|
| Vulkan    	| X       	| X     	| X     	| X       	| X   	|
| DirectX12 	| -       	| -     	| -     	| -       	| -   	|
| Metal     	| -       	| -     	| -     	| -       	| -   	

## Principle of simplicity and expandability

The goal is to provide a simple and easily extendable game engine. We rely exclusively 
on C# (also applies to the backend) and visual scripting as the programming language. 
The entire engine, including the backend, is therefore high-performing and also easy to expand.

## Construct

Similar to Godot, we rely on a node system without components and entities. But all the more on services.

Classic structure looks like this: Scene -> Node -> Resource

We particularly rely on OOP.

## Libraries in use

* [Silk.NET.Math](https://github.com/dotnet/Silk.NET/tree/main/src/Maths) (Fully integrated and modified)
* [Veldrid](https://github.com/mellinoe/veldrid)  (Most parts changed, not ready yet.)
* [Freetype] (https://github.com/freetype) (For font-rendering)
* [AssimpNet] (https://bitbucket.org/Starnick/assimpnet/src/master/) (For 3d model parsing)
* [BinaryPack] (https://github.com/Sergio0694/BinaryPack) and [Json.NET] (https://github.com/JamesNK/Newtonsoft.Json) (For serialization)
* [ImageSharp] (https://github.com/SixLabors/ImageSharp) (For image import)
