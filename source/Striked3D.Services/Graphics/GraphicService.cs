using Striked3D.Core.Window;
using Striked3D.Utils;
using System;
using Veldrid;
using Silk.NET.Vulkan;
using Silk.NET.Maths;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Striked3D.Resources;
using Striked3D.Core.AssetsPrimitives;
using Striked3D.Core;
using Striked3D.Graphics;

namespace Striked3D.Services
{
    public class GraphicService : IService
    {
        private  GraphicsDevice _graphicsDevice;
        private CommandList _commandList;
        private CommandList[] threadsCommands;
        private CommandList[] threadsCommands3D;
        private CommandList[] preRenderCommands;

        public FrameTimeAverager frameTimer = new(0.3);

        private ScreneTreeService treeService;
        private ServiceRegistry _registry;

        public GraphicsDevice Renderer3D => _graphicsDevice;

        public TextureSampleCount SampleCount = TextureSampleCount.Count1;

        public int maxRecordPerThread = 512;

        public int maxPossibleThreads = 6;


        public ResourceLayout Material3DLayout { get; set; }
        public ResourceLayout Material2DLayout { get; set; }
        public ResourceLayout TransformLayout { get; set; }
        public ResourceLayout FontAtlasLayout { get; set; }

        public Material2D Default2DMaterial { get; set; }
        public Material3D Default3DMaterial { get; set; }

        public TextureView DefaultTextureView { get; set; }
        public Texture DefaultTexture { get; set; }
        public ResourceSet DefaultTextureSet { get; set; }
        public DeviceBuffer indexDefaultBuffer { get; set; }
        public DeviceBuffer mat2DFontInfoBuffer { get; set; }
        public ResourceSet mat2DFontInfoSet { get; set; }

        public void Register(IWindow window)
        {

            maxPossibleThreads = Math.Max(1, Environment.ProcessorCount - 1);

            _graphicsDevice = window.CreateDevice();
            window.OnResize += Window_OnResize;

            this._registry = window.Services;
            var info = _graphicsDevice.GetVulkanInfo();

            var api = Vk.GetApi();
            var device = new PhysicalDevice();
            device.Handle = info.PhysicalDevice;

            var features = api.GetPhysicalDeviceFeature(device);
            CreateResources(window);
        }

        private void Window_OnResize(Vector2D<int> size)
        {
            _graphicsDevice.ResizeMainWindow((uint)size.X, (uint)size.Y);
        }

        public ulong GetImageHandle(Texture texture)
        {
            var info = _graphicsDevice.GetVulkanInfo();
            var imageInfo = info.GetVkImage(texture);

            return imageInfo;
        }

        public unsafe IntPtr GetProc(string name, IntPtr instanceHandle, IntPtr deviceHandle)
        {
            var api = Vk.GetApi();
            if (deviceHandle != IntPtr.Zero)
            {
                var device = new Device();
                device.Handle = deviceHandle;
                var res = api.GetDeviceProcAddr(device, name);
                return res;
            }

            var instance = new Instance();
            instance.Handle = instanceHandle;
            var res2 = api.GetInstanceProcAddr(instance, name);

            return res2;
        }

        public void Render(double delta)
        {

            frameTimer.AddTime(delta);

            if(this.treeService == null)
                this.treeService = this._registry.Get<ScreneTreeService>();


            //start to free objects
            treeService.QueueFreeAll();

            //process before draw
            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.End();

            _graphicsDevice.SubmitCommands(_commandList);

            this.RenderObjects(treeService.GetAll<IDrawable>(), delta);
            _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
        }

        private void RenderObjects(List<IDrawable> drawables, double delta)
        {
            if (drawables != null && drawables.Count > 0)
            {
                int maxRenderablesPerThread = maxRecordPerThread / maxPossibleThreads;
                var drawables2D = drawables.Where(df => df is IDrawable2D).Select(df => df as IDrawable2D).Chunkify(maxRenderablesPerThread);
                var drawables3D = drawables.Where(df => df is IDrawable3D).Select(df => df as IDrawable3D).Chunkify(maxRenderablesPerThread);
                var drawablesPreRender = drawables.Chunkify(maxRenderablesPerThread);

                Task<CommandList>[] preRenderTask = new Task<CommandList>[drawablesPreRender.Count()];
                Task<CommandList>[] renderThreadsTask3D = new Task<CommandList>[drawables3D.Count()];
                Task<CommandList>[] renderThreadsTask2D = new Task<CommandList>[drawables2D.Count()];

                int i = 0;
                foreach (var batch in drawablesPreRender)
                {
                    var commandBuffer = preRenderCommands[i];
                    preRenderTask[i++] = this.InitResources(commandBuffer, batch.ToList(), delta);
                }

                Task.WaitAll(preRenderTask);

                i = 0;
                foreach (var batch in drawables2D)
                {
                    var commandBuffer = threadsCommands[i];
                    renderThreadsTask2D[i++] = this.RenderObjects2D(commandBuffer, batch.ToList(), delta);
                }

                i = 0;
                foreach (var batch in drawables3D)
                {
                    var commandBuffer = threadsCommands3D[i];
                    renderThreadsTask3D[i++] = this.RenderObjects3D(commandBuffer, batch.ToList(), delta);
                }

                Task.WaitAll(renderThreadsTask3D);
                Task.WaitAll(renderThreadsTask2D);

                foreach (var task in renderThreadsTask3D)
                {
                    _graphicsDevice.SubmitCommands(task.Result);
                }

                foreach (var task in renderThreadsTask2D)
                {
                    _graphicsDevice.SubmitCommands(task.Result);
                }
            }
        }

        public Task<CommandList> RenderObjects2D(CommandList clist, List<IDrawable2D> drawables, double delta)
        {
            return Task.Factory.StartNew(() =>
            {
                var renderer = new Renderer(clist, this, delta);
                clist.PushDebugGroup("2D Renderer - " + Thread.CurrentThread.ManagedThreadId);
                clist.Begin();
                clist.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);

                foreach(var child in drawables)
                {
                    try
                    {
                        if (child is IViewport && child.IsVisible)
                        {
                            child.OnDraw2D(renderer);
                        }
                        else if (child.Viewport != null && child.Viewport.IsVisible && child.Viewport.Enable2D && !child.Viewport.isDirty)
                        {
                            if (child.IsVisible)
                            {
                                child.OnDraw2D(renderer);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[RenderError2D][" + Thread.CurrentThread.ManagedThreadId + "] " + ex.Message + "=> " + ex.StackTrace);
                    }
                }

                clist.End();
                clist.PopDebugGroup();

                renderer = null;

                return clist;
            });
        }

        public Task<CommandList> RenderObjects3D(CommandList clist, List<IDrawable3D> drawables, double delta)
        {
            return Task.Factory.StartNew(() =>
            {
                var renderer = new Renderer(clist, this, delta);

                clist.PushDebugGroup("3D Renderer - " + Thread.CurrentThread.ManagedThreadId);
                clist.Begin();
                clist.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);

                foreach (var child in drawables)
                {
                    try
                    {
                        if (child is IViewport && child.IsVisible)
                        {
                            child.OnDraw3D(renderer);
                        }
                        else if (child.Viewport != null && child.Viewport.IsVisible && child.Viewport.Enable3D && !child.Viewport.isDirty)
                        {
                            if (child.IsVisible)
                            {
                                child.OnDraw3D(renderer);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[RenderError3D][" + Thread.CurrentThread.ManagedThreadId + "] " + ex.Message + "=> " + ex.StackTrace);
                    }
                }

                clist.End();
                clist.PopDebugGroup();

                renderer = null;

                return clist;
            });
        }

        public Task<CommandList> InitResources(CommandList clist, List<IDrawable> drawables, double delta)
        {
            return Task.Factory.StartNew(() =>
            {
                var renderer = new Renderer(clist, this, delta);

                clist.PushDebugGroup("Resources - " + Thread.CurrentThread.ManagedThreadId);
                clist.Begin();
                clist.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);

                foreach (var child in drawables)
                {
                    try
                    {
                        if (child is IViewport && child.IsVisible)
                        {
                            child.BeforeDraw(renderer);
                        }
                        else if (child.Viewport != null && child.Viewport.IsVisible && child.Viewport.Enable3D && !child.Viewport.isDirty)
                        {
                            if (child.IsVisible)
                            {
                                child.BeforeDraw(renderer);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Resources][" + Thread.CurrentThread.ManagedThreadId + "] " + ex.Message + "=> " + ex.StackTrace);
                    }
                }

                clist.End();
                clist.PopDebugGroup();

                _graphicsDevice.SubmitCommands(clist);

                if(renderer.requiredWait)
                {
                    _graphicsDevice.WaitForIdle();
                }

                renderer = null;

                return clist;
            });
        }

        private void CreateResources(IWindow window)
        {
            var factory = _graphicsDevice.ResourceFactory;
            _commandList = factory.CreateCommandList();
            var renderer = new Renderer(_commandList, this, 0);

            threadsCommands = new CommandList[maxPossibleThreads];
            threadsCommands3D = new CommandList[maxPossibleThreads];
            preRenderCommands = new CommandList[maxPossibleThreads];

            for (int i = 0; i < maxPossibleThreads; i++)
            {
                threadsCommands[i] = factory.CreateCommandList();
                threadsCommands3D[i] = factory.CreateCommandList();
                preRenderCommands[i] = factory.CreateCommandList();
            }

            //3d sets
            Material3DLayout = this._graphicsDevice.ResourceFactory
                .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ));

            Material2DLayout = this._graphicsDevice.ResourceFactory
            .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("CanvasBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ));

            FontAtlasLayout = this._graphicsDevice.ResourceFactory
            .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("FontTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("FontTextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));

            TransformLayout = this._graphicsDevice.ResourceFactory
                .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ModalBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ));

            this.Default2DMaterial = new Material2D();
            this.Default2DMaterial.BeforeDraw(renderer);
            this.Default3DMaterial = new Material3D();
            this.Default3DMaterial.BeforeDraw(renderer);

            var buffer = new byte[4 * 4 * 4];
            var proc = new ProcessedTexture(PixelFormat.R8_G8_B8_A8_UNorm, TextureType.Texture2D, 4,4,1,1,1, buffer);
            DefaultTexture = proc.CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory, TextureUsage.Sampled);
            DefaultTextureView = _graphicsDevice.ResourceFactory.CreateTextureView(DefaultTexture);

            ResourceSetDescription resourceSetDescription = new (this.FontAtlasLayout, DefaultTextureView, _graphicsDevice.LinearSampler);
            DefaultTextureSet = _graphicsDevice.ResourceFactory.CreateResourceSet(resourceSetDescription);

            ushort[] indicies = new ushort[6] {0,1,2,2,0,3 };
            var ibDescription = new BufferDescription
            (
                (uint)(sizeof(ushort) * indicies.Length),
                BufferUsage.IndexBuffer
            );

            indexDefaultBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(ibDescription);

            _commandList.Begin();
            _commandList.UpdateBuffer(indexDefaultBuffer, 0, indicies.ToArray());
            _commandList.End();

            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.WaitForIdle();
        }

        public void Unregister()
        {
            foreach (var clist in threadsCommands)
            {
                clist.Dispose();
            }

            foreach (var clist in threadsCommands3D)
            {
                clist.Dispose();
            }

            foreach (var clist in preRenderCommands)
            {
                clist.Dispose();
            }

            _commandList.Dispose();
            _graphicsDevice.Dispose();
        }

        public void Update(double delta)
        {
    
        }
    }
}
