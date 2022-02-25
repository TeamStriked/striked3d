using BinaryPack.Attributes;
using BinaryPack.Enums;
using System.Collections.Generic;

namespace Striked3D.Importer
{
    [BinarySerialization(SerializationMode.Properties | SerializationMode.NonPublicMembers)]
    public class SerializeModel
    {
        public Dictionary<string, object> Content { get; set; }
        public int Version { get; set; }
    }
}
