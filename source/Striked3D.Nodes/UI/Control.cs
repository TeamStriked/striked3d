using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Input;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

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
            this._isFocused = focused;
        }

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            UpdateSizes();

            this.Root.OnResize += Root_OnResize;
        }

        private void Root_OnResize(Vector2D<int> newSize)
        {
            UpdateSizes();
        }

        public StringVector Size
        {
            get { return _Size; }
            set
            {
                var orig = _Size;

                if (!orig.Equals(value))
                {
                    SetProperty("Size", ref _Size, value);
                    this.UpdateSizes(); 
                }
            }
        }

        public StringVector Position
        {
            get { return _Position; }
            set
            {
                var orig = _Position;

                if (!orig.Equals(value))
                {
                    SetProperty("Position", ref _Position, value);
                    this.UpdateSizes();
                }
            }
        }

        public Vector4D<int> Padding
        {
            get { return _Padding; }
            set
            {
                var orig = _Padding;

                if (!orig.Equals(value))
                {
                    SetProperty("Padding", ref _Padding, value);
                    this.UpdateSizes();
                }
            }
        }

        public void UpdateSize()
        {
            if (this.Root != null && this.IsEnterTree)
            {
                var parent = this.GetParent<Control>();
                if (parent != null)
                {
                    _screenPosition = this._Position.CalculateSize(parent.ScreenPosition, true);
                    _screenSize = this._Size.CalculateSize(parent.ScreenSize);
                }
                else if (this.Viewport != null)
                {
                    _screenPosition = this._Position.CalculateSize(this.Viewport.Position, true);
                    _screenSize = this._Size.CalculateSize(this.Viewport.Size);
                }
            }

            this.UpdateCanvas();
        }

        public virtual void UpdateSizes()
        {
            if (this.Root != null && this.IsEnterTree)
            {
                this.UpdateSize();

                foreach (var child in GetChilds<Control>())
                {
                    child.UpdateSizes();
                }
            }
        }

        public virtual void OnInput(InputEvent ev)
        {
            if(ev is MouseInputEvent)
            {
                var cursorPos = (ev as MouseInputEvent).Position;

                if(cursorPos.X >= this.ScreenPosition.X
                    && cursorPos.X <= this.ScreenPostionEnd.X 
                    && cursorPos.Y >= this.ScreenPosition.Y
                    && cursorPos.Y <= this.ScreenPostionEnd.Y)
                {
                    if(_isHover != true)
                    {
                        OnHover?.Invoke();
                    }
                    _isHover = true;
                }
                else
                {
                    if (_isHover != false)
                    {
                        OnHoverLeave?.Invoke();
                    }
                    _isHover = false;
                }
            }

            if(ev is MouseButtonEvent)
            {
                var button = (ev as MouseButtonEvent);
                if(button.Button == Silk.NET.Input.MouseButton.Left && isHover && button.IsUp)
                {
                    OnClick?.Invoke();
                    
                    if (_isFocused != true)
                    {
                        OnFocus?.Invoke();
                    }
                    this._isFocused = true;
                }
                else
                {
                    if (_isFocused != false)
                    {
                        OnFocusLeave?.Invoke();
                    }
                    this._isFocused = false;
                }
            }
        }
    }
}
