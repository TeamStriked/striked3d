using Silk.NET.Input;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Striked3D.Graphics;
using Striked3D.Nodes;
using Striked3D.Math;
using Striked3D.Platform;
using Striked3D.Services;
using Striked3D.Types;
using System;
using System.Diagnostics;
using WindowSilk = Silk.NET.Windowing.Window;
using WindowSilkHandle = Silk.NET.Windowing.IWindow;

namespace Striked3D.Core.Window
{
    /// <summary>
    /// The root window or first window of the application
    /// </summary>
    public class RootWindow : IWindow, IDisposable
    {
        private readonly ServiceRegistry _serviceRegistry;
        public FrameTimeAverager frameTimer = new FrameTimeAverager(0.3);

        private readonly WindowSilkHandle _nativeWindow;

        public event IWindow.OnResizeHandler OnResize;
        public event IWindow.OnLoadHandler OnLoad;

        public Viewport RootViewport { get; } = new Viewport();

        public bool IsDebug { get; set; }

        public Veldrid.GraphicsDevice? CreateDevice()
        {
            return _nativeWindow?.CreateGraphicsDevice(new()
            {
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
                SyncToVerticalBlank = VSync,
                ResourceBindingModel = Veldrid.ResourceBindingModel.Improved,
                Debug = IsDebug,
                SwapchainDepthFormat = Veldrid.PixelFormat.R16_UNorm,
                SwapchainSrgbFormat = true,

            }, PlatformInfo.preferredBackend);
        }

        public IInputContext CreateInput()
        {
            return _nativeWindow?.CreateInput();
        }

        /// <inheritdoc />
        public ServiceRegistry Services => _serviceRegistry;

        public bool VSync
        {
            get => _nativeWindow.VSync;
            set => _nativeWindow.VSync = value;
        }

        public Vector2D<int> Size
        {
            get => new Vector2D<int>(_nativeWindow.Size.X, _nativeWindow.Size.Y);
            set => _nativeWindow.Size = new Silk.NET.Maths.Vector2D<int>(_nativeWindow.Size.X, _nativeWindow.Size.Y);
        }

        public WindowState State
        {
            get => _nativeWindow.WindowState;
            set => _nativeWindow.WindowState = value;
        }

        public Vector2D<int> Position
        {
            get => new Vector2D<int>(_nativeWindow.Position.X, _nativeWindow.Position.Y);
            set => _nativeWindow.Position = new Silk.NET.Maths.Vector2D<int>(_nativeWindow.Position.X, _nativeWindow.Position.Y);
        }

        public string Title
        {
            get => _nativeWindow.Title;
            set => _nativeWindow.Title = value;
        }

        public RootWindow()
        {
            _serviceRegistry = new ServiceRegistry();

            WindowOptions opts = WindowOptions.Default;
            opts.Position = new Silk.NET.Maths.Vector2D<int>(0, 0);
            opts.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);

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

        private void _OnResize(Silk.NET.Maths.Vector2D<int> obj)
        {
            RootViewport.Size = new Vector2D<float>(obj.X, obj.Y);
            OnResize?.Invoke(new Vector2D<int>(obj.X, obj.Y));
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

            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach (IService service in _serviceRegistry.All)
            {
                service.Update(delta);
            }
            watch.Stop();

            updateTime = watch.Elapsed.TotalMilliseconds;

            ScreneTreeService? nodeService = Services.Get<ScreneTreeService>();

            if (totalDelta > 1.0)
            {
                int objects = nodeService.TotalChilds();
                string nt = frameTimer.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + frameTimer.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms");
                Console.WriteLine(nt + " - Update time: " + updateTime + " - Render time " + renderTime + " - objects: " + objects);
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

            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach (IService service in _serviceRegistry.All)
            {
                service.Render(delta);
            }
            watch.Stop();

            renderTime = watch.Elapsed.TotalMilliseconds;

        }

        private void _OnLoad()
        {
            RootViewport.Size = new Vector2D<float>(Size.X, Size.Y);
            foreach (IService service in _serviceRegistry.All)
            {
                service.Register(this);
            }

            ScreneTreeService sceneTree = Services.Get<ScreneTreeService>();
            sceneTree?.AddNode(RootViewport);
            OnLoad();
        }


        public void Dispose()
        {
            foreach (IService service in _serviceRegistry.All)
            {
                service.Unregister();
            }

            _nativeWindow.Dispose();
        }
    }
}
