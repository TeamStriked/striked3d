﻿using Striked3D.Core;

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

            if (Root != null)
            {
                AdjustChilds();
            }
        }

        private void AdjustChilds()
        {
            foreach (Viewport child in GetChilds<Viewport>())
            {
                if (ScreenPosition != child.Position)
                {
                    child.Position = ScreenPosition;
                }

                if (ScreenSize != child.Size)
                {
                    child.Size = ScreenSize;

                    foreach (Control subChilds in child.GetChilds<Control>())
                    {
                        subChilds.UpdateSizes();
                    }
                }
            }
        }
    }
}