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
    public class Rectangle2D : Control
    {
        private RgbaFloat _Color = new RgbaFloat(1, 0, 0, 1);

        public RgbaFloat Color
        {
            get { return _Color; }
            set
            {
                SetProperty("Color", ref _Color, value);
                this.UpdateCanvas();
            }
        }

        public override void DrawCanvas()
        {
            this.DrawRect(Color, ScreenPosition, ScreenSize);
        }
    }
}
