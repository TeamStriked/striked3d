using Striked3D.Core;

namespace Striked3D.Graphics
{
    public interface IDrawable2D : IDrawable
    {
        public void OnDraw2D(IRenderer renderer);
    }
}
