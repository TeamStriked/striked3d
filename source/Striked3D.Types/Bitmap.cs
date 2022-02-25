using BinaryPack.Attributes;
using BinaryPack.Enums;
using System;
using System.Runtime.InteropServices;


namespace Msdfgen
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [BinarySerialization(SerializationMode.Explicit)]
    public struct FloatRgb
    {
        [SerializableMember]
        public float R;
        [SerializableMember]
        public float G;
        [SerializableMember]
        public float B;

        public FloatRgb(float R, float G, float B)
        {
            this.R = R;
            this.G = G;
            this.B = B;
        }

    }

    [BinarySerialization(SerializationMode.Explicit)]
    public struct Bitmap<T> where T : struct
    {
        [SerializableMember]
        public T[,] content { get; set; } 

        public Bitmap(int width, int height)
        {
            Width = width;
            Height = height;

            content = new T[width, height];
        }

        public ref T this[int x, int y] => ref content[x, y];

        public void SetPixel(int x, int y, T color)
        {
            content[x, y] = color;
        }

        public T GetPixel(int x, int y)
        {
            return content[x, y];
        }

        [SerializableMember]
        public int Width { get; set; }

        [SerializableMember]
        public int Height { get; set; }
    }


}
