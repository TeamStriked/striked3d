using Striked3D.Types;
using System.Runtime.InteropServices;
namespace Striked3D.Core
{
    public interface ICamera : INode
    {
        public CameraInfo CameraInfo { get; }
        public void UpdateCamera();
    }
}
