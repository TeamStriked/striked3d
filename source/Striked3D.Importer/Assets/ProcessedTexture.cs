using System;
using System.IO;
using Veldrid;

namespace Striked3D.Core.AssetsPrimitives
{
  
    public class ProcessedTextureDataSerializer : BinaryAssetSerializer<ProcessedTexture>
    {
        public override ProcessedTexture ReadT(BinaryReader reader)
        {
            return new ProcessedTexture(
                reader.ReadEnum<PixelFormat>(),
                reader.ReadEnum<TextureType>(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadByteArray());
        }

        public override void WriteT(BinaryWriter writer, ProcessedTexture ptd)
        {
            writer.WriteEnum(ptd.Format);
            writer.WriteEnum(ptd.Type);
            writer.Write(ptd.Width);
            writer.Write(ptd.Height);
            writer.Write(ptd.Depth);
            writer.Write(ptd.MipLevels);
            writer.Write(ptd.ArrayLayers);
            writer.WriteByteArray(ptd.TextureData);
        }
    }
}
