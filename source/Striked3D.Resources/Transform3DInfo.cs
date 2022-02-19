using System.Runtime.InteropServices;

namespace Striked3D.Resources
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Transform3DInfo
    {
        public Silk.NET.Maths.Vector4D<float> modelMatrix1 { get; set; }
        public Silk.NET.Maths.Vector4D<float> modelMatrix2 { get; set; }
        public Silk.NET.Maths.Vector4D<float> modelMatrix3 { get; set; }
        public Silk.NET.Maths.Vector4D<float> modelMatrix4 { get; set; }

        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Transform3DInfo));
        }
    }

}
