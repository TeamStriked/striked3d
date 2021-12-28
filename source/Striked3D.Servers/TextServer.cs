using Striked3D.Core;
using System;
using System.Numerics;

namespace Striked3D.Servers
{
    public partial class TextServer : Server
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
                return ServerType.None;
            }
        }
        public TextServer() : base(new TextServerThread())
        {

        }

        protected override void Deregister()
        {
         
        }

        public Vector2 workSize = Vector2.Zero;
        public Vector2 workPositon = Vector2.Zero;

        protected override void Loop(double delta)
        {

          
        }

        public void Draw(double delta)
        {
            this.Loop(delta);
        }

        protected override void Register()
        {
            
        }

    }
}
