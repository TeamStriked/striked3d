using Silk.NET.Vulkan;
using Striked3D.Core;
using Striked3D.Core.AssetsPrimitives;
using Striked3D.Core.Window;
using Striked3D.Graphics;
using Striked3D.Resources;
using Striked3D.Types;
using Striked3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Services
{
    public class GraphicService : IService
    {
        private GraphicsDevice _graphicsDevice;
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

            _registry = window.Services;
            BackendInfoVulkan info = _graphicsDevice.GetVulkanInfo();

            Vk api = Vk.GetApi();
            PhysicalDevice device = new PhysicalDevice
            {
                Handle = info.PhysicalDevice
            };

            PhysicalDeviceFeatures features = api.GetPhysicalDeviceFeature(device);
            CreateResources(window);
        }

        private void Window_OnResize(Vector2D<int> size)
        {
            _graphicsDevice.ResizeMainWindow((uint)size.X, (uint)size.Y);
        }

        public ulong GetImageHandle(Texture texture)
        {
            BackendInfoVulkan info = _graphicsDevice.GetVulkanInfo();
            ulong imageInfo = info.GetVkImage(texture);

            return imageInfo;
        }

        public unsafe IntPtr GetProc(string name, IntPtr instanceHandle, IntPtr deviceHandle)
        {
            Vk api = Vk.GetApi();
            if (deviceHandle != IntPtr.Zero)
            {
                Device device = new Device
                {
                    Handle = deviceHandle
                };
                Silk.NET.Core.PfnVoidFunction res = api.GetDeviceProcAddr(device, name);
                return res;
            }

            Instance instance = new Instance
            {
                Handle = instanceHandle
            };

            Silk.NET.Core.PfnVoidFunction res2 = api.GetInstanceProcAddr(instance, name);

            return res2;
        }

        public void Render(double delta)
        {
            frameTimer.AddTime(delta);

            if (treeService == null)
            {
                treeService = _registry.Get<ScreneTreeService>();
            }

            treeService.QueueFreeAll();


            RenderThreads(treeService.GetAll<IDrawable>(), delta);
            _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
        }

        private void RenderThreads(List<IDrawable> drawables, double delta)
        {

            bool requiredWait = false;
            IEnumerable<IDrawable>? visibles = drawables.Where(df => df.IsVisible);

            if (drawables != null && drawables.Count > 0)
            {
                int maxRenderablesPerThread = maxRecordPerThread / maxPossibleThreads;

                IEnumerable<IEnumerable<IDrawable>> drawablesPreRender = visibles.Chunkify(maxRenderablesPerThread);

                Task<bool>[] preRenderTask = new Task<bool>[drawablesPreRender.Count()];

                int i = 0;
                foreach (IEnumerable<IDrawable> batch in drawablesPreRender)
                {
                    CommandList commandBuffer = preRenderCommands[i];
                    preRenderTask[i++] = InitResources(commandBuffer, batch.ToList(), delta);
                }

                Task.WaitAll(preRenderTask);

                foreach (Task<bool> task in preRenderTask)
                {
                    if (task.Result == true)
                    {
                        requiredWait = true;
                    }
                }

                /*
                foreach (Task<CommandList> task in renderThreadsTask3D)
                {
                    _graphicsDevice.SubmitCommands(task.Result);
                }

                foreach (Task<CommandList> task in renderThreadsTask2D)
                {
                    _graphicsDevice.SubmitCommands(task.Result);
                }
                */
            }

            _commandList.PushDebugGroup("Default Renderer - " + Thread.CurrentThread.ManagedThreadId);
            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
            _commandList.ClearDepthStencil(1.0f);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.BeginWithSubpasses();

            List<CommandList>? renderCommandList = new List<CommandList>();

            if (drawables != null && drawables.Count > 0)
            {
                int maxRenderablesPerThread = maxRecordPerThread / maxPossibleThreads;

                IEnumerable<IEnumerable<IDrawable2D>> drawables2D = visibles.Where(df => df is IDrawable2D).Select(df => df as IDrawable2D).Chunkify(maxRenderablesPerThread);
                IEnumerable<IEnumerable<IDrawable3D>> drawables3D = visibles.Where(df => df is IDrawable3D).Select(df => df as IDrawable3D).Chunkify(maxRenderablesPerThread);

                Task<CommandList>[] renderThreadsTask3D = new Task<CommandList>[drawables3D.Count()];
                Task<CommandList>[] renderThreadsTask2D = new Task<CommandList>[drawables2D.Count()];

                int i = 0;
                foreach (IEnumerable<IDrawable2D> batch in drawables2D)
                {
                    CommandList commandBuffer = threadsCommands[i];
                    renderThreadsTask2D[i++] = RenderObjects2D(_commandList, commandBuffer, batch.ToList(), delta);
                    renderCommandList.Add(commandBuffer);
                }

                i = 0;
                foreach (IEnumerable<IDrawable3D> batch in drawables3D)
                {
                    CommandList commandBuffer = threadsCommands3D[i];
                    renderThreadsTask3D[i++] = RenderObjects3D(_commandList, commandBuffer, batch.ToList(), delta);
                    renderCommandList.Add(commandBuffer);
                }

                Task.WaitAll(renderThreadsTask3D);
                Task.WaitAll(renderThreadsTask2D);

                /*
                foreach (Task<CommandList> task in renderThreadsTask3D)
                {
                    _graphicsDevice.SubmitCommands(task.Result);
                }

                foreach (Task<CommandList> task in renderThreadsTask2D)
                {
                    _graphicsDevice.SubmitCommands(task.Result);
                }
                */
            }

            _commandList.EndWithSubpasses(renderCommandList.ToArray());
            _commandList.PopDebugGroup();

            _graphicsDevice.SubmitCommands(_commandList);

            if (requiredWait)
            {
                _graphicsDevice.WaitForIdle();
            }
        }

        public Task<CommandList> RenderObjects2D(CommandList mainBuffer, CommandList clist, List<IDrawable2D> drawables, double delta)
        {
            return Task.Factory.StartNew(() =>
            {
                Renderer renderer = new Renderer(clist, this, delta);

                clist.PushDebugGroup("2D Renderer - " + Thread.CurrentThread.ManagedThreadId);
                clist.BeginAsSubpass(mainBuffer);

                foreach (IDrawable2D child in drawables)
                {
                    try
                    {
                        if (child is IViewport && child.IsVisible)
                        {
                            child.OnDraw2D(renderer);
                        }
                        else if (child.Viewport != null && child.Viewport.IsVisible && child.Viewport.Enable2D && !child.Viewport.isDirty)
                        {
                            child.OnDraw2D(renderer);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[RenderError2D][" + Thread.CurrentThread.ManagedThreadId + "] " + ex.Message + "=> " + ex.StackTrace);
                    }
                }

                clist.EndAsSubpass();
                clist.PopDebugGroup();

                renderer = null;

                return clist;
            });
        }

        public Task<CommandList> RenderObjects3D(CommandList mainBuffer, CommandList clist, List<IDrawable3D> drawables, double delta)
        {
            return Task.Factory.StartNew(() =>
            {
                Renderer renderer = new Renderer(clist, this, delta);
                clist.PushDebugGroup("3D Renderer - " + Thread.CurrentThread.ManagedThreadId);
                clist.BeginAsSubpass(mainBuffer);

                foreach (IDrawable3D child in drawables)
                {
                    try
                    {
                        if (child is IViewport)
                        {
                            child.OnDraw3D(renderer);
                        }
                        else if (child.Viewport != null && child.Viewport.IsVisible && child.Viewport.Enable3D && !child.Viewport.isDirty)
                        {
                            child.OnDraw3D(renderer);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[RenderError3D][" + Thread.CurrentThread.ManagedThreadId + "] " + ex.Message + "=> " + ex.StackTrace);
                    }
                }

                clist.EndAsSubpass();
                clist.PopDebugGroup();

                renderer = null;

                return clist;
            });
        }

        public Task<bool> InitResources(CommandList clist, List<IDrawable> drawables, double delta)
        {
            return Task.Factory.StartNew(() =>
            {
                Renderer renderer = new Renderer(clist, this, delta);

                clist.PushDebugGroup("Resources - " + Thread.CurrentThread.ManagedThreadId);
                clist.Begin();

                //        clist.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);

                foreach (IDrawable child in drawables)
                {
                    try
                    {
                        if (child is IViewport)
                        {
                            child.BeforeDraw(renderer);
                        }
                        else if (child.Viewport != null && child.Viewport.IsVisible && child.Viewport.Enable3D && !child.Viewport.isDirty)
                        {
                            child.BeforeDraw(renderer);
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

                return renderer.requiredWait;
            });
        }

        private void CreateResources(IWindow window)
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            _commandList = factory.CreateCommandList();
            Renderer renderer = new Renderer(_commandList, this, 0);

            threadsCommands = new CommandList[maxPossibleThreads];
            threadsCommands3D = new CommandList[maxPossibleThreads];
            preRenderCommands = new CommandList[maxPossibleThreads];

            for (int i = 0; i < maxPossibleThreads; i++)
            {
                threadsCommands[i] = factory.CreateCommandList(new CommandListDescription
                {
                    isSubpass = true
                });
                threadsCommands3D[i] = factory.CreateCommandList(new CommandListDescription
                {
                    isSubpass = true
                });
                preRenderCommands[i] = factory.CreateCommandList();
            }

            //3d sets
            Material3DLayout = _graphicsDevice.ResourceFactory
                .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ));

            Material2DLayout = _graphicsDevice.ResourceFactory
            .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("CanvasBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ));

            FontAtlasLayout = _graphicsDevice.ResourceFactory
            .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("FontTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("FontTextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));


            TransformLayout = _graphicsDevice.ResourceFactory
                .CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ModalBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ));

            Default2DMaterial = new Material2D();
            Default2DMaterial.BeforeDraw(renderer);
            Default3DMaterial = new Material3D();
            Default3DMaterial.BeforeDraw(renderer);

            byte[] buffer = new byte[4 * 4 * 4];


            ProcessedTexture proc = new ProcessedTexture(PixelFormat.R8_G8_B8_A8_UNorm, TextureType.Texture2D, 4, 4, 1, 1, 1, buffer);
            DefaultTexture = proc.CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory, TextureUsage.Sampled);
            DefaultTextureView = _graphicsDevice.ResourceFactory.CreateTextureView(DefaultTexture);

            ResourceSetDescription resourceSetDescription = new(FontAtlasLayout, DefaultTextureView, _graphicsDevice.LinearSampler);
            DefaultTextureSet = _graphicsDevice.ResourceFactory.CreateResourceSet(resourceSetDescription);


            ushort[] indicies = new ushort[6] { 0, 1, 2, 2, 0, 3 };
            BufferDescription ibDescription = new BufferDescription
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

        public void GetStats()
        {
        }

        public void Unregister()
        {
            foreach (CommandList clist in threadsCommands)
            {
                clist.Dispose();
            }

            foreach (CommandList clist in threadsCommands3D)
            {
                clist.Dispose();
            }

            foreach (CommandList clist in preRenderCommands)
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
