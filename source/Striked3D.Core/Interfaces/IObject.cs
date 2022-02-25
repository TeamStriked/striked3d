using System;

namespace Striked3D.Core
{
    public interface IObject : IDisposable
    {
        public Guid Id { get; set; }
    }
}
