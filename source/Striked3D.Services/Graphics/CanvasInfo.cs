
using System.Runtime.InteropServices;

namespace Striked3D.Resources
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CanvasInfo
    {
        public Silk.NET.Maths.Vector4D<float> screenResolution { get; set; }
        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(CanvasInfo));
        }
    }

}
