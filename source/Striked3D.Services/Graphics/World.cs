using Silk.NET.Maths;
using Striked3D.Graphics;
using Striked3D.Types;
using Veldrid;

namespace Striked3D.Core.Graphics
{
    public abstract class World : Object, IWorld
    {
        public Vector2D<float> Size { get; set; }
        public Vector2D<float> Position { get; set; }

        protected ResourceSet _resourceSet = null;
        public ResourceSet ResourceSet => _resourceSet;

        public virtual void Update(IRenderer renderer, IViewport viewport)
        {
        }
    }
}
