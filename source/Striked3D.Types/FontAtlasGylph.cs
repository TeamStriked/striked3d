using BinaryPack.Attributes;
using BinaryPack.Enums;
using Msdfgen;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Types
{
    [BinarySerialization(SerializationMode.Explicit)]
    public struct FontAtlasGylph
    {
        [SerializableMember]
        public double advance { get; set; }

        [SerializableMember]
        public Vector2D<float> region { get; set; }

        [SerializableMember]
        public int atlasId { get; set; }

        public FontAtlasGylph(double advance, Vector2D<float> region, int atlasid)
        {
            this.advance = advance;
            this.region = region;
            this.atlasId = atlasid;
        }
    }

    [BinarySerialization(SerializationMode.Explicit)]
    public struct FontData
    {
        [SerializableMember]
        public Dictionary<int, Bitmap<FloatRgb>> atlasse { get; set; }

        [SerializableMember]
        public Dictionary<char, FontAtlasGylph> charIds { get; set; }

        [SerializableMember]
        public double ascend { get; set; }

        [SerializableMember]
        public double decend { get; set; }

        public double totalHeight => ascend + decend;

        public FontData(Dictionary<int, Bitmap<FloatRgb>> atlasse, Dictionary<char, FontAtlasGylph> charIds, double ascend, double decend)
        {
            this.atlasse = atlasse;
            this.charIds = charIds;
            this.ascend = ascend;
            this.decend = decend;
        }
    }
}
