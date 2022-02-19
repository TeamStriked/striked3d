using System;
using System.Runtime.InteropServices;

namespace Msdfgen
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct FloatRgb
    {
        public float R , G , B;
    }

    [Serializable]
    public class Bitmap<T> where T : struct
    {
        private readonly T[,] _content;

        public Bitmap(int width, int height)
        {
            Width = width;
            Height = height;
            _content = new T[width, height];
        }

        public ref T this[int x, int y] => ref _content[x, y];

        public void SetPixel(int x, int y, T color)
        {
            this._content[x, y] = color;
        }

        public T GetPixel(int x, int y)
        {
            return this._content[x, y];
        }

        public int Width { get; }

        public int Height { get; }
    }
}