using Silk.NET.Input;
using Silk.NET.Windowing;
using Striked3D.Math;
using Striked3D.Types;

namespace Striked3D.Core.Window
{
    /// <summary>
    /// The window interface
    /// </summary>
    public interface IWindow
    {
        public Veldrid.GraphicsDevice CreateDevice();
        public IInputContext CreateInput();

        /// <summary>
        /// The service registry which holds the registered services of the window.
        /// </summary>
        /// <value></value>
        public ServiceRegistry Services { get; }

        /// <summary>
        /// Activate or Deactivate VSync
        /// </summary>
        /// <value>True or false</value>
        public bool VSync { get; set; }

        public Vector2D<int> Size { get; set; }
        public WindowState State { get; set; }

        /// <summary>
        /// Set or get the position of the window
        /// </summary>
        /// <value>Required X and Y position from the screen</value>
        public Vector2D<int> Position { get; set; }

        /// <summary>
        /// Set or get the window title
        /// </summary>
        /// <value>Required string</value>
        public string Title { get; set; }

        public delegate void OnResizeHandler(Vector2D<int> newSize);
        public event OnResizeHandler OnResize;

        public delegate void OnLoadHandler();
        public event OnLoadHandler OnLoad;
    }
}
