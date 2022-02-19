using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using System.Linq;
using Veldrid.SPIRV;
using Striked3D.Platform;
using Striked3D.Core;
using Striked3D.Services;

namespace Striked3D.Graphics
{
    public class Renderer : IRenderer
    {
        private readonly CommandList clist;
        private readonly GraphicService service;
        private Guid lastViewport = Guid.Empty;
        private Guid lastMaterial = Guid.Empty;
        private readonly double delta;

        private int lastIndexBuffer = 0;
        private int lastVertexBuffer = 0;
        private int lastResourceSet = 0;

        public double Delta => delta;

        public bool requiredWait = false;

        public Renderer(CommandList clist, GraphicService service, double delta)
        {
            this.clist = clist;
            this.service = service;
            this.delta = delta;
        }

        public DeviceBuffer indexDefaultBuffer => service.indexDefaultBuffer;
        public ResourceLayout Material3DLayout => service.Material3DLayout;
        public ResourceLayout Material2DLayout => service.Material2DLayout;
        public ResourceLayout TransformLayout => service.TransformLayout;
        public ResourceLayout FontAtlasLayout => service.FontAtlasLayout;
        public ResourceSet DefaultTextureSet => service.DefaultTextureSet;
        public IMaterial Default2DMaterial => service.Default2DMaterial;
        public IMaterial Default3DMaterial => service.Default3DMaterial;

        public Shader[] CreateShader(string vertexCode, string fragmentCode)
        {
            if (PlatformInfo.DebugRendering)
            {
                Logger.Debug(this, "CreateShader");
            }

            var vertexShaderDesc = new ShaderDescription
             (
                 ShaderStages.Vertex,
                 Encoding.UTF8.GetBytes(vertexCode),
                 "main"
             );

            var fragmentShaderDesc = new ShaderDescription
            (
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragmentCode),
                "main"
            );

            return this.service.Renderer3D.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        }

        public Veldrid.Pipeline CreatePipeline(GraphicsPipelineDescription desc)
        {
            if (PlatformInfo.DebugRendering)
            {
                Logger.Debug(this, "CreatePipeline");
            }
            desc.Outputs = service.Renderer3D.MainSwapchain.Framebuffer.OutputDescription;
            return this.service.Renderer3D.ResourceFactory.CreateGraphicsPipeline(desc);
        }

        public void SetViewport(IViewport viewport)
        {
            if(viewport != null && lastViewport != viewport.Id)
            {
                clist.SetViewport(0, new Veldrid.Viewport(viewport.Position.X, viewport.Position.Y, viewport.Size.X, viewport.Size.Y, 0, 1));
                clist.SetScissorRect(0, (uint)viewport.Position.X, (uint)viewport.Position.Y, (uint)viewport.Size.X, (uint)viewport.Size.Y);
                lastViewport = viewport.Id;
            }
        }

        public void SetMaterial(IMaterial mat)
        {
            if(mat != null && lastMaterial !=  mat.Id)
            {
                clist.SetPipeline(mat.Pipeline);
                lastMaterial = mat.Id; 
            }
        }

        public void SetResourceSets(ResourceSet[] sets)
        {
            if(sets  != null && sets.Length > 0)
            {
                var hash = sets.Sum(df => df.GetHashCode());
                if(lastResourceSet == 0 || hash != lastResourceSet)
                {
                    for (uint i = 0; i < sets.Length; i++)
                    {
                        clist.SetGraphicsResourceSet(i, sets[i]);
                    }

                    lastResourceSet = hash;
                }
            }
        }

        public unsafe void UpdateBuffer<T>(
          DeviceBuffer buffer,
          uint bufferOffsetInBytes,
          T source) where T : unmanaged
        {
            if (PlatformInfo.DebugRendering)
            {
                Logger.Debug(this, "UpdateBuffer");
            }

            clist.UpdateBuffer(buffer, bufferOffsetInBytes, source);
            this.requiredWait = true;
        }

        public void UpdateBuffer<T>(
           DeviceBuffer buffer,
           uint bufferOffsetInBytes,
           T[] source) where T : unmanaged
        {
            if(PlatformInfo.DebugRendering)
            { 
                Logger.Debug(this, "UpdateBuffer");
            }

            clist.UpdateBuffer(buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)source);
            this.requiredWait = true;
        }

        public DeviceBuffer CreateBuffer(BufferDescription desc)
        {
            if (PlatformInfo.DebugRendering)
            {
                Logger.Debug(this, "CreateBuffer");
            }

            this.requiredWait = true;
            return service.Renderer3D.ResourceFactory.CreateBuffer(desc);
        }

        public ResourceSet CreateResourceSet(ResourceSetDescription description)
        {
            if (PlatformInfo.DebugRendering)
            {
                Logger.Debug(this, "CreateResourceSet");
            }

            this.requiredWait = true;
            return service.Renderer3D.ResourceFactory.CreateResourceSet(description);
        }

        public void BindBuffers(DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer = null)
        {
            if (vertexBuffer != null && (lastVertexBuffer == 0 || lastVertexBuffer != vertexBuffer.GetHashCode()))
            {
                clist.SetVertexBuffer(0, vertexBuffer);
                lastVertexBuffer = vertexBuffer.GetHashCode();
            }

            if (indexBuffer != null && (lastIndexBuffer == 0 || lastIndexBuffer != indexBuffer.GetHashCode()))
            {
                clist.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
                lastIndexBuffer = indexBuffer.GetHashCode();
            }
        }

        public void DrawIndexInstanced(int indiceLength, int instances = 1)
        {
            clist.DrawIndexed((uint)indiceLength, (uint)instances, 0, 0, 0);
        }

        public void DrawInstanced(int vertexAmount, int instances = 1)
        {
            clist.Draw((uint)vertexAmount, (uint)instances, 0, 0);
        }

        public void PushConstant<T>(
            T source) where T : unmanaged
        {
            clist.PushConstant(source);
        }
    }
}
