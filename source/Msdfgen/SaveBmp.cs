using System;
using System.IO;

namespace Msdfgen
{
    public static class Bmp
    {
        public static void SaveBmp(Bitmap<float> bitmap, string filename)
        {
            using (Writer writer = new Writer(filename))
            {
                writer.WriteContent(bitmap);
            }
        }

        public static void SaveBmp(Bitmap<FloatRgb> bitmap, string filename)
        {
            using (Writer writer = new Writer(filename))
            {
                writer.WriteContent(bitmap);
            }
        }

        private class Writer : IDisposable
        {
            private readonly FileStream _file;
            private readonly BinaryWriter _writer;

            internal Writer(string name)
            {
                _writer = new BinaryWriter(_file = new FileStream(name, FileMode.OpenOrCreate));
            }

            public void Dispose()
            {
                _writer?.Dispose();
                _file?.Dispose();
            }

            private int WriteHeader(int width, int height)
            {
                const uint bitmapStart = 54;
                int paddedWidth = (3 * width + 3) & ~3;
                uint bitmapSize = (uint)(paddedWidth * height);
                uint fileSize = bitmapStart + bitmapSize;
                WriteHeaderSection1(fileSize, bitmapStart);
                WriteHeaderSection2BeforeSize(width, height);
                WriteHeaderSection2Rest(bitmapSize);
                return paddedWidth;
            }

            private void WriteHeaderSection2Rest(uint bitmapSize)
            {
                _writer.Write(bitmapSize);
                _writer.Write((uint)2835);
                _writer.Write((uint)2835);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
            }

            private void WriteHeaderSection2BeforeSize(int width, int height)
            {
                _writer.Write((uint)40);
                _writer.Write(width);
                _writer.Write(height);
                _writer.Write((ushort)1);
                _writer.Write((ushort)24);
                _writer.Write((uint)0);
            }

            private void WriteHeaderSection1(uint fileSize, uint bitmapStart)
            {
                _writer.Write((ushort)0x4d42u);
                _writer.Write(fileSize);
                _writer.Write((ushort)0);
                _writer.Write((ushort)0);
                _writer.Write(bitmapStart);
            }

            internal void WriteContent(Bitmap<float> bitmap)
            {
                int paddedWidth = WriteHeader(bitmap.Width, bitmap.Height);

                byte[] padding = new byte[paddedWidth - 3 * bitmap.Width];

                for (int y = 0; y < bitmap.Height; ++y)
                {
                    for (int x = 0; x < bitmap.Width; ++x)
                    {
                        byte px = (byte)MathExtension.Clamp(bitmap[x, y] * 0x100, 0, 0xff);
                        _writer.Write(px);
                        _writer.Write(px);
                        _writer.Write(px);
                    }

                    _writer.Write(padding);
                }
            }

            internal void WriteContent(Bitmap<FloatRgb> bitmap)
            {
                int paddedWidth = WriteHeader(bitmap.Width, bitmap.Height);

                byte[] padding = new byte[paddedWidth - 3 * bitmap.Width];

                for (int y = 0; y < bitmap.Height; ++y)
                {
                    for (int x = 0; x < bitmap.Width; ++x)
                    {
                        byte[] bgr = new[]
                        {
                            (byte) MathExtension.Clamp(bitmap[x, y].B * 0x100, 0, 0xff),
                            (byte) MathExtension.Clamp(bitmap[x, y].G * 0x100, 0, 0xff),
                            (byte) MathExtension.Clamp(bitmap[x, y].R * 0x100, 0, 0xff)
                        };
                        _writer.Write(bgr);
                    }

                    _writer.Write(padding);
                }
            }
        }
    }
}