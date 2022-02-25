using Striked3D.Core;

namespace Striked3D.Graphics
{
    /// <summary>
    /// The interface for allowing 2d rendering tasks for the given node
    /// </summary>
    public interface IDrawable2D : IDrawable
    {
        /// <summary>
        /// Inject the 2d rendering tasks
        /// </summary>
        /// <param name="renderer"></param>
        public void OnDraw2D(IRenderer renderer);
    }
}
