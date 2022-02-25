using System.Runtime.InteropServices;

namespace Striked3D.Resources
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Transform3DInfo
    {
        public Striked3D.Types.Vector4D<float> modelMatrix1 { get; set; }
        public Striked3D.Types.Vector4D<float> modelMatrix2 { get; set; }
        public Striked3D.Types.Vector4D<float> modelMatrix3 { get; set; }
        public Striked3D.Types.Vector4D<float> modelMatrix4 { get; set; }

        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Transform3DInfo));
        }
    }

}
