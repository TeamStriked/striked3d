
using System.Runtime.InteropServices;

namespace Striked3D.Resources
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CanvasInfo
    {
        public Striked3D.Types.Vector4D<float> screenResolution { get; set; }
        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(CanvasInfo));
        }
    }

}
