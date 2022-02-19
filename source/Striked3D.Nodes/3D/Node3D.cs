using Striked3D.Core;
using Striked3D.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Striked3D.Nodes
{
    public abstract class Node3D : Node
    {
        private Transform3D _transform = new Transform3D();

        [Export]
        public Transform3D Transform
        {
            get { return _transform; }
            set { _transform = value; }
        }
    }
}
