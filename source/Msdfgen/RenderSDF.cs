using System;

namespace Msdfgen
{
    public static class Render
    {
        private static FloatRgb Mix(FloatRgb a, FloatRgb b, double weight)
        {
            FloatRgb output = new FloatRgb
            {
                R = Arithmetic.Mix(a.R, b.R, weight),
                G = Arithmetic.Mix(a.G, b.G, weight),
                B = Arithmetic.Mix(a.B, b.B, weight)
            };
            return output;
        }

        private static FloatRgb Sample(Bitmap<FloatRgb> bitmap, Vector2 pos)
        {
            int w = bitmap.Width, h = bitmap.Height;
            double x = pos.X * w - .5;
            double y = pos.Y * h - .5;
            int l = (int)Math.Floor(x);
            int b = (int)Math.Floor(y);
            int r = l + 1;
            int t = b + 1;
            double lr = x - l;
            double bt = y - b;
            l = MathExtension.Clamp(l, 0, w - 1);
            r = MathExtension.Clamp(r, 0, w - 1);
            b = MathExtension.Clamp(b, 0, h - 1);
            t = MathExtension.Clamp(t, 0, h - 1);
            return Mix(Mix(bitmap[l, b], bitmap[r, b], lr), Mix(bitmap[l, t], bitmap[r, t], lr), bt);
        }

        private static float Sample(Bitmap<float> bitmap, Vector2 pos)
        {
            int w = bitmap.Width, h = bitmap.Height;
            double x = pos.X * w - .5;
            double y = pos.Y * h - .5;
            int l = (int)Math.Floor(x);
            int b = (int)Math.Floor(y);
            int r = l + 1;
            int t = b + 1;
            double lr = x - l;
            double bt = y - b;
            l = MathExtension.Clamp(l, 0, w - 1);
            r = MathExtension.Clamp(r, 0, w - 1);
            b = MathExtension.Clamp(b, 0, h - 1);
            t = MathExtension.Clamp(t, 0, h - 1);
            return Arithmetic.Mix(Arithmetic.Mix(bitmap[l, b], bitmap[r, b], lr),
                Arithmetic.Mix(bitmap[l, t], bitmap[r, t], lr), bt);
        }

        private static float DistVal(float dist, double pxRange)
        {
            if (pxRange == 0)
            {
                return dist > .5 ? 1 : 0;
            }

            return (float)MathExtension.Clamp((dist - .5) * pxRange + .5, 0, 1);
        }

        public static void RenderSdf(Bitmap<float> output, Bitmap<float> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double)(w + h) / (sdf.Width + sdf.Height);
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    float s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                    output[x, y] = DistVal(s, pxRange);
                }
            }
        }

        public static void RenderSdf(Bitmap<FloatRgb> output, Bitmap<float> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double)(w + h) / (sdf.Width + sdf.Height);
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    float s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                    float v = DistVal(s, pxRange);
                    output[x, y].R = v;
                    output[x, y].G = v;
                    output[x, y].B = v;
                }
            }
        }

        public static void RenderSdf(Bitmap<float> output, Bitmap<FloatRgb> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double)(w + h) / (sdf.Width + sdf.Height);
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    FloatRgb s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                    output[x, y] = DistVal(Arithmetic.Median(s.R, s.G, s.B), pxRange);
                }
            }
        }

        public static void RenderSdf(Bitmap<FloatRgb> output, Bitmap<FloatRgb> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double)(w + h) / (sdf.Width + sdf.Height);
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    FloatRgb s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                    output[x, y].R = DistVal(s.R, pxRange);
                    output[x, y].G = DistVal(s.G, pxRange);
                    output[x, y].B = DistVal(s.B, pxRange);
                }
            }
        }

        public static void Simulate8Bit(Bitmap<float> bitmap)
        {
            int w = bitmap.Width, h = bitmap.Height;
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    byte v = (byte)MathExtension.Clamp(bitmap[x, y] * 0x100, 0, 0xff);
                    bitmap[x, y] = v / 255.0f;
                }
            }
        }

        public static void Simulate8Bit(Bitmap<FloatRgb> bitmap)
        {
            int w = bitmap.Width, h = bitmap.Height;
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    byte r = (byte)MathExtension.Clamp(bitmap[x, y].R * 0x100, 0, 0xff);
                    byte g = (byte)MathExtension.Clamp(bitmap[x, y].G * 0x100, 0, 0xff);
                    byte b = (byte)MathExtension.Clamp(bitmap[x, y].B * 0x100, 0, 0xff);
                    bitmap[x, y].R = r / 255.0f;
                    bitmap[x, y].G = g / 255.0f;
                    bitmap[x, y].B = b / 255.0f;
                }
            }
        }
    }
}