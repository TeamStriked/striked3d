using Striked3D.Core;
using System;
using System.Numerics;
using System.Linq;

namespace Striked3D.Servers
{
    public partial class CameraServer : Server
    {
        public override int Priority
        {
            get
            {
                return 0;
            }
        }

        public override ServerType RunType
        {
            get
            {
                return ServerType.SyncService;
            }
        }

        public CameraServer() : base(new CameraServerThread())
        {

        }
        protected override void Deregister()
        {
            Logger.Debug(this, "Deregister camera server");
        }

        public Vector2 workSize = Vector2.Zero;
        public Vector2 workPositon = Vector2.Zero;

        protected override void Loop(double delta)
        {
            var cc = commandClass as CameraServerThread;
        }

        public  Camera.CameraData GetActiveCamera()
        {
            var cc = commandClass as CameraServerThread;
            return cc.Cameras.Values.FirstOrDefault();
        }


        protected override void Register()
        {
            var cc = commandClass as CameraServerThread;
            cc.Initialize(serverWindow);

            Logger.Debug(this, "Register camera server");
        }
    }
}
