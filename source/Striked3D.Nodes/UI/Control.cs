using Striked3D.Core.Input;
using Striked3D.Math;
using Striked3D.Types;

namespace Striked3D.Nodes
{
    public abstract class Control : Canvas, IInputable
    {
        protected StringVector _Size = StringVector.Zero;
        protected StringVector _Position = StringVector.Zero;

        protected Vector2D<float> _screenSize = Vector2D<float>.Zero;
        protected Vector2D<float> _screenPosition = Vector2D<float>.Zero;
        protected Vector4D<int> _Padding = Vector4D<int>.Zero;

        public Vector2D<float> ScreenSize => _screenSize;
        public Vector2D<float> ScreenPosition => _screenPosition;
        public Vector2D<float> ScreenPostionEnd => _screenPosition + _screenSize;

        public delegate void OnClickHandler();
        public event OnClickHandler OnClick;

        public delegate void OnHoverHandler();
        public event OnHoverHandler OnHover;
        public event OnHoverHandler OnHoverLeave;

        public delegate void OnFocusHandler();
        public event OnFocusHandler OnFocus;
        public event OnFocusHandler OnFocusLeave;

        private bool _isHover = false;
        public bool isHover => _isHover;

        private bool _isFocused = false;
        public bool isFocused => _isFocused;

        public void SetFocus(bool focused)
        {
            _isFocused = focused;
        }

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            UpdateSizes();

            Root.OnResize += Root_OnResize;
        }

        private void Root_OnResize(Vector2D<int> newSize)
        {
            UpdateSizes();
        }

        public StringVector Size
        {
            get => _Size;
            set
            {
                StringVector orig = _Size;

                if (!orig.Equals(value))
                {
                    SetProperty("Size", ref _Size, value);
                    UpdateSizes();
                }
            }
        }

        public StringVector Position
        {
            get => _Position;
            set
            {
                StringVector orig = _Position;

                if (!orig.Equals(value))
                {
                    SetProperty("Position", ref _Position, value);
                    UpdateSizes();
                }
            }
        }

        public Vector4D<int> Padding
        {
            get => _Padding;
            set
            {
                Vector4D<int> orig = _Padding;

                if (!orig.Equals(value))
                {
                    SetProperty("Padding", ref _Padding, value);
                    UpdateSizes();
                }
            }
        }

        public virtual void UpdateSizes(bool withSubElements = true)
        {
            if (Root != null && IsEnterTree)
            {
                Control parent = GetParent<Control>();
                Vector2D<float> newPos = new Vector2D<float>(0, 0);
                Vector2D<float> newSize = new Vector2D<float>(0, 0);

                if (parent != null)
                {
                    newPos = _Position.CalculateSize(parent.ScreenPosition, true);
                    newSize = _Size.CalculateSize(parent.ScreenSize);
                }
                else if (Viewport != null)
                {
                    newPos = _Position.CalculateSize(Viewport.Position, true);
                    newSize = _Size.CalculateSize(Viewport.Size);
                }

                if (!_screenPosition.Equals(newPos) || !_screenSize.Equals(newSize))
                {
                    _screenPosition = newPos;
                    _screenSize = newSize;

                    UpdateCanvas();

                    if (withSubElements)
                    {
                        foreach (Control child in GetChilds<Control>())
                        {
                            child.UpdateSizes();
                        }
                    }
                }
            }
        }

        public virtual void OnInput(InputEvent ev)
        {
            if (ev is MouseInputEvent)
            {
                Vector2D<float> cursorPos = (ev as MouseInputEvent).Position;

                if (cursorPos.X >= ScreenPosition.X
                    && cursorPos.X <= ScreenPostionEnd.X
                    && cursorPos.Y >= ScreenPosition.Y
                    && cursorPos.Y <= ScreenPostionEnd.Y)
                {
                    if (_isHover != true)
                    {
                        _isHover = true;
                        OnHover?.Invoke();
                    }
                }
                else
                {
                    if (_isHover != false)
                    {
                        _isHover = false;
                        OnHoverLeave?.Invoke();
                    }
                }
            }

            if (ev is MouseButtonEvent)
            {
                MouseButtonEvent button = (ev as MouseButtonEvent);
                if (button.Button == Silk.NET.Input.MouseButton.Left && isHover && button.IsUp)
                {
                    OnClick?.Invoke();

                    if (_isFocused != true)
                    {
                        _isFocused = true;
                        OnFocus?.Invoke();
                    }
                }
                else
                {
                    if (_isFocused != false)
                    {
                        _isFocused = false;
                        OnFocusLeave?.Invoke();
                    }
                }
            }
        }
    }
}
