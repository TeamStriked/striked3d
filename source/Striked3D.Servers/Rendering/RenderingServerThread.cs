using Striked3D.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using Silk.NET.Windowing;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using SubmitInfo = Silk.NET.Vulkan.SubmitInfo;
using Result = Silk.NET.Vulkan.Result;
using Striked3D.Servers.Rendering.Vulkan;
using Window = Striked3D.Core.Window;
using Striked3D.Servers.Rendering;
using Striked3D.Types;
using System.Collections;
using System.Reflection;
using Silk.NET.Vulkan;

namespace Striked3D.Servers
{

    [ApiDeclaration("RenderingServer")]
    public class RenderingServerThread : ServerThreadRunner
    {
        protected IWindow _window { get; set; }

        protected VulkanDriver driver = new VulkanDriver();

        public void Draw(double delta)
        {
            driver.Draw(delta);
        }

        public void Destroy()
        {
            driver.Destroy();
        }

        public void Initialize(Window win)
        {
            driver.rootViewport = CreateViewport();
            SetViewportPosition(driver.rootViewport, new Vector2D<int>(0, 0));
            SetViewportSize(driver.rootViewport, new Vector2D<int>(win.NativeWindow.FramebufferSize.X, win.NativeWindow.FramebufferSize.Y));
            driver.Initialize(win);
            driver.baseMaterial = CreateMaterial();

            SetMaterialShader(driver.baseMaterial, ShaderStageFlags.ShaderStageVertexBit, "shader.vert");
            SetMaterialShader(driver.baseMaterial, ShaderStageFlags.ShaderStageFragmentBit, "shader.frag");

            win.NativeWindow.FramebufferResize += (Silk.NET.Maths.Vector2D<int> size) => {
                SetViewportPosition(driver.rootViewport, new Vector2D<int>(0, 0));
                SetViewportSize(driver.rootViewport, new Vector2D<int>(win.NativeWindow.FramebufferSize.X, win.NativeWindow.FramebufferSize.Y));
                driver.OnFramebufferResize(size);
            };
        }


        [ApiDeclarationMethod]
        public Guid CreateViewport()
        {
            Guid g = Guid.NewGuid();
            driver.Viewports.Add(g, new RenderViewport { size = Vector2D<int>.One, position = Vector2D<int>.One });
            return g;
        }


        [ApiDeclarationMethod]
        public void SetViewportPosition(Guid viewport, Vector2D<int> position)
        {
            if (!driver.Viewports.ContainsKey(viewport))
            {
                throw new Exception("Cant find viewport");
            }
            else
            {
                var viewportObj = driver.Viewports[viewport];
                viewportObj.position = position;
                driver.Viewports[viewport] = viewportObj;
            }
        }

        [ApiDeclarationMethod]
        public void SetViewportSize(Guid viewport, Vector2D<int> size)
        {
            if (!driver.Viewports.ContainsKey(viewport))
            {
                throw new Exception("Cant find viewport");
            }
            else
            {
                var viewportObj = driver.Viewports[viewport];
                viewportObj.size = size;
                driver.Viewports[viewport] = viewportObj;
            }
        }

        [ApiDeclarationMethod]
        public void SetViewportCamera(Guid viewport, Guid cameraId)
        {
            if (!driver.Viewports.ContainsKey(viewport))
            {
                throw new Exception("Cant find viewport");
            }
            else
            {
                var viewportObj = driver.Viewports[viewport];
                viewportObj.camera = cameraId;
                driver.Viewports[viewport] = viewportObj;
            }
        }

        [ApiDeclarationMethod]
        public Guid CreateMesh()
        {
            Guid g = Guid.NewGuid();
            var renderable = new RenderObject();

            renderable.transformMatrix = Matrix4X4<float>.Identity;
            renderable.viewport = driver.rootViewport;
            driver.Renderables.Add(g, renderable);

            return g;
        }

        [ApiDeclarationMethod]
        public Guid CreateCanvas()
        {
            Guid g = Guid.NewGuid();
            var canvas = new RenderCanvas();

            canvas.transformMatrix = Matrix4X4<float>.Identity;
            canvas.viewport = driver.rootViewport;

            driver.Canvas.Add(g, canvas);
            return g;
        }

        [ApiDeclarationMethod]
        public void CreateCanvasRectangle(Guid canvasID, Vector2D<float> Position, Vector2D<float> Size, Vector4D<float> Color)
        {
            if (driver.Canvas.ContainsKey(canvasID))
            {
                var meshObj = driver.Canvas[canvasID];
                meshObj.elements.Add(new CanvasRect { Size = Size, Position = Position, Color = Color });
                driver.Canvas[canvasID] = meshObj;
            }
            else
            {
                throw new Exception("Cant find mesh");
            }
        }

        [ApiDeclarationMethod]
        public void SetTransform(Guid meshId, Vector3D<float> pos, Vector3D<float> scale)
        {
            if (driver.Renderables.ContainsKey(meshId))
            {
                var meshObj = driver.Renderables[meshId];
                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(pos);

                driver.Renderables[meshId] = meshObj;
            }
            else
            {
                throw new Exception("Cant find mesh");
            }
        }

        [ApiDeclarationMethod]
        public void SetMeshData(Guid meshId, int surface, RenderPrimitiveType type, Hashtable meshData)
        {
            if (driver.Renderables.ContainsKey(meshId))
            {
                var meshObj = driver.Renderables[meshId];

                if (!meshObj.surfaces.ContainsKey(surface))
                {
                    meshObj.surfaces.Add(surface, new RenderObjectSurface
                    {
                        material = driver.baseMaterial
                    });
                }

                var found = meshObj.surfaces[surface];

                var positions = meshData.Get<Vector3D<float>[]>("positions");
                var normals = meshData.ContainsKey("normals") ? meshData.Get<Vector3D<float>[]>("normals") : null;
                var colors = meshData.ContainsKey("colors") ? meshData.Get<Vector3D<float>[]>("colors") : null;
                var indicies = meshData.ContainsKey("indicies") ? meshData.Get<ushort[]>("indicies") : null;

                var vertexData = new Vertex[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    vertexData[i].position = positions[i];
                    vertexData[i].normal = (normals == null || normals.Length != positions.Length) ? Vector3D<float>.Zero : normals[i];
                    vertexData[i].color = (colors == null || colors.Length != colors.Length) ? new Vector3D<float>(1f, 0f, 0f) : colors[i];
                }

                found.vertices = vertexData;
                found.indicies = indicies;
                found.type = type;
                found.isDirty = true;

                meshObj.surfaces[surface] = found;
                driver.Renderables[meshId] = meshObj;
            }
            else
            {
                throw new Exception("Cant find mesh");
            }
        }

        [ApiDeclarationMethod]
        public Guid CreateMaterial()
        {
            Guid g = Guid.NewGuid();

            var graphicsPipeline = new GraphicsPipeline();
            graphicsPipeline.info = Vertex.GetVertexDescription();

            Material mat = new Material();
            mat.pipeline = graphicsPipeline;
            mat.parameters = new MaterialParameters();

            driver.Materials.Add(g, mat);
            return g;
        }

        [ApiDeclarationMethod]
        public void SetMaterialShader(Guid material, ShaderStageFlags flag, string filename)
        {
            if (!driver.Materials.ContainsKey(material))
            {
                throw new Exception("Cant find material");
            }
            try
            {
                var mat = driver.Materials[material];
                mat.parameters.shaders.Add(flag, filename);
                mat.isDirty = true;

                driver.Materials[material] = mat;
            }
            catch (Exception ex)
            {
                Logger.Error(this, "Cant set shader " + material, ex.StackTrace);
            }
        }

        [ApiDeclarationMethod]
        public void SetMaterialParameter(Guid material, string name, object parameter)
        {
            if (!driver.Materials.ContainsKey(material))
            {
                throw new Exception("Cant find material");
            }

            try
            {
                var mat = driver.Materials[material];
                mat.isDirty = true;

                object par = mat.parameters;
                var piShared = par.GetType().GetProperty(name);
                piShared.SetValue(par, parameter, null);
                mat.parameters = (MaterialParameters)par;

                driver.Materials[material] = mat;
            }
            catch (Exception ex)
            {
                Logger.Error(this, "Cant set paramter " + name, ex.StackTrace);
            }
        }

        [ApiDeclarationMethod]
        public void SetPriority(Guid mesh, int priority)
        {
            if (driver.Renderables.ContainsKey(mesh))
            {
                var meshObj = driver.Renderables[mesh];
                meshObj.Priority = priority;
                driver.Renderables[mesh] = meshObj;
            }
            else
            {
                throw new Exception("Cant find mesh");
            }
        }

        [ApiDeclarationMethod]
        public void SetMaterial(Guid mesh, int surface, Guid material)
        {
            if (!driver.Materials.ContainsKey(material))
            {
                throw new Exception("Cant find material");
            }

            if (driver.Renderables.ContainsKey(mesh))
            {
                var meshObj = driver.Renderables[mesh];

                if (!meshObj.surfaces.ContainsKey(surface))
                {
                    meshObj.surfaces.Add(surface, new RenderObjectSurface());
                }

                var found = meshObj.surfaces[surface];
                found.material = material;
                meshObj.surfaces[surface] = found;

                driver.Renderables[mesh] = meshObj;
            }
            else
            {
                throw new Exception("Cant find mesh");
            }
        }

    }
}
