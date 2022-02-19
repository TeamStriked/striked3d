using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Pipeline;
using Striked3D.Resources;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Nodes
{
    public class EditorGrid : VisualInstance3D
    {
        public GridMaterial3D Material = new ();
        
        private DeviceBuffer deviceBufferVertex;
        private readonly Vertex[] vertices;

        public EditorGrid() : base()
        {
            vertices = new Vertex[6];
            vertices[0] = new Vertex { Position = new Silk.NET.Maths.Vector3D<float>(1, 1, 0) };
            vertices[1] = new Vertex { Position = new Silk.NET.Maths.Vector3D<float>(-1, -1, 0) };
            vertices[2] = new Vertex { Position = new Silk.NET.Maths.Vector3D<float>(-1, 1, 0) };
            vertices[3] = new Vertex { Position = new Silk.NET.Maths.Vector3D<float>(-1, -1, 0) };
            vertices[4] = new Vertex { Position = new Silk.NET.Maths.Vector3D<float>(1, 1, 0) };
            vertices[5] = new Vertex { Position = new Silk.NET.Maths.Vector3D<float>(1, -1, 0) };

            this._isDirty = true;
        }

        public override void BeforeDraw(IRenderer renderer)
        {
            if (Viewport != null && Viewport.World3D != null && this.Transform != null)
            {
                if (this._isDirty)
                {
                    this.Transform.BeforeDraw(renderer);
                    this.Material?.BeforeDraw(renderer);

                    uint size = (uint)vertices.Length * Vertex.GetSizeInBytes();
                    var vbDescription = new BufferDescription
                    (
                        size,
                        BufferUsage.VertexBuffer
                    );

                    deviceBufferVertex = renderer.CreateBuffer(vbDescription);
                    renderer.UpdateBuffer(deviceBufferVertex, 0, vertices);

                    this._isDirty = false;
                }
            }
        }

        public override void OnDraw3D(IRenderer renderer)
        {
            if (Material != null && !Material.isDirty)
            {
                this.Transform?.OnDraw3D(renderer);

                renderer.SetViewport(Viewport);
                renderer.SetMaterial(Material);
                renderer.SetResourceSets(new ResourceSet[] { Viewport.World3D.ResourceSet });
                renderer.BindBuffers(deviceBufferVertex);

                renderer.DrawInstanced(this.vertices.Length);
            }
        }
    }
}
