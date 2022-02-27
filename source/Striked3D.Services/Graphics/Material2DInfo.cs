using Striked3D.Math;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;

namespace Striked3D.Services.Graphics
{

    [StructLayout(LayoutKind.Sequential)]
    public struct Material2DInfo
    {
        public RgbaFloat color { get; set; }
        public Vector2D<float> position { get; set; }
        public Vector2D<float> size { get; set; }
        public Vector4D<float> FontRegion { get; set; }
        public Vector4D<float> Modulate { get; set; }
        public float IsFont { get; set; }
        public float FontRange { get; set; }
        public float UseTexture { get; set; }
        public float Pad2 { get; set; }


        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Material2DInfo));
        }
    }
}
