using Striked3D.Core;

namespace Striked3D.Graphics
{
    /// <summary>
    /// The core interface for allow pre-rendering tasks on nodes
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// The given viewport for the node
        /// </summary>
        public IViewport Viewport { get; }

        /// <summary>
        /// Is node is visible (renderer) or not
        /// </summary>
        [Export]
        public bool IsVisible { get; set; }

        /// <summary>
        /// The method which is called from the graphics service, to do pre-rendering operations (like creating buffers, or textures)
        /// </summary>
        /// <param name="renderer"></param>
        public void BeforeDraw(IRenderer renderer);
    }
}
