using Striked3D.Core.Reference;

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
