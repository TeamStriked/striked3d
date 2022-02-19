using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Nodes
{

    public class ViewportContainer : Control
    {
        public override void OnDraw2D(IRenderer renderer)
        {
             base.OnDraw2D(renderer);
        }

        public override void BeforeDraw(IRenderer renderer)
        {
             base.BeforeDraw(renderer);
        }
        public override void DrawCanvas()
        {
        }

        public override void UpdateSizes()
        {
            base.UpdateSize();

            if (this.Root != null)
            {
                this.AdjustChilds();
            }
        }

        private void AdjustChilds()
        {
            foreach (var child in this.GetChilds<Viewport>())
            {
                if (this.ScreenPosition != child.Position)
                {
                    child.Position = this.ScreenPosition;
                }

                if (this.ScreenSize != child.Size)
                {
                    child.Size = this.ScreenSize;

                    foreach (var subChilds in child.GetChilds<Control>())
                    {
                        subChilds.UpdateSizes();
                    }
                }
            }
        }
    }
}
