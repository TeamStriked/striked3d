using Msdfgen;
using Msdfgen.IO;
using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Core.Graphics;
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
        public static  int renderSize = 32;
        public static float renderRange = 4;
        public static Font SystemFont = new ("Arial");
        
        public double ascend;
        public double decend;

        private FontAtlas atlas;
        private ResourceSet fontAtlasSet;

        private readonly string FilePath = null;

        private Texture fontAtlasTexture;
        private TextureView fontAtlasTextureView;

        public FontAtlas Atlas
        {
            get
            {
                return atlas;
            }
        }
        public ResourceSet ResourceAtlasSet
        {
            get
            {
                return fontAtlasSet;
            }
        }

        public double totalHeight
        {
            get
            {
                return ascend + decend;
            }
        }

        private Dictionary<char, FontGylph> cache = new();

        public Font(string fontname)
        {
            FilePath = GetFilesForFont(fontname);

            if(String.IsNullOrEmpty(FilePath))
            {
                throw new Exception("Cant load font");
            }

            var ft = ImportFont.InitializeFreetype();
            var fontFace = ImportFont.LoadFont(ft, FilePath);

            this.ascend = ImportFont.GetFontAscend(fontFace);
            this.decend = ImportFont.GetFontDecend(fontFace);


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
                var ft = ImportFont.InitializeFreetype();
                var fontFace = ImportFont.LoadFont(ft, FilePath);

                var generator = Generate.Msdf();

                double advance = 0;
                var shape = ImportFont.LoadGlyph(fontFace, charCode, ref advance);
                var msdf = new Bitmap<FloatRgb>(renderSize, renderSize);

                generator.Output = msdf;
                generator.Range = renderRange;
                generator.Scale = new Vector2(1.0);
                generator.Translate = new Vector2(0, 0);

                shape.Normalize();

                Coloring.EdgeColoringSimple(shape, 3.0);

                generator.Shape = shape;
                generator.Compute();

                var record = new FontGylph { advance = advance, bitmap = msdf, Order = this.cache.Values.Count };
                cache.Add(charCode, record);
                isDirty = true;

                fontFace.Dispose();
                ft.Dispose();
            }
        }

        bool isDirty = false;

        public FontGylph GetGlyph(char c)
        {
            if(this.cache.ContainsKey(c))
                return this.cache[c];
            else
            {
                return default;
            }
        }

        public void AddChars(string searchString)
        {
            foreach(var c in searchString)
            {
                this.AddChar(c);
            }

            if(isDirty == true)
            {
                this.GenerateAtlas();
            }
        }

        public void Bind(GraphicService device)
        {
            if(this.isDirty == true)
            {
                this.CreateTexture(device);
                this.isDirty = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            fontAtlasTexture?.Dispose();
            fontAtlasTextureView?.Dispose();
            this.fontAtlasSet?.Dispose();

            this.fontAtlasTexture = null;
            this.fontAtlasTextureView = null;
            this.fontAtlasSet = null;
        }

        private void CreateTexture(GraphicService device)
        {
            this.Dispose();

            if (atlas.bitmap == null)
                return;

            //buffers
            fontAtlasTexture = device.Renderer3D.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)atlas.bitmap.Width,
                (uint)atlas.bitmap.Height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled | TextureUsage.Storage));

            fontAtlasTextureView = device.Renderer3D.ResourceFactory.CreateTextureView(fontAtlasTexture);

            ResourceSetDescription resourceSetDescription = new (device.FontAtlasLayout, fontAtlasTextureView, device.Renderer3D.LinearSampler);
            fontAtlasSet = device.Renderer3D.ResourceFactory.CreateResourceSet(resourceSetDescription);

            var buffer = new byte[atlas.bitmap.Width * atlas.bitmap.Height * 4];
            ulong buferId = 0;
            for (var y = 0; y < atlas.bitmap.Height; y++)
            {
                for (var x = 0; x < atlas.bitmap.Width; x++)
                {
                    var rgb = atlas.bitmap[x, y];

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
                                          this.fontAtlasTexture, (IntPtr)texDataPtr, (uint)buffer.Length,
                                          0, 0, 0, (uint)atlas.bitmap.Width, (uint)atlas.bitmap.Height, 1,
                                          0, 0);
                }
            }
        }

        private void GenerateAtlas()
        {
            var maxPossibleColumns = (int) MathF.Ceiling(2048 / renderSize);
            var totalColumns = (this.cache.Count > maxPossibleColumns) ? maxPossibleColumns : this.cache.Count;
            var totalRows = Math.Max(1, (int)MathF.Floor((float)this.cache.Count / maxPossibleColumns));

            var width = (int)totalColumns * renderSize;
            var height = (int)totalRows * renderSize;

            var atlasBitmap = new Bitmap<FloatRgb>(width, height);
            var currentRow = 0;
            var currentColumn = 0;
            var cacheLookup = new Dictionary<char, FontAtlasGylph>();
          
            foreach (var gylph in cache.OrderBy(df => df.Value.Order).ToArray())
            {
                var regionStartX = currentColumn * renderSize;
                var regionStartY = currentRow * renderSize;

                var regionStartXEnd = currentColumn * renderSize + renderSize;

                for (var y = 0; y < gylph.Value.bitmap.Height; y++)
                {
                    for (var x = 0; x < gylph.Value.bitmap.Width; x++)
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
            DirectoryInfo directoryInfo = new (path);
            FileInfo[] fontFiles = directoryInfo.GetFiles("*.ttf");

            foreach (var fontFile in fontFiles)
            {
                var fileWithoutExt = fontFile.Name.Replace(fontFile.Extension, "");
                if(fileWithoutExt.ToLower() == fontName.ToLower())
                {
                    return fontFile.FullName;
                }
            }

            return null;
        }
    }
}
