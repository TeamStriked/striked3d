using Striked3D.Core.Reference;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    public interface IObject: IDisposable
    {
        public Guid Id { get; }
    }
}
