using BinaryPack;
using Msdfgen;
using Striked3D.Core;
using Striked3D.Core.Interfaces;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using Veldrid;

namespace Striked3D.Resources
{
    public enum FontAlign
    {
        Left,
        Rright,
        Center
    }
    public struct FontGylph
    {
        public Bitmap<FloatRgb> bitmap { get; set; }
        public double advance { get; set; }
        public int Order { get; set; }
        public char Char { get; set; }
    }


    public class FontAtlas
    {
        public Texture fontAtlasTexture;
        public TextureView fontAtlasTextureView;
        public ResourceSet fontAtlasSet;

        public void Dispose()
        {
            fontAtlasTexture?.Dispose();
            fontAtlasTextureView?.Dispose();
            fontAtlasSet?.Dispose();

            fontAtlasTexture = null;
            fontAtlasTextureView = null;
        }
    }

    public struct FontSerializeData
    {
        public Guid Id { get; set; }
        public FontData Data { get; set; }
    }

    public class Font : Resource, ISerializable
    {
        public FontData _FontData;
        public FontData FontData
        {
            get => _FontData;
            set
            {
                _FontData = value;
                isDirty = true;
            }
        }

        public static Font SystemFont;

        private bool isDirty = false;

        private readonly List<char> charsInUsage = new();
        private readonly Dictionary<int, FontAtlas> renderAtlases = new();
        public void Bind(IRenderer device)
        {
            if (isDirty == true)
            {
                CreateTexture(device);
                isDirty = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (KeyValuePair<int, FontAtlas> atlas in renderAtlases)
            {
                atlas.Value.Dispose();
            }
            renderAtlases.Clear();

        }

        public FontAtlasGylph GetChar(char c)
        {
            if (!FontData.charIds.ContainsKey(c))
            {
                return default;
            }

            return FontData.charIds[c];
        }
        public FontAtlas GetAtlas(int atlasId)
        {
            if (!renderAtlases.ContainsKey(atlasId))
            {
                return default;
            }

            return renderAtlases[atlasId];
        }

        private void CreateTexture(IRenderer device)
        {
            Dispose();

            if (FontData.atlasse.Count <= 0)
            {
                return;
            }

            foreach (KeyValuePair<int, Bitmap<FloatRgb>> bitmap in FontData.atlasse)
            {
                FontAtlas? atlas = new FontAtlas();
                if (bitmap.Value.Width == 0 || bitmap.Value.Height == 0)
                {
                    return;
                }

                atlas.Dispose();
                atlas.fontAtlasTexture = device.CreateTexture(TextureDescription.Texture2D(
                    (uint)bitmap.Value.Width,
                    (uint)bitmap.Value.Height,
                    1,
                    1,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Sampled | TextureUsage.Storage));

                atlas.fontAtlasTextureView = device.CreateTextureView(atlas.fontAtlasTexture);

                ResourceSetDescription resourceSetDescription = new(device.FontAtlasLayout, atlas.fontAtlasTextureView, device.LinearSampler);
                atlas.fontAtlasSet = device.CreateResourceSet(resourceSetDescription);

                byte[] buffer = new byte[bitmap.Value.Width * bitmap.Value.Height * 4];
                ulong buferId = 0;
                for (int y = 0; y < bitmap.Value.Height; y++)
                {
                    for (int x = 0; x < bitmap.Value.Width; x++)
                    {
                        FloatRgb rgb = bitmap.Value[x, y];

                        buffer[buferId++] = (byte)MathExtension.Clamp(rgb.R * 0x100, 0, 0xff);
                        buffer[buferId++] = (byte)MathExtension.Clamp(rgb.G * 0x100, 0, 0xff);
                        buffer[buferId++] = (byte)MathExtension.Clamp(rgb.B * 0x100, 0, 0xff);
                        buffer[buferId++] = 1;
                    }
                }

                unsafe
                {
                    fixed (byte* texDataPtr = &buffer[0])
                    {
                        device.UpdateTexture(
                                              atlas.fontAtlasTexture, (IntPtr)texDataPtr, (uint)buffer.Length,
                                              0, 0, 0, (uint)bitmap.Value.Width, (uint)bitmap.Value.Height, 1,
                                              0, 0);
                    }
                }

                renderAtlases.Add(bitmap.Key, atlas);
            }
        }

        public byte[] Serialize()
        {
            FontSerializeData data = new FontSerializeData
            {
                Id = Id,
                Data = FontData
            };

            return BinaryConverter.Serialize<FontSerializeData>(data);
        }

        public void Deserialize(byte[] byteArray)
        {
            FontSerializeData data = BinaryConverter.Deserialize<FontSerializeData>(byteArray);

            Id = data.Id;
            FontData = data.Data;
        }
    }
}
