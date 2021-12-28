using Striked3D.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    public class BaseNode
    {
        private Guid id = Guid.NewGuid();
        public Guid Id { get { return id; } }

        public Striked3D.Core.Window window { get; set; }

        public BaseNode()
        {
        }
        public virtual void Update(double delta)
        {
        }
        public virtual void Render(double delta)
        {
        }

        public virtual void OnEnterTree()
        {
            Logger.Debug(this, "Enter Tree");
        }

        public virtual void OnInput(InputEvent e)
        {
        }
    }
}
