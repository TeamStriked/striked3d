using Veldrid;

namespace Striked3D.Core
{
    public interface IWorld : IObject
    {
        public ResourceSet ResourceSet { get; }
        public void Update(IRenderer renderer, IViewport viewport);
    }
}
