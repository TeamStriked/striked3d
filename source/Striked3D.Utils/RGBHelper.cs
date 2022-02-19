using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Veldrid;

namespace Striked3D.Helpers
{
    public static class RGBHelper
    {
        public static RgbaFloat FromHex(string hex)
        {
            hex = hex.Replace("#", "");
            var bytes = Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => (int) Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();

            return new RgbaFloat((float) bytes[0] / 255, (float)bytes[1] / 255, (float)bytes[2] / 255, 1);

        }
    }
}
