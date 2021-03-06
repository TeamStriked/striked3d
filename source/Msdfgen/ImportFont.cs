using SharpFont;
using Striked3D.Math;
using System;
using System.Collections.Generic;
namespace Msdfgen.IO
{
    public static class ImportFont
    {
        public static Library InitializeFreetype()
        {
            return new Library();
        }

        public static void DeinitializeFreetype(Library library)
        {
            library.Dispose();
        }

        public static Face LoadFont(Library library, string filename)
        {
            return library.NewFace(filename, 0);
        }
        public static Face LoadFont(Library library, byte[] array)
        {
            return library.NewMemoryFace(array, 0);
        }

        public static void DestroyFont(Face font)
        {
            font.Dispose();
        }

        public static double GetFontScale(Face font)
        {
            return font.UnitsPerEM / 64.0;
        }

        public static double GetFontAscend(Face font)
        {
            return font.Size.Metrics.Ascender / 64.0;
        }
        public static double GetFontDecend(Face font)
        {
            return font.Size.Metrics.Descender / 64.0;
        }

        public static void GetFontWhitespaceWidth(ref double spaceAdvance, ref double tabAdvance, Face font)
        {
            font.LoadChar(' ', LoadFlags.NoScale, LoadTarget.Normal);
            spaceAdvance = font.Glyph.Advance.X.Value / 64.0;
            font.LoadChar('\t', LoadFlags.NoScale, LoadTarget.Normal);
            tabAdvance = font.Glyph.Advance.X.Value / 64.0;
        }
        public static List<char> GetAllChars(Face font)
        {
            List<char> chars = new List<char>();

            uint currentChar = font.GetFirstChar(out uint glyphIndex);
            chars.Add((char)currentChar);

            while (true)
            {
                currentChar = font.GetNextChar(currentChar, out glyphIndex);
                if (glyphIndex == 0)
                {
                    break;
                }
                else
                {
                    chars.Add((char)currentChar);
                }
            }

            return chars;
        }
        public static Shape LoadGlyph(Face font, uint unicode, ref float advance, ref Vector2D<float> bearing, ref Vector2D<float> size)
        {
            Shape result = new Shape();
            font.SetPixelSizes(0, 32);
            font.LoadChar(unicode, LoadFlags.NoScale, LoadTarget.Normal);

            result.InverseYAxis = false;
            advance = font.Glyph.Metrics.HorizontalAdvance.ToSingle() ;

            bearing = new Vector2D<float>(font.Glyph.Metrics.HorizontalBearingX.ToSingle(), font.Glyph.Metrics.HorizontalBearingY.ToSingle());
            size = new Vector2D<float>(font.Glyph.Metrics.Width.ToSingle(), font.Glyph.Metrics.Height.ToSingle());
            FtContext context = new FtContext(result);


                OutlineFuncs ftFunctions = new OutlineFuncs
            {
                MoveFunction = context.FtMoveTo,
                LineFunction = context.FtLineTo,
                ConicFunction = context.FtConicTo,
                CubicFunction = context.FtCubicTo,
                Shift = 0
            };

            font.Glyph.Outline.Decompose(ftFunctions, IntPtr.Zero);
            return result;
        }

        public static double GetKerning(Face font, uint unicode1, uint unicode2)
        {
            FTVector26Dot6 kerning = font.GetKerning(font.GetCharIndex(unicode1), font.GetCharIndex(unicode2),
                KerningMode.Unscaled);
            return kerning.X.Value / 64.0;
        }

        private class FtContext
        {
            private readonly Shape _shape;
            private Contour _contour;
            private Vector2 _position;

            public FtContext(Shape output)
            {
                _shape = output;
            }

            private static Vector2 FtPoint2(ref FTVector vector)
            {
                return new Vector2(vector.X.Value / 64.0, vector.Y.Value / 64.0);
            }

            internal int FtMoveTo(ref FTVector to, IntPtr context)
            {
                _contour = new Contour();
                _shape.Add(_contour);
                _position = FtPoint2(ref to);
                return 0;
            }

            internal int FtLineTo(ref FTVector to, IntPtr context)
            {
                _contour.Add(new LinearSegment(_position, FtPoint2(ref to)));
                _position = FtPoint2(ref to);
                return 0;
            }

            internal int FtConicTo(ref FTVector control, ref FTVector to, IntPtr context)
            {
                _contour.Add(new QuadraticSegment(EdgeColor.White, _position, FtPoint2(ref control), FtPoint2(ref to)));
                _position = FtPoint2(ref to);
                return 0;
            }

            internal int FtCubicTo(ref FTVector control1, ref FTVector control2, ref FTVector to, IntPtr context)
            {
                _contour.Add(new CubicSegment(EdgeColor.White, _position, FtPoint2(ref control1), FtPoint2(ref control2), FtPoint2(ref to)));
                _position = FtPoint2(ref to);
                return 0;
            }
        }
    }
}
