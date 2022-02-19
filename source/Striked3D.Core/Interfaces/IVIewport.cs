using Silk.NET.Maths;

namespace Striked3D.Core
{
    public interface IViewport : INode
    {
        public bool Enable3D { get; }
        public bool Enable2D { get; }
        public bool isDirty { get; }
        public bool IsVisible { get; }

        public Vector2D<float> Size { get; set; }
        public Vector2D<float> Position { get; set; }
        public ICamera ActiveCamera { get; set; }

        public IWorld World3D { get; }
        public IWorld World2D { get; }

        public bool IsMouseInside();
    }
}
