using Striked3D.Core;

namespace Striked3D.Graphics
{
    public interface IDrawable
    {
        public IViewport Viewport { get; }

        [Export]
        public bool IsVisible { get; set; }

        public void BeforeDraw(IRenderer renderer);

    }
}
