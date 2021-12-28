using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Striked3D.Servers.Camera
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]

    public struct CameraData
    {
        public Matrix4X4<float> projection = Matrix4X4<float>.Identity;
        public Matrix4X4<float> view = Matrix4X4<float>.Identity;

        public float near = 0.01f;
        public float far = 1000f;
        public float test = 0f;
        public float test2 = 0f;
    }
}
