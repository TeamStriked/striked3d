using Striked3D.Math;
using Striked3D.Types;
using System.Runtime.InteropServices;
using Veldrid;

namespace Striked3D.Engine.Resources
{

    [StructLayout(LayoutKind.Sequential)]
    public struct Material2DInfo
    {
        public RgbaFloat color { get; set; }
        public Vector2D<float> position { get; set; }
        public Vector2D<float> size { get; set; }
        public Vector4D<float> FontRegion { get; set; }
        public float IsFont { get; set; }
        public float FontRange { get; set; }
        public float Pad1 { get; set; }
        public float Pad2 { get; set; }

        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Material2DInfo));
        }
    }
}
