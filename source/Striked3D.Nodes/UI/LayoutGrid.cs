using Striked3D.Types;
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
            get => _BackgroundColor;
            set
            {
                SetProperty("BackgroundColor", ref _BackgroundColor, value);
                UpdateCanvas();
            }
        }
        public UIPanelDirection Direction
        {
            get => _Direction;
            set
            {
                SetProperty("Direction", ref _Direction, value);
                AdjustChilds();
            }
        }

        public bool AutoHeight
        {
            get => _AutoHeight;
            set
            {
                SetProperty("AutoHeight", ref _AutoHeight, value);
                AdjustChilds();
            }
        }

        public bool AutoGrid
        {
            get => _AutoGrid;
            set
            {
                SetProperty("AutoGrid", ref _AutoGrid, value);
                AdjustChilds();
            }
        }

        public override void DrawCanvas()
        {
            if (BackgroundColor.A > 0)
            {
                DrawRect(BackgroundColor, ScreenPosition, ScreenSize);
            }
        }

        public override void Update(double delta)
        {
            AdjustChilds();
        }

        public void AdjustChilds()
        {
            if (Root == null)
            {
                return;
            }

            float height = 0;
            float width = 0;
            float highestValue = 0;

            Control[] elements = GetChilds<Control>();
            for (int i = 0; i < elements.Length; i++)
            {
                Control child = elements[i];
                float widthPerElement = _screenSize.X / elements.Length;
                float heightPerElement = _screenSize.Y / elements.Length;

                StringVector newPosition = new StringVector(0, 0);
                StringVector newSize = new StringVector(0, 0);
                StringVector oldSize = child.Size;
                StringVector oldPosition = child.Position;

                if (Direction == UIPanelDirection.VERTICAL)
                {
                    newPosition = new StringVector(0, height);

                    if (AutoGrid)
                    {
                        newSize = new StringVector("100%", heightPerElement + "px");
                    }
                    else
                    {
                        newSize = new StringVector("100%", oldSize.Y);
                    }
                }
                else if (Direction == UIPanelDirection.HORIZONTAL)
                {
                    newPosition = new StringVector(width, 0);

                    if (AutoGrid)
                    {
                        newSize = new StringVector(widthPerElement + "px", "100%");
                    }
                    else
                    {
                        newSize = new StringVector(oldSize.X, "100%");
                    }
                }

                if (!oldPosition.Equals(newPosition))
                {
                    child.Position = newPosition;
                }

                if (!oldSize.Equals(newSize))
                {
                    child.Size = newSize;
                }

                Vector2D<float> newScreenSize = child.ScreenSize;
                if (newScreenSize.Y > highestValue)
                {
                    highestValue = newScreenSize.Y;
                }

                height += newScreenSize.Y;
                width += newScreenSize.X;

                if (Direction == UIPanelDirection.VERTICAL)
                {
                    child.IsVisible = (height <= ScreenSize.Y);
                }
                else if (Direction == UIPanelDirection.HORIZONTAL)
                {
                    child.IsVisible = (width <= ScreenSize.X);
                }
            }

            if (AutoHeight)
            {
                if (Direction == UIPanelDirection.HORIZONTAL)
                {
                    _screenSize = new Vector2D<float>(_screenSize.X, highestValue);
                }
                else if (Direction == UIPanelDirection.VERTICAL)
                {
                    _screenSize = new Vector2D<float>(_screenSize.X, height);
                }
            }
        }
    }
}
