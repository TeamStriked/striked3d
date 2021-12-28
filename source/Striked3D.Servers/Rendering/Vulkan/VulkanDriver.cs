using System;
using System.Collections.Generic;
using System.Text;
using SubmitInfo = Silk.NET.Vulkan.SubmitInfo;
using Result = Silk.NET.Vulkan.Result;
using PipelineBindPoint = Silk.NET.Vulkan.PipelineBindPoint;
using Silk.NET.Input;
using Striked3D.Core;
using Silk.NET.Vulkan;
using System.Linq;
using Striked3D.Types;
using System.Collections;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using Silk.NET.Core.Native;
using System.Runtime.InteropServices;
using System.IO;
using Striked3D.Servers.Camera;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class VulkanDriver : RenderingDriver
    {
        public Dictionary<Guid, Material> Materials = new Dictionary<Guid, Material>();
        public Dictionary<Guid, RenderObject> Renderables = new Dictionary<Guid, RenderObject>();
        public Dictionary<Guid, RenderViewport> Viewports = new Dictionary<Guid, RenderViewport>();
        public Dictionary<Guid, RenderCanvas> Canvas = new Dictionary<Guid, RenderCanvas>();
        private int _currentFrame;
        private IInputContext _inputContext;
        public Guid rootViewport;
        public Guid baseMaterial;
 
        public override void Initialize(Window win)
        {
            _window = win;

      
            instance = new RenderingInstance(win.NativeWindow);
            instance.Instanciate();

            debugger = new Debugger(instance);
            debugger.Instanciate();

            instance.CreateSurface();

            physicalDevice = new PhysicalDevice(instance);
            physicalDevice.Instanciate();

            logicalDevice = new LogicalDevice(instance, physicalDevice);
            logicalDevice.Instanciate();

            swapChain = new Swapchain(instance, logicalDevice);
            swapChain.Instanciate();

            swapChain.CreateImageViews();

            renderPass = new RenderPass(instance, swapChain);
            renderPass.Instanciate();

            commandPool = new CommandPool(instance, logicalDevice);
            commandPool.Instanciate();

            swapChain.createColorTexture(commandPool);
            swapChain.createDepthTexture(commandPool);

            frameBuffers = new FrameBuffers(instance, renderPass);
            frameBuffers.Instanciate();

            commandBuffers = new CommandBuffers(instance, logicalDevice, commandPool);
            commandBuffers.Instanciate();

            queue = new RenderQueue(instance, swapChain);
            queue.Instanciate();

     
        }

        public void OnFramebufferResize(Silk.NET.Maths.Vector2D<int> size)
        {
            _framebufferResized = true;
            RecreateSwapChain();
            _window.NativeWindow.DoRender();
        }

        private unsafe void RecreateSwapChain()
        {
            Silk.NET.Maths.Vector2D<int> framebufferSize = _window.NativeWindow.FramebufferSize;

            while (framebufferSize.X == 0 || framebufferSize.Y == 0)
            {
                framebufferSize = _window.NativeWindow.FramebufferSize;
                _window.NativeWindow.DoEvents();
            }

            logicalDevice.WaitFor();
            CleanupSwapchain();

            // TODO: On SDL it is possible to get an invalid swap chain when the window is minimized.
            // This check can be removed when the above frameBufferSize check catches it.
            while (!swapChain.Instanciate())
            {
                _window.NativeWindow.DoEvents();
            }

            swapChain.createColorTexture(commandPool);
            swapChain.createDepthTexture(commandPool);

            swapChain.CreateImageViews();
            renderPass.Instanciate();

            foreach (var mat in Materials)
            {
                mat.Value.pipeline.Instanciate(mat.Value.parameters, instance, renderPass);
            }

            frameBuffers.Instanciate();
            commandBuffers.Instanciate();
        }

        public static float DegreesToRadians(float degrees)
        {
            return (float) (Math.PI / 180f) * degrees;
        }

        protected unsafe void DrawMeshes()
        {
            var camera = _window.GetService<CameraServer>();
            var activeCameraData = camera.GetActiveCamera();

            ulong lastPipe = 0;
            foreach (var mesh in Renderables.OrderBy(d => d.Value.Priority))
            {
                foreach(var surface in mesh.Value.surfaces)
                {
                    var renderViewport = this.Viewports[mesh.Value.viewport];
                    var material = this.Materials[surface.Value.material];

                    if (!material.pipeline.IsCreated)
                        continue;

                    var viewport = new Viewport();

                    viewport.X = renderViewport.position.X;
                    viewport.Y = renderViewport.position.Y;
                    viewport.Width = renderViewport.size.X;
                    viewport.Height = renderViewport.size.Y;
                    viewport.MinDepth = 0.0f;
                    viewport.MaxDepth = 1.0f;

                    instance.Api.CmdSetViewport(commandBuffers.Buffer, 0, 1, viewport);

                    var scissorOffset = new Offset2D();
                    scissorOffset.X = 0;
                    scissorOffset.Y = 0;

                    var scissorExtent = new Extent2D();
                    scissorExtent.Width = (uint) this._window.NativeWindow.Size.X;
                    scissorExtent.Height = (uint)this._window.NativeWindow.Size.Y;

                    var scissor = new Rect2D();
                    scissor.Offset = scissorOffset;
                    scissor.Extent = scissorExtent;

                    ulong vertex_offset = 0;

                    ReadOnlySpan<Buffer> vertex_buffers = stackalloc Buffer[] { surface.Value.renderBuffer.VertexBuffer };

                    var constants = new MeshPushConstants();
                  //  constants.render_matrix = projectionMatrix * viewMatrix * mesh.Value.transformMatrix;
                    constants.model =  mesh.Value.transformMatrix;

                    IntPtr unmanagedAddr = Marshal.AllocHGlobal(Marshal.SizeOf(constants));
                    Marshal.StructureToPtr(constants, unmanagedAddr, true);

               //     var cameraConstants = new CameraData();

                    IntPtr unmanagedAddrCamera = Marshal.AllocHGlobal(Marshal.SizeOf(activeCameraData));
                    Marshal.StructureToPtr(activeCameraData, unmanagedAddrCamera, true);

                    var pushSizeCamera = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(CameraData));
                    var pushSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshPushConstants));


                    if(lastPipe == 0 || lastPipe != material.pipeline.NativeHandle.Handle)
                    {
                        instance.Api.CmdBindPipeline(commandBuffers.Buffer, PipelineBindPoint.Graphics, material.pipeline.NativeHandle);
                        lastPipe = material.pipeline.NativeHandle.Handle;
                    }

                    instance.Api.CmdPushConstants(commandBuffers.Buffer, material.pipeline.NativeLayoutHandle, ShaderStageFlags.ShaderStageVertexBit, 0, pushSizeCamera, unmanagedAddrCamera.ToPointer());
                    instance.Api.CmdPushConstants(commandBuffers.Buffer, material.pipeline.NativeLayoutHandle, ShaderStageFlags.ShaderStageVertexBit, pushSizeCamera, pushSize, unmanagedAddr.ToPointer());

                    //                _vk.CmdBindIndexBuffer(commandBuffer, frameRenderBuffer.IndexBuffer, 0, sizeof(ushort) == 2 ? IndexType.Uint16 : IndexType.Uint32);

                    instance.Api.CmdSetScissor(commandBuffers.Buffer, 0, 1, scissor);
                    instance.Api.CmdBindVertexBuffers(commandBuffers.Buffer, 0, 1, vertex_buffers, (ulong*)Unsafe.AsPointer(ref vertex_offset));
                    instance.Api.CmdDraw(commandBuffers.Buffer, (uint)surface.Value.vertices.Length, 1, 0, 0);
                }
            }
        }

        public override void Draw(double delta)
        {
            queue.WaitFor();

            //create material pipelines
            foreach (var mat in Materials.Where(df => df.Value.isDirty).ToArray())
            {
                var newMaterial = mat.Value;

                if (newMaterial.pipeline.IsCreated)
                {
                    this.Destroy();
                }

                newMaterial.pipeline.Instanciate(mat.Value.parameters, instance, renderPass);
                newMaterial.isDirty = false;

                Materials[mat.Key] = newMaterial;
            }

            //upload surfaces in case they are not! -> can be move to async
            var renderables = Renderables.Where(df => df.Value.surfaces.Count(df => df.Value.isDirty) > 0).ToArray();
            foreach (var obj in renderables)
            {
                var meshObj = obj.Value;
                var surfaces = obj.Value.surfaces.Where(df => df.Value.isDirty).ToArray();
                foreach(var surf in surfaces)
                {
                    Logger.Debug(this, "Upload data " + obj.Key + " surface " + surf.Key );

                    var surface = surf.Value;
                    surface.UploadMesh(instance, logicalDevice);
                    surface.isDirty = false;

                    Renderables[obj.Key].surfaces[surf.Key] = surface;
                }
            }

            //do some draw stuff
            uint imageIndex;
            Result result = queue.AquireNextImages(out imageIndex);

            if (result == Result.ErrorOutOfDateKhr)
            {
                result = Result.ErrorOutOfDateKhr;
            }
            else if (result != Result.Success && result != Result.SuboptimalKhr)
            {
                throw new Exception("failed to acquire swap chain image!");
            }

            queue.BeginCommandBuffer(commandBuffers);
            queue.BeginRenderPass(commandBuffers, frameBuffers, renderPass, imageIndex);

            DrawMeshes();

            queue.EndRenderPass(commandBuffers);

            queue.EndCommandBuffer(commandBuffers);
            queue.QueueSubmit(commandBuffers);

            result = queue.QueuePresent(imageIndex);

            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || _framebufferResized)
            {
                _framebufferResized = false;
                result = Result.ErrorOutOfDateKhr;
            }
            else if (result != Result.Success)
            {
                throw new Exception("failed to present swap chain image!");
            }

            _currentFrame++;

            if (result == Result.ErrorOutOfDateKhr)
            {
                RecreateSwapChain();
            }
        }

        private void CleanupSwapchain()
        {
            swapChain.ColorTexture.Destroy();
            swapChain.DepthTexture.Destroy();
            frameBuffers.Destroy();
            commandBuffers.Destroy();
            foreach (var mat in Materials)
            {
                mat.Value.pipeline.Destroy();
            }
            renderPass.Destroy();
            swapChain.Destroy();
        }

        public override void Destroy()
        {
            foreach (var renderable in Renderables)
            {
            }

            logicalDevice.WaitFor();
            CleanupSwapchain();
            queue.Destroy();
            commandPool.Destroy();
            logicalDevice.Destroy();

            debugger.Destroy();
            instance.Destroy();
        }


    }
}
