using Striked3D.Core.Window;

namespace Striked3D.Core
{
    public interface INode : IObject
    {
        public IWindow Root { get; set; }
        public string Name { get; }
        public IViewport Viewport { get; }

        public void Update(double delta);
        public void OnEnterTree();
        public void OnLeaveTree();
        public void OnEnterTreeReady();

    }
}
