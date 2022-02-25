using Striked3D.Types;
using System.Runtime.InteropServices;
using Veldrid;

namespace Striked3D.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex2D
    {
        public Vector2D<float> Position { get; set; }
        public RgbaFloat Color { get; set; }
        public Vector2D<float> Uv1 { get; set; }

        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex2D));
        }

        public static VertexLayoutDescription GetLayout()
        {
            return new VertexLayoutDescription
             (
           /*      new VertexElementDescription
                     ("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                 new VertexElementDescription
                     ("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                 new VertexElementDescription
                     ("Uv1", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
           */
             );
        }
    }
}
