using Msdfgen;
using Msdfgen.IO;
using Striked3D.Math;
using Striked3D.Resources;
using Striked3D.Types;
using Striked3D.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Striked3D.Importer
{
    public class FontImporter : ImportProcessor<Font>
    {
        public static int maxBitmapSize = 1024;
        public static int renderSize = 32;
        public static float renderRange = 4;
        public override string OutputExtension => ".stf";

        public override Font DoImport(byte[] array)
        {
            SharpFont.Library ft = ImportFont.InitializeFreetype();
            SharpFont.Face fontFace = ImportFont.LoadFont(ft, array);

            FontData data = new FontData(new Dictionary<int, Bitmap<FloatRgb>>(), new Dictionary<char, FontAtlasGylph>(), 0, 0);

            data.ascend = ImportFont.GetFontAscend(fontFace);
            data.decend = ImportFont.GetFontDecend(fontFace);

            FontGylph[]? listOfChars = GetAllChars(fontFace);
            int maxPossibleItems = (maxBitmapSize * maxBitmapSize) / (renderSize * renderSize);
            List<List<FontGylph>>? groupedChars = listOfChars.ToList().ChunkBy(maxPossibleItems);

            foreach (List<FontGylph> group in groupedChars)
            {
                data = GenerateAtlas(group, data);
            }

            groupedChars = null;

            fontFace.Dispose();
            ft.Dispose();

            return new Font
            {
                FontData = data
            };
        }

        public override Font DoImport(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new Exception("Cant load font");
            }

            SharpFont.Library ft = ImportFont.InitializeFreetype();
            SharpFont.Face fontFace = ImportFont.LoadFont(ft, filePath);

            FontData data = new FontData(new Dictionary<int, Bitmap<FloatRgb>>(), new Dictionary<char, FontAtlasGylph>(), 0, 0);

            data.ascend = ImportFont.GetFontAscend(fontFace);
            data.decend = ImportFont.GetFontDecend(fontFace);

            FontGylph[]? listOfChars = GetAllChars(fontFace);
            int maxPossibleItems = (maxBitmapSize * maxBitmapSize) / (renderSize * renderSize);
            List<List<FontGylph>>? groupedChars = listOfChars.ToList().ChunkBy(maxPossibleItems);

            foreach (List<FontGylph> group in groupedChars)
            {
                data = GenerateAtlas(group, data);
            }

            groupedChars = null;

            fontFace.Dispose();
            ft.Dispose();

            return new Font
            {
                FontData = data
            };
        }

        private FontGylph[] GetAllChars(SharpFont.Face fontFace)
        {
            List<FontGylph>? list = new List<FontGylph>();
            Generate.IMsdf generator = Generate.Msdf();
            foreach (char charCode in ImportFont.GetAllChars(fontFace))
            {
                float advance = 0;
                Vector2D<float> bearing = Vector2D<float>.Zero;
                Vector2D<float> size = Vector2D<float>.Zero;
                Shape shape = ImportFont.LoadGlyph(fontFace, charCode, ref advance, ref bearing, ref size);
                Bitmap<FloatRgb> msdf = new Bitmap<FloatRgb>(renderSize, renderSize);

                generator.Output = msdf;
                generator.Range = renderRange;
                generator.Scale = new Vector2(1.0);
                var diff = (renderSize - bearing.Y);
                generator.Translate = new Vector2(-bearing.X, diff);

                shape.Normalize();
                Coloring.EdgeColoringSimple(shape, 3.0);
                generator.Shape = shape;
                generator.Compute();

                list.Add(new FontGylph { advance = advance, bitmap = msdf, Char = charCode, bearing = bearing, size = size });
            }

            generator = null;
            return list.ToArray();
        }

        private FontData GenerateAtlas(List<FontGylph> chars, FontData data)
        {
            int atlasSetId = data.atlasse.Count();

            int maxPossibleItems = (maxBitmapSize * maxBitmapSize) / (renderSize * renderSize);

            int maxPossibleColumns = (int)System.Math.Ceiling((float)maxBitmapSize / renderSize);
            int maxPossibleRows = maxPossibleColumns;

            int requiredColumns = (chars.Count() > maxPossibleColumns) ? maxPossibleColumns : chars.Count();
            int requiredRows = (int)System.Math.Ceiling((chars.Count() / (float)maxPossibleItems) * maxPossibleRows);

            int width = requiredColumns * renderSize;
            int height = requiredRows * renderSize;

            Bitmap<FloatRgb> atlasBitmap = new Bitmap<FloatRgb>(width, height);

            int currentRow = 0;
            int currentColumn = 0;

            foreach (FontGylph gylph in chars)
            {
                int regionStartX = currentColumn * renderSize;
                int regionStartY = currentRow * renderSize;

                int regionStartXEnd = currentColumn * renderSize + renderSize;

                for (int y = 0; y < gylph.bitmap.Height; y++)
                {
                    for (int x = 0; x < gylph.bitmap.Width; x++)
                    {
                        atlasBitmap[x + regionStartX, y + regionStartY] = gylph.bitmap[x, (gylph.bitmap.Height - 1) - y];
                    }
                }

                data.charIds.Add(gylph.Char, new FontAtlasGylph { region = new Vector2D<float>(regionStartX, regionStartY),
                    bearing = gylph.bearing, size = gylph.size, advance = gylph.advance, atlasId = atlasSetId });

                if ((currentColumn + 1) == maxPossibleColumns)
                {
                    currentRow++;
                    currentColumn = 0;
                }
                else
                {
                    currentColumn++;
                }
            }

            data.atlasse.Add(atlasSetId, atlasBitmap);

            return data;
        }

        public static string GetSystemFontPath(string fontName)
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
