using System.CodeDom.Compiler;
using Msdfgen.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Striked3D.Types;

namespace Msdfgen.ManualTest
{
    public struct FontCacheReponseGlyph
    {
        public double advance;
        public Vector2D<float> region;
    }

    public struct FontCacheReponse
    {
        public Bitmap<FloatRgb> bitmap;
        public Dictionary<char, FontCacheReponseGlyph> chars;
    }

    public struct FontCacheGlyph
    {
        public Bitmap<FloatRgb> bitmap;
        public double advance;
    }

    public class FontCache
    {
       public  Dictionary<char, FontCacheGlyph> Glyphs = new Dictionary<char, FontCacheGlyph>();
    }

    public static class SDFFontParser
    {
        public const int renderSize = 32;
        public const float renderRange = 4;

        public static Dictionary<string, FontCache> fontCache = new Dictionary<string, FontCache>();
        public static FontCache cacheText(string fontPath, string code)
        {
            var cacheId = CreateMD5(fontPath);

            if (!fontCache.ContainsKey(cacheId))
            {
                fontCache.Add(cacheId, new FontCache());
            }

            var cache = fontCache[cacheId];
            var ft = ImportFont.InitializeFreetype();
            var font = ImportFont.LoadFont(ft, fontPath);
            var generator = Generate.Msdf();
            var cachedGlyphs = code.Distinct().ToArray();

            foreach (var charCode in cachedGlyphs)
            {
                if(!cache.Glyphs.ContainsKey(charCode))
                {
                    double advance = 0;
                    var shape = ImportFont.LoadGlyph(font, charCode, ref advance);
                    var msdf = new Bitmap<FloatRgb>(renderSize, renderSize);

                    generator.Output = msdf;
                    generator.Range = renderRange;
                    generator.Scale = new Vector2(0.9);
                    generator.Translate = new Vector2(1, 1);

                    shape.Normalize();

                    Coloring.EdgeColoringSimple(shape, 3.0);

                    generator.Shape = shape;
                    generator.Compute();

                    cache.Glyphs.Add(charCode, new FontCacheGlyph { bitmap = msdf , advance = advance});
                }
            }

            ImportFont.DestroyFont(font);
            ImportFont.DeinitializeFreetype(ft);

            return cache;
        }
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static FontCacheReponse renderText(string fontPath, string searchString)
        {
            var cache = cacheText(fontPath, searchString);
            char[] searchArray = searchString.Distinct().ToArray();
            var cachedChars = cache.Glyphs.Where(d => searchArray.Contains(d.Key)).ToArray();

            var totalGlphysPerRow = Math.Ceiling(Math.Sqrt(cachedChars.Length));

            var width = (int)totalGlphysPerRow * renderSize;
            var height = (int)totalGlphysPerRow * renderSize;

            var atlasBitmap = new Bitmap<FloatRgb>(width, height);
            var currentRow = 0;
            var currentColumn = 0;
            var cacheLookup = new Dictionary<char, FontCacheReponseGlyph>();

            foreach (var c in cachedChars.OrderBy(df => df.Key))
            {
                var regionStartX = currentColumn * renderSize;
                var regionStartY = currentRow * renderSize;

                for (var y = 0; y < c.Value.bitmap.Height; y++)
                {
                    for (var x = 0; x < c.Value.bitmap.Width; x++)
                    {
                        atlasBitmap[x + regionStartX, y + regionStartY] = c.Value.bitmap[x, y];
                    }
                }

                cacheLookup.Add(c.Key, new FontCacheReponseGlyph { region = new Vector2D<float>(regionStartX, regionStartY), advance = c.Value.advance }) ;
                currentColumn++;
                if (currentColumn >= totalGlphysPerRow)
                {
                    currentRow++;
                    currentColumn = 0;
                }
            }

            return new FontCacheReponse
            {
                chars = cacheLookup,
                bitmap = atlasBitmap
            };
        }

        /*
        static void Main(string[] args)
        {
            var res = renderText("test.ttf", "123DF");
            Console.WriteLine(res);
        }
        */
    }
}
