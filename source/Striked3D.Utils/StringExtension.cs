namespace Striked3D.Utils
{
    public static class StringExtension
    {
        public static float ToFloat(this string value)
        {
            float.TryParse(value, out float result);
            return result;
        }
        public static int ToInteger(this string value)
        {
            int.TryParse(value, out int result);
            return result;
        }
    }
}
