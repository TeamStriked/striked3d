using Striked3D.Core.Reference;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Striked3D.Core
{
    public abstract class Resource : Object, IResource
    {
        public virtual void Dispose()
        {
            base.Dispose();
        }
  
    }
}
