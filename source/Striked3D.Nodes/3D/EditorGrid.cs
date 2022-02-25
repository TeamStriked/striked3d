using Striked3D.Core;
using Striked3D.Resources;
using Striked3D.Types;
using Veldrid;

namespace Striked3D.Nodes
{
    public class EditorGrid : VisualInstance3D
    {
        public GridMaterial3D Material = new();

        private DeviceBuffer deviceBufferVertex;
        private readonly Vertex[] vertices;

        public EditorGrid() : base()
        {
            vertices = new Vertex[6];

            vertices[0] = new Vertex { Position = new Striked3D.Types.Vector3D<float>(1, 1, 0) };
            vertices[1] = new Vertex { Position = new Striked3D.Types.Vector3D<float>(-1, -1, 0) };
            vertices[2] = new Vertex { Position = new Striked3D.Types.Vector3D<float>(-1, 1, 0) };
            vertices[3] = new Vertex { Position = new Striked3D.Types.Vector3D<float>(-1, -1, 0) };
            vertices[4] = new Vertex { Position = new Striked3D.Types.Vector3D<float>(1, 1, 0) };
            vertices[5] = new Vertex { Position = new Striked3D.Types.Vector3D<float>(1, -1, 0) };

            _isDirty = true;
        }

        public override void BeforeDraw(IRenderer renderer)
        {
            if (Viewport != null && Viewport.World3D != null && Transform != null)
            {
                Transform.BeforeDraw(renderer);
                Material?.BeforeDraw(renderer);

                if (_isDirty)
                {

                    uint size = (uint)vertices.Length * Vertex.GetSizeInBytes();
                    BufferDescription vbDescription = new BufferDescription
                    (
                        size,
                        BufferUsage.VertexBuffer
                    );

                    deviceBufferVertex = renderer.CreateBuffer(vbDescription);
                    renderer.UpdateBuffer(deviceBufferVertex, 0, vertices);

                    _isDirty = false;
                }
            }
        }

        public override void OnDraw3D(IRenderer renderer)
        {
            if (Material != null && !Material.isDirty)
            {
                Transform?.OnDraw3D(renderer);

                renderer.SetViewport(Viewport);
                renderer.SetMaterial(Material);
                renderer.SetResourceSets(new ResourceSet[] { Viewport.World3D.ResourceSet });
                renderer.BindBuffers(deviceBufferVertex);

                renderer.DrawInstanced(vertices.Length);
            }
        }
    }
}
