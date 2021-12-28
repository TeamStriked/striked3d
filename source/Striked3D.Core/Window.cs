using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Text;
using SilkWindow = Silk.NET.Windowing.Window;
using SilkIWindow = Silk.NET.Windowing.IWindow;
using System.Linq;
using Striked3D.Types;

namespace Striked3D.Core
{
    public class Window
    {
        private SilkIWindow _window { get; set; }

        public Dictionary<Type, Server> Servers = new Dictionary<Type, Server>();

        private bool windowIsLoaded = false;
        public Queue<EngineCompleteTask> tasksCompleted = new Queue<EngineCompleteTask>();

        public delegate void InputEventHandler();
        public event InputEventHandler OnLoad;
        public NodeTree Tree  { get;set; }

        public Window(string title, Types.Vector2D<int> size)
        {
            var options = WindowOptions.DefaultVulkan;
            options.Size = new Silk.NET.Maths.Vector2D<int>(size.X, size.Y);
            options.Title = "LearnOpenGL with Silk.NET";
            _window = SilkWindow.Create(options);

            Tree = new NodeTree(this);

            _window.Load += RunServices;
            _window.Render += LoopServices;
            _window.Closing += Reset;
            _window.Update += UpdateServices;
        }

        public Vector2D<int> Size { 
            get
            {
                return new Vector2D<int>(_window.Size.X, _window.Size.Y);
            } 
        }

        public SilkIWindow NativeWindow
        {
            get {
                return _window; 
            } 
        }

        public bool IsRunning = true;
        public void Init()
        {
            Logger.Debug(this, "Window is init..");
            _window.Initialize(); 
            if (_window?.VkSurface is null)
            {
                throw new NotSupportedException("Windowing platform doesn't support Vulkan.");
            }

            OnLoad?.Invoke();
        }

        public void Step()
        {
            _window.DoEvents();
            _window.DoUpdate();
            _window.DoRender();
        }

        public void Reset()
        {
            IsRunning = false;
            Logger.Debug(this, "Window is resetted..");
            CloseServices();
            _window.Reset();
            _window.Dispose();
        }

        private void RunServices()
        {
            windowIsLoaded = true;
            Logger.Debug(this, "Window is loaded with " + Servers.Values.Count + " services.");
            foreach (var server in Servers.Values)
            {
                server.Initialize(this);
            }
        }

        private void LoopServices(double delta)
        {
            Tree.ForwardRender(delta);

            foreach (var server in Servers.Values.Where(df => df.RunType == ServerType.SyncRenderService))
            {
                server.Sync(delta);
                server.FinishEvent.WaitOne();
            }
        }

        private void CloseServices()
        {
            Logger.Debug(this, "Window is closed..");
            foreach (var server in Servers.Values)
            {
                server.Stop();
                server.Sync(0);
            }
        }


        public void AddCompleteTask(EngineCompleteTask task)
        {
            tasksCompleted.Enqueue(task);
        }

        private void UpdateServices(double delta)
        {
            //resolve completed server tasks
            while(tasksCompleted.Count > 0)
            {
                var command = tasksCompleted.Dequeue();

                if (command.completedTask.callback != null)
                {
                    command.completedTask.callback(command.result);
                }
            }

            _window.Title = "FPS " + System.Math.Ceiling(1d / delta);

            foreach (var server in Servers.Values.Where(df => df.RunType == ServerType.SyncService))
            {
                server.Sync(delta);
                server.FinishEvent.WaitOne();
            }

            Tree.ForwardUpdate(delta);
        }

        public T RegisterService<T>() where T : Server
        {
            var newService = Activator.CreateInstance<T>();
            Servers.Add(typeof(T), newService);

            if (windowIsLoaded)
            {
                newService.Initialize(this);
            }

            return newService;
        }

        public T GetService<T>() where T : Server
        {
            if (Servers.ContainsKey(typeof(T)))
            {
                return Servers[typeof(T)] as T;
            }
            else
            {
                return null;
            }
        }
    }
}
