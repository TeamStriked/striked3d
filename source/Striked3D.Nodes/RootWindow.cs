using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Striked3D.Core.Graphics;
using Striked3D.Graphics;
using Striked3D.Nodes;
using Striked3D.Platform;
using Striked3D.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WindowSilk = Silk.NET.Windowing.Window;
using WindowSilkHandle = Silk.NET.Windowing.IWindow;

namespace Striked3D.Core.Window
{
    public class RootWindow : IWindow, IDisposable
    {
        private ServiceRegistry _serviceRegistry;
        public FrameTimeAverager frameTimer = new FrameTimeAverager(0.3);

        private readonly WindowSilkHandle _nativeWindow;

        public event IWindow.OnResizeHandler OnResize;
        public event IWindow.OnLoadHandler OnLoad;

        private Viewport _rootViewport = new Viewport();
        public Viewport RootViewport { get { return _rootViewport; } }

        public Veldrid.GraphicsDevice CreateDevice()
        {

#if (DEBUG)
           var debugMode = true;
#elif (RELEASE)
           var debugMode = false;
#endif
            if (debugMode)
                Console.WriteLine("Debug Mode enabled");

            return _nativeWindow?.CreateGraphicsDevice(new()
            {
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
                SyncToVerticalBlank = this.VSync,
                ResourceBindingModel = Veldrid.ResourceBindingModel.Improved,
                Debug = debugMode,
                SwapchainSrgbFormat = true,

            }, PlatformInfo.preferredBackend);
        }

        public IInputContext CreateInput()
        {
            return _nativeWindow?.CreateInput();
        }

        public ServiceRegistry Services
        {
            get { return _serviceRegistry; }
        }

        public bool VSync
        {
            get => _nativeWindow.VSync;
            set => _nativeWindow.VSync = value;
        }

        public Vector2D<int> Size
        {
            get => _nativeWindow.Size;
            set => _nativeWindow.Size = value;
        }

        public WindowState State
        {
            get => _nativeWindow.WindowState;
            set => _nativeWindow.WindowState = value;
        }

        public Vector2D<int> Position
        {
            get => _nativeWindow.Position;
            set => _nativeWindow.Position = value;
        }

        public string Title
        {
            get => _nativeWindow.Title;
            set => _nativeWindow.Title = value;
        }

        public RootWindow()
        {
            _serviceRegistry = new ServiceRegistry();

            var opts = WindowOptions.Default;
            opts.Position = new Vector2D<int>(0, 0);
            opts.Size = new Vector2D<int>(800, 600);

            opts.Title = "Window";
            opts.API = Platform.PlatformInfo.preferredBackend.ToGraphicsAPI();
            opts.VSync = false;

            Silk.NET.Windowing.Glfw.GlfwWindowing.Use();
            Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform();

            _nativeWindow = WindowSilk.Create(opts);
            _nativeWindow.Render += _OnRenderFrame;
            _nativeWindow.Update += _OnFrame;
            _nativeWindow.Load += _OnLoad;
            _nativeWindow.Closing += Dispose;
            _nativeWindow.Resize += _OnResize;
            _nativeWindow.VSync = false;
        }

        private void _OnResize(Vector2D<int> obj)
        {
            this.RootViewport.Size = new Vector2D<float>(this.Size.X, this.Size.Y);
            this.OnResize?.Invoke(obj);
        }

        public void Run()
        {
            _nativeWindow.Run();
        }

        private double totalDelta = 0;
        private double updateTime = 0;
        private double renderTime = 0;

        private void _OnFrame(double delta)
        {

            var watch = new Stopwatch();
            watch.Start();
            foreach (var service in _serviceRegistry.All)
            {
                service.Update(delta);
            }
            watch.Stop();

            this.updateTime = watch.Elapsed.TotalMilliseconds;

            if (totalDelta > 1.0)
            {
                var nt = frameTimer.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + frameTimer.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms");
                Console.WriteLine(nt + " - Update time: " + updateTime + " - Render time " + renderTime);
                totalDelta = 0;
            }
            else
            {
                totalDelta += delta;
            }

        }

        private void _OnRenderFrame(double delta)
        {
            frameTimer.AddTime(delta);

            var watch = new Stopwatch();
            watch.Start();
            foreach (var service in _serviceRegistry.All)
            {
                service.Render(delta);
            }
            watch.Stop();

            this.renderTime = watch.Elapsed.TotalMilliseconds;

        }

        private void _OnLoad()
        {
            this.RootViewport.Size = new Vector2D<float>(this.Size.X, this.Size.Y);
            foreach (var service in _serviceRegistry.All)
            {
                service.Register(this);
            }

            var sceneTree = this.Services.Get<ScreneTreeService>();
            sceneTree?.AddNode(_rootViewport);
            this.OnLoad();
        }


        public void Dispose()
        {
            foreach(var service in _serviceRegistry.All)
            {
                service.Unregister();
            }

            _nativeWindow.Dispose();
        }
    }
}
