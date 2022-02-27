using Msdfgen;
using Msdfgen.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
    public class BitmapTextureImporter : ImportProcessor<BitmapTexture>
    {
        public override string OutputExtension => ".stb";

        public override BitmapTexture DoImport(byte[] array)
        {
            Image<Rgba32> image = Image.Load<Rgba32>(array);

            var bitmap = new Bitmap<FloatRgba>(image.Width, image.Height);

            for(int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var imageVector = image[x, y].ToVector4();
                    bitmap.SetPixel(x, y, new FloatRgba { R = imageVector.X, G = imageVector.Y, B = imageVector.Z, A = imageVector.W });
                }
            }

            return new BitmapTexture
            {
                BitmapData = bitmap
            };
        }

        public override BitmapTexture DoImport(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new Exception("Cant load font");
            }

            return null;
        }
    }
}
