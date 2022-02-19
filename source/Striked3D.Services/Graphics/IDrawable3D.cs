using Striked3D.Core;

namespace Striked3D.Graphics
{
    public interface IDrawable3D : IDrawable
    {
        public void OnDraw3D(IRenderer renderer);
    }
}
