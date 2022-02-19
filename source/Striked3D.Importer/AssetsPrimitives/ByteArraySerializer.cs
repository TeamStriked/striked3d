using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Striked3D.Core.AssetsPrimitives
{
    public class ByteArraySerializer : BinaryAssetSerializer<byte[]>
    {
        public override byte[] ReadT(BinaryReader reader)
        {
            return reader.ReadByteArray();
        }

        public override void WriteT(BinaryWriter writer, byte[] value)
        {
            writer.WriteByteArray(value);
        }
    }
}
