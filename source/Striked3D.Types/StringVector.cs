using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Types
{
    public struct StringVector
    {
        public string X;
        public string Y;

        public bool Equals(StringVector compare)
        {
            return (compare.X == this.X && compare.Y == this.Y);
        }

        public StringVector(string _x, string _y)
        {
            X = _x; Y = _y;
        }

        public StringVector(int _x, int _y)
        {
            X = _x + "px"; Y = _y + "px";
        }

        public StringVector(float _x, float _y)
        {
            X = _x + "px"; Y = _y + "px";
        }

        public StringVector(Vector2D<float> _size)
        {
            X = _size.X + "px"; Y = _size.Y + "px";
        }

        public StringVector(Vector2D<int> _size)
        {
            X = _size.X + "px"; Y = _size.Y + "px";
        }

        public static StringVector Zero
        {
            get
            {
                return new StringVector { X = "0px", Y = "0px" };
            }
        }

        public Vector2D<float> CalculateSize(Vector2D<float> ScreenSize, bool append = false)
        {
            var vec = new Vector2D<float>();

            vec.X = Calculate(X, ScreenSize.X, true, append);
            vec.Y = Calculate(Y, ScreenSize.Y, true, append);

            return vec;
        }

        public Vector2D<float> CalculateSize(Vector2D<int> ScreenSize, bool append = false)
        {
            return CalculateSize(new Vector2D<float>(ScreenSize.X, ScreenSize.Y), append);
        }

        private float Calculate(string obj, float screenSize, bool applySub = true, bool append = false)
        {
            float value = 0;
            if (obj.Contains(";"))
            {
                var objects = obj.Split(';');
                if (objects.Length > 0)
                {
                    float newValue = 0;
                    foreach (var newObj in objects)
                    {
                        newValue += Calculate(newObj, screenSize, false);
                    }

                    value = newValue;
                }
            }
            else if (obj.Contains("%"))
            {
                var percent = float.Parse(obj.Replace("%", ""));
                value = screenSize * (percent / 100f);
            }
            else if (obj.Contains("px"))
            {
                var px = float.Parse(obj.Replace("px", ""));
                value = (px < 0 && applySub) ? screenSize + px : px;

                if (append)
                {
                    value += screenSize;
                }
            }

            return value;
        }
    }

}
