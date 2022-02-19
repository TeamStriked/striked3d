using Striked3D.Core;
using Striked3D.Core.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Striked3D.Graphics
{
    public interface IDrawable
    {
        public IViewport Viewport { get; }

        [Export]
        public bool IsVisible { get; set; }

        public void BeforeDraw(IRenderer renderer);

    }
}
