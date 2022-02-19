using Striked3D.Core;
using Striked3D.Core.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Graphics
{
    public interface IDrawable2D : IDrawable
    {
        public  void OnDraw2D(IRenderer renderer);
    }
}
