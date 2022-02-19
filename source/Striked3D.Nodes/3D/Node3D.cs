using Striked3D.Core;
using Striked3D.Resources;

namespace Striked3D.Nodes
{
    public abstract class Node3D : Node
    {
        private Transform3D _transform = new Transform3D();

        [Export]
        public Transform3D Transform
        {
            get => _transform;
            set => _transform = value;
        }
    }
}
