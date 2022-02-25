using BinaryPack.Attributes;
using BinaryPack.Enums;
using Striked3D.Core.Reference;

namespace Striked3D.Core
{
    [BinarySerialization(SerializationMode.Properties | SerializationMode.NonPublicMembers)]
    public abstract class Resource : Object, IResource
    {
        public virtual void Dispose()
        {
            base.Dispose();
        }
    }
}
