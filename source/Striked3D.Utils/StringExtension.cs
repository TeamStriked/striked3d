using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Striked3D.Utils
{
    public static class StringExtension
    {
        public static float ToFloat(this string value)
        {
            float result = 0f;
            float.TryParse(value, out result);
            return result;
        }
        public static int ToInteger(this string value)
        {
            int result = 0;
            int.TryParse(value, out result);
            return result;
        }
    }
}
