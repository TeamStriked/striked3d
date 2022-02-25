using Striked3D.Core;

namespace Striked3D.Graphics
{
    /// <summary>
    /// The interface for allowing 3d rendering tasks for the given node
    /// </summary>
    public interface IDrawable3D : IDrawable
    {
        /// <summary>
        /// Inject the 3d rendering tasks
        /// </summary>
        /// <param name="renderer"></param>
        public void OnDraw3D(IRenderer renderer);
    }
}
