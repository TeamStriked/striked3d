using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core.Window
{
    public interface IWindow
    {
        public Veldrid.GraphicsDevice CreateDevice();
        public IInputContext CreateInput();
        public ServiceRegistry Services { get; }
        public bool VSync { get; set; }
        public Vector2D<int> Size { get; set; }
        public WindowState State { get; set; }
        public Vector2D<int> Position { get; set; }
        public string Title { get; set; }

        public delegate void OnResizeHandler(Vector2D<int> newSize);
        public event OnResizeHandler OnResize;

        public delegate void OnLoadHandler();
        public event OnLoadHandler OnLoad;
    }
}
