using Msdfgen;
using Msdfgen.IO;
using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    }
    public struct FontAtlasGylph
    {
        public double advance;
        public Vector2D<float> region;
    }

    public struct FontAtlas
    {
        public Bitmap<FloatRgb> bitmap;
        public Dictionary<char, FontAtlasGylph> chars;
    }

    public class Font : Resource
    {
        public static int renderSize = 32;
        public static float renderRange = 4;
        public static Font SystemFont = new("Arial");

        public double ascend;
        public double decend;

        private FontAtlas atlas;
        private ResourceSet fontAtlasSet;

        private readonly string FilePath = null;

        private Texture fontAtlasTexture;
        private TextureView fontAtlasTextureView;

        public FontAtlas Atlas => atlas;
        public ResourceSet ResourceAtlasSet => fontAtlasSet;

        public double totalHeight => ascend + decend;

        private readonly Dictionary<char, FontGylph> cache = new();

        public Font(string fontname)
        {
            FilePath = GetFilesForFont(fontname);

            if (string.IsNullOrEmpty(FilePath))
            {
                throw new Exception("Cant load font");
            }

            SharpFont.Library ft = ImportFont.InitializeFreetype();
            SharpFont.Face fontFace = ImportFont.LoadFont(ft, FilePath);

            ascend = ImportFont.GetFontAscend(fontFace);
            decend = ImportFont.GetFontDecend(fontFace);


            fontFace.Dispose();
            ft.Dispose();
        }

        private void AddChar(char charCode)
        {
            if (cache.ContainsKey(charCode))
            {
                return;
            }

            lock (cache)
            {
                SharpFont.Library ft = ImportFont.InitializeFreetype();
                SharpFont.Face fontFace = ImportFont.LoadFont(ft, FilePath);

                Generate.IMsdf generator = Generate.Msdf();

                double advance = 0;
                Shape shape = ImportFont.LoadGlyph(fontFace, charCode, ref advance);
                Bitmap<FloatRgb> msdf = new Bitmap<FloatRgb>(renderSize, renderSize);

                generator.Output = msdf;
                generator.Range = renderRange;
                generator.Scale = new Vector2(1.0);
                generator.Translate = new Vector2(0, 0);

                shape.Normalize();

                Coloring.EdgeColoringSimple(shape, 3.0);

                generator.Shape = shape;
                generator.Compute();

                FontGylph record = new FontGylph { advance = advance, bitmap = msdf, Order = cache.Values.Count };
                cache.Add(charCode, record);
                isDirty = true;

                fontFace.Dispose();
                ft.Dispose();
            }
        }

        private bool isDirty = false;

        public FontGylph GetGlyph(char c)
        {
            if (cache.ContainsKey(c))
            {
                return cache[c];
            }
            else
            {
                return default;
            }
        }

        public void AddChars(string searchString)
        {
            foreach (char c in searchString)
            {
                AddChar(c);
            }

            if (isDirty == true)
            {
                GenerateAtlas();
            }
        }

        public void Bind(GraphicService device)
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

            fontAtlasTexture?.Dispose();
            fontAtlasTextureView?.Dispose();
            fontAtlasSet?.Dispose();

            fontAtlasTexture = null;
            fontAtlasTextureView = null;
            fontAtlasSet = null;
        }

        private void CreateTexture(GraphicService device)
        {
            Dispose();

            if (atlas.bitmap == null)
            {
                return;
            }

            //buffers
            fontAtlasTexture = device.Renderer3D.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)atlas.bitmap.Width,
                (uint)atlas.bitmap.Height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled | TextureUsage.Storage));

            fontAtlasTextureView = device.Renderer3D.ResourceFactory.CreateTextureView(fontAtlasTexture);

            ResourceSetDescription resourceSetDescription = new(device.FontAtlasLayout, fontAtlasTextureView, device.Renderer3D.LinearSampler);
            fontAtlasSet = device.Renderer3D.ResourceFactory.CreateResourceSet(resourceSetDescription);

            byte[] buffer = new byte[atlas.bitmap.Width * atlas.bitmap.Height * 4];
            ulong buferId = 0;
            for (int y = 0; y < atlas.bitmap.Height; y++)
            {
                for (int x = 0; x < atlas.bitmap.Width; x++)
                {
                    FloatRgb rgb = atlas.bitmap[x, y];

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
                    device.Renderer3D.UpdateTexture(
                                          fontAtlasTexture, (IntPtr)texDataPtr, (uint)buffer.Length,
                                          0, 0, 0, (uint)atlas.bitmap.Width, (uint)atlas.bitmap.Height, 1,
                                          0, 0);
                }
            }
        }

        private void GenerateAtlas()
        {
            int maxPossibleColumns = (int)MathF.Ceiling(2048 / renderSize);
            int totalColumns = (cache.Count > maxPossibleColumns) ? maxPossibleColumns : cache.Count;
            int totalRows = Math.Max(1, (int)MathF.Floor((float)cache.Count / maxPossibleColumns));

            int width = totalColumns * renderSize;
            int height = totalRows * renderSize;

            Bitmap<FloatRgb> atlasBitmap = new Bitmap<FloatRgb>(width, height);
            int currentRow = 0;
            int currentColumn = 0;
            Dictionary<char, FontAtlasGylph> cacheLookup = new Dictionary<char, FontAtlasGylph>();

            foreach (KeyValuePair<char, FontGylph> gylph in cache.OrderBy(df => df.Value.Order).ToArray())
            {
                int regionStartX = currentColumn * renderSize;
                int regionStartY = currentRow * renderSize;

                int regionStartXEnd = currentColumn * renderSize + renderSize;

                for (int y = 0; y < gylph.Value.bitmap.Height; y++)
                {
                    for (int x = 0; x < gylph.Value.bitmap.Width; x++)
                    {
                        atlasBitmap[x + regionStartX, y + regionStartY] = gylph.Value.bitmap[x, y];
                    }
                }

                cacheLookup.Add(gylph.Key, new FontAtlasGylph { region = new Vector2D<float>(regionStartX, regionStartY), advance = gylph.Value.advance });

                if (currentColumn == maxPossibleColumns)
                {
                    currentRow++;
                    currentColumn = 0;
                }
                else
                {
                    currentColumn++;
                }
            }


            atlas = new FontAtlas
            {
                chars = cacheLookup,
                bitmap = atlasBitmap
            };
        }

        private string GetFilesForFont(string fontName)
        {
            Environment.SpecialFolder specialFolder = Environment.SpecialFolder.Fonts;
            string path = Environment.GetFolderPath(specialFolder);
            DirectoryInfo directoryInfo = new(path);
            FileInfo[] fontFiles = directoryInfo.GetFiles("*.ttf");

            foreach (FileInfo fontFile in fontFiles)
            {
                string fileWithoutExt = fontFile.Name.Replace(fontFile.Extension, "");
                if (fileWithoutExt.ToLower() == fontName.ToLower())
                {
                    return fontFile.FullName;
                }
            }

            return null;
        }
    }
}
