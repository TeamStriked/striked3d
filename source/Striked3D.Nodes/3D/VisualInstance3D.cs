using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Nodes
{
    public class VisualInstance3D : Node3D, IDrawable3D
    {
        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        protected bool _isDirty = false;
        public bool IsDirty => _isDirty;

        public virtual void OnDraw3D(IRenderer renderer)
        {
        }

        public virtual void BeforeDraw(IRenderer renderer)
        {
        }
    }
}
