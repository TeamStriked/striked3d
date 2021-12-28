using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Servers.Rendering
{
    public struct RenderViewport
    {
        public Guid parentViewport;
        public Vector2D<int> size;
        public Vector2D<int> position;
        public Guid camera;
    }
}
