using Striked3D.Core;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Striked3D.Servers
{
    public static class WindowServer
    {
        private static Dictionary<Guid, Window> All = new Dictionary<Guid, Window>();
        public static Window createWindow( string title, Vector2D<int> size)
        {
            var window = new Window(title, size);
            var id = Guid.NewGuid();
            All.Add(Guid.NewGuid(), window);

            Thread thread1 = new Thread(() => StartWindow(window));
            thread1.Start();

            return window;
        }

        private static void StartWindow(Window _window)
        {
            _window.Init();
            while (_window.IsRunning)
            {
                _window.Step();
            }
            _window.Reset();
        }

        public static void CloseAll()
        {
            foreach(var window in All.Values)
            {
                window.IsRunning = false;
            }
        }
    }
}
