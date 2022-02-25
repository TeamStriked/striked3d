using Striked3D.Core;
using Veldrid;

namespace Striked3D.Graphics
{
    public interface IWorld : IObject
    {
        public ResourceSet ResourceSet { get; }
        public void Update(IRenderer renderer, IViewport viewport);
    }
}
