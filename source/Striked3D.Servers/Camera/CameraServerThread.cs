using Striked3D.Core;
using Striked3D.Servers.Camera;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;


namespace Striked3D.Servers
{
    [ApiDeclaration("CameraServer")]
    internal class CameraServerThread : ServerThreadRunner
    {
        protected Window _win;
        public Dictionary<Guid, CameraData> Cameras = new Dictionary<Guid, CameraData>();

        public void Initialize(Window win)
        {
            _win = win;
        }

        [ApiDeclarationMethod]
        public Guid CreateCamera()
        {
            var id = Guid.NewGuid();
            var data = new CameraData();
            Cameras.Add(id, data);

            return id;
        }

        private float DegreesToRadians(float degrees)
        {
            return (float)(Math.PI / 180f) * degrees;
        }

        [ApiDeclarationMethod]
        public void SetPerspective(Guid camId, float fov, float aspect, float near, float far)
        {
            if (!Cameras.ContainsKey(camId))
            {
                throw new Exception("Cant find material");
            }

            var camera = Cameras[camId];
            camera.near = near;
            camera.far = far;
            camera.projection = Types.Matrix4X4.CreatePerspectiveFieldOfView(DegreesToRadians(fov), aspect, near, far);

            Cameras[camId] = camera;
        }

        [ApiDeclarationMethod]
        public void SetView(Guid camId, Vector3D<float> position, Vector3D<float> front, Vector3D<float> up )
        {
            if (!Cameras.ContainsKey(camId))
            {
                throw new Exception("Cant find material");
            }

            var camera = Cameras[camId];
            camera.view = Types.Matrix4X4.CreateLookAt(position, position + front, up);
            Cameras[camId] = camera;
        }
    }
}
