using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Types
{
    public struct MeshSurfaceLodBlock
    {
        public int IndexOffset = 0;

        public int VertexOffset = 0;
        public int Indicies { get; set; }
        public int Vertices { get; set; }

    }
    public struct MeshSurface
    {
        public Dictionary<int, MeshSurfaceLodBlock> LodBlocks = new Dictionary<int, MeshSurfaceLodBlock>();
        public Vertex[] Vertices { get; set; }
        public ushort[] Indicies { get; set; }

    }
}
