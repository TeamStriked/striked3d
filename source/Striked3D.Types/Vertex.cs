using Silk.NET.Maths;
using System.Runtime.InteropServices;
using Veldrid;

namespace Striked3D.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public Vector3D<float> Position { get; set; }
        public Vector3D<float> Tangent { get; set; }
        public Vector3D<float> Normal { get; set; }
        public RgbaFloat Color { get; set; }
        public Vector2D<float> Uv1 { get; set; }
        public Vector2D<float> Uv2 { get; set; }

        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));
        }

        public static VertexLayoutDescription GetLayout()
        {
            return new VertexLayoutDescription
             (
                 new VertexElementDescription
                     ("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                 new VertexElementDescription
                     ("Tangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                 new VertexElementDescription
                     ("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                 new VertexElementDescription
                     ("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                 new VertexElementDescription
                     ("Uv1", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                 new VertexElementDescription
                     ("Uv2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
             );
        }
    }
}
