using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Nodes
{
    public enum UIPanelDirection
    {
        HORIZONTAL,
        VERTICAL
    }

    public class LayoutGrid : Control
    {
        protected UIPanelDirection _Direction = UIPanelDirection.VERTICAL;
        protected bool _AutoHeight = false;
        protected bool _AutoGrid = false;
        public RgbaFloat _BackgroundColor = RgbaFloat.Clear;
        public RgbaFloat BackgroundColor
        {
            get { return _BackgroundColor; }
            set
            {
                SetProperty("BackgroundColor", ref _BackgroundColor, value);
                this.UpdateCanvas();
            }
        }
        public UIPanelDirection Direction
        {
            get { return _Direction; }
            set
            {
                SetProperty("Direction", ref _Direction, value);
                this.AdjustChilds();
            }
        }

        public bool AutoHeight
        {
            get { return _AutoHeight; }
            set
            {
                SetProperty("AutoHeight", ref _AutoHeight, value);
                this.AdjustChilds();
            }
        }

        public bool AutoGrid
        {
            get { return _AutoGrid; }
            set
            {
                SetProperty("AutoGrid", ref _AutoGrid, value);
                this.AdjustChilds();
            }
        }

        public override void DrawCanvas()
        {
            if(BackgroundColor.A > 0)
            {
                this.DrawRect(BackgroundColor, ScreenPosition, ScreenSize);
            }
        }

        public override void Update(double delta)
        {
            this.AdjustChilds();
        }

        public void AdjustChilds()
        {
            if (this.Root == null)
                return;

            float height = 0;
            float width = 0;
            float highestValue = 0;

            var elements = GetChilds<Control>();
            for (int i = 0; i < elements.Length; i++)
            {
                var child = elements[i];
                var widthPerElement = this._screenSize.X / elements.Length;
                var heightPerElement = this._screenSize.Y / elements.Length;

                var newPosition = new StringVector(0, 0);
                var newSize = new StringVector(0, 0);
                var oldSize = child.Size;
                var oldPosition = child.Position;

                if (this.Direction == UIPanelDirection.VERTICAL)
                {
                    newPosition = new StringVector(0, height);

                    if (this.AutoGrid)
                    {
                        newSize = new StringVector("100%", heightPerElement + "px");
                    }
                    else
                        newSize = new StringVector("100%", oldSize.Y);
                }
                else if (this.Direction == UIPanelDirection.HORIZONTAL)
                {
                    newPosition = new StringVector(width, 0);

                    if(this.AutoGrid)
                    {
                        newSize = new StringVector(widthPerElement+"px", "100%");
                    }
                    else
                        newSize = new StringVector(oldSize.X, "100%");
                }

                if (!oldPosition.Equals(newPosition))
                {
                    child.Position = newPosition;
                }

                if (!oldSize.Equals(newSize))
                {
                    child.Size = newSize;
                }
              
                var newScreenSize = child.ScreenSize;
                if (newScreenSize.Y > highestValue)
                {
                    highestValue = newScreenSize.Y;
                }

                height += newScreenSize.Y;
                width += newScreenSize.X;

                if (this.Direction == UIPanelDirection.VERTICAL)
                {
                    child.IsVisible = (height <= this.ScreenSize.Y);
                }
                else if (this.Direction == UIPanelDirection.HORIZONTAL)
                {
                    child.IsVisible = (width <= this.ScreenSize.X);
                }
            }

            if(this.AutoHeight)
            {
                if (this.Direction == UIPanelDirection.HORIZONTAL)
                {
                    this._screenSize = new Vector2D<float>(this._screenSize.X, highestValue);
                }
                else if (this.Direction == UIPanelDirection.VERTICAL)
                    this._screenSize = new Vector2D<float>(this._screenSize.X, height);
            }
        }
    }
}
