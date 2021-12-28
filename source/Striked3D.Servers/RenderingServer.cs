using Striked3D.Core;
using Striked3D.Servers.Rendering;
using System;

namespace Striked3D.Servers
{
 
    public partial class RenderingServer  : Server
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
                return ServerType.SyncRenderService;
            }
        }
        public RenderingServer() : base(new RenderingServerThread())
        {
        }

        protected override void Deregister()
        {
            Logger.Debug(this, "Thread is on cleanup..");
            var cc = this.commandClass as RenderingServerThread;
            cc.Destroy();
        }

        protected override void Loop(double delta)
        {
            var cc = this.commandClass as RenderingServerThread;
            cc.Draw(delta);
        }

        protected override void Register()
        {
            var cc = this.commandClass as RenderingServerThread;
            cc.Initialize(this.serverWindow);
        }
    }
}
