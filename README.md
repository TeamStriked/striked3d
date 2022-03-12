# Striked3d

An easy 2 use and simple the extend 2D/3D Game Engine with Vulkan and DirectX12 Rendering Backend

## Current Platform support

The rendering backend is fully multi-threaded and thread safe.

| Rendering 	| Windows 	| Linux 	| MacOS 	| Android 	| iOS 	|
|-----------	|---------	|-------	|-------	|---------	|-----	|
| Vulkan    	| X       	| X     	| X     	| X       	| X   	|
| DirectX12 	| -       	| -     	| -     	| -       	| -   	|
| Metal     	| -       	| -     	| -     	| -       	| -   	

## Current available nodes

* Node (Base class for each node)
* Viewports
* Windows (Installs a new window with its own rendering thread)
* CanvasItems (Contains 2D Rendering stuff like, create rectangles, draw text, etc.)
* Mesh
* Control (based on CanvasItems) and Control Elements like Buttons, etc.
* Camera3D (Camera instance for an scene)
* EditorGrid (Editor grid the preview window)

For 2d rendering we using an css-style positioning system (px or percent, relative or absolute).

## Current available node services

* GraphicsService (Rendering Backend)
* NodeTreeService (Node manager)
* InputService (Input manager)

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
* [Freetype](https://github.com/freetype) (For font-rendering)
* [AssimpNet](https://bitbucket.org/Starnick/assimpnet/src/master/) (For 3d model parsing)
* [BinaryPack](https://github.com/Sergio0694/BinaryPack) and [Json.NET](https://github.com/JamesNK/Newtonsoft.Json) (For serialization)
* [ImageSharp](https://github.com/SixLabors/ImageSharp) (For image import)

## :clap: Supporters

[![Stargazers repo roster for @TeamStriked/striked3d](https://reporoster.com/stars/TeamStriked/striked3d)](https://github.com/TeamStriked/striked3d)

