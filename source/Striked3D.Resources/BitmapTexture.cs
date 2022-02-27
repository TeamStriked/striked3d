using BinaryPack;
using BinaryPack.Attributes;
using BinaryPack.Enums;
using Msdfgen;
using Striked3D.Core;
using Striked3D.Core.Interfaces;
using Striked3D.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Striked3D.Resources
{
    [BinarySerialization(SerializationMode.Explicit)]
    public struct BitmapData
    {
        [SerializableMember]
        public Guid Id { get; set; }

        [SerializableMember]
        public Bitmap<FloatRgba> bitmapData { get; set; }
    }

    public class BitmapTexture : Resource, IDrawable, ISerializable
    {
        private bool isDirty = false;

        public IViewport Viewport => null;

        public Texture bitmapTexture;
        public TextureView bitmapTextureView;
        public ResourceSet bitmapTextureSet;

        private Bitmap<FloatRgba> _bitmapData;

        [Export]
        public Bitmap<FloatRgba> BitmapData
        {
            get => _bitmapData;
            set
            {
                SetProperty("Position", ref _bitmapData, value);
                isDirty = true;
            }
        }

        public bool IsVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void BeforeDraw(IRenderer renderer)
        {
            if(isDirty && BitmapData.Width > 0 && BitmapData.Height > 0)
            {
                Dispose();
                bitmapTexture = renderer.CreateTexture(TextureDescription.Texture2D(
                    (uint)BitmapData.Width,
                    (uint)BitmapData.Height,
                    1,
                    1,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Sampled | TextureUsage.Storage));

                bitmapTextureView  = renderer.CreateTextureView(bitmapTexture);

                ResourceSetDescription resourceSetDescription = new(renderer.MaterialBitmapTexture, bitmapTextureView, renderer.LinearSampler);
                bitmapTextureSet = renderer.CreateResourceSet(resourceSetDescription);

                byte[] buffer = new byte[BitmapData.Width * BitmapData.Height * 4];
                ulong buferId = 0;
                for (int y = 0; y < BitmapData.Height; y++)
                {
                    for (int x = 0; x < BitmapData.Width; x++)
                    {
                        FloatRgba rgba = BitmapData[x, y];

                        buffer[buferId++] = (byte)MathExtension.Clamp(rgba.R * 0x100, 0, 0xff);
                        buffer[buferId++] = (byte)MathExtension.Clamp(rgba.G * 0x100, 0, 0xff);
                        buffer[buferId++] = (byte)MathExtension.Clamp(rgba.B * 0x100, 0, 0xff);
                        buffer[buferId++] = (byte)MathExtension.Clamp(rgba.A * 0x100, 0, 0xff);
                    }
                }

                unsafe
                {
                    fixed (byte* texDataPtr = &buffer[0])
                    {
                        renderer.UpdateTexture(
                                              bitmapTexture, (IntPtr)texDataPtr, (uint)buffer.Length,
                                              0, 0, 0, (uint)BitmapData.Width, (uint)BitmapData.Height, 1,
                                              0, 0);
                    }
                }

                isDirty = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            bitmapTexture?.Dispose();
            bitmapTextureView?.Dispose();
            bitmapTextureSet?.Dispose();

            bitmapTexture = null;
            bitmapTextureView = null;
        }

        public byte[] Serialize()
        {
            BitmapData data = new BitmapData
            {
                Id = Id,
                bitmapData = BitmapData
            };

            return BinaryConverter.Serialize<BitmapData>(data);
        }

        public void Deserialize(byte[] array)
        {
            BitmapData data = BinaryConverter.Deserialize<BitmapData>(array);

            Id = data.Id;
            BitmapData = data.bitmapData;
        }
    }
}
