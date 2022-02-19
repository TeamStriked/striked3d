using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Input;
using Striked3D.Graphics;

namespace Striked3D.Nodes
{
    public class Viewport : Node, IViewport, IDrawable3D, IDrawable2D
    {
        private bool _isVisible = true;

        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        private ICamera _activeCamera { get; set; }

        private bool _enable2D = true;
        private bool _enable3D = true;
        public bool Enable3D => _enable3D;
        public bool Enable2D => _enable2D;

        protected bool _isDirty = true;
        public bool isDirty => _isDirty;


        protected World3D _world3D = new World3D();
        public IWorld World3D => _world3D;

        protected World2D _world2D = new World2D();
        public IWorld World2D => _world2D;

        private Vector2D<float> _Size = Vector2D<float>.Zero;

        private Vector2D<float> _Position = Vector2D<float>.Zero;

        [Export]
        public Vector2D<float> Size
        {
            get
            {
                return this._Size;
            }
            set
            {
                this._Size = value;
                _isDirty = true;
            }
        }

        [Export]
        public Vector2D<float> Position
        {
            get
            {
                return this._Position;
            }
            set
            {
                this._Position = value;
                _isDirty = true;
            }
        }

        public ICamera ActiveCamera
        {
            get
            {
                return _activeCamera;
            }
            set
            {
                if (value?.Id != _activeCamera?.Id)
                {
                    _activeCamera = value;
                    _isDirty = true;
                }
            }
        }

        public override void Dispose()
        {
            _world2D?.Dispose();
            _world3D?.Dispose();
        }

        public void OnDraw3D(IRenderer renderer)
        {

        }

        private InputService inputService;

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            inputService = this.Root.Services.Get<InputService>();
        }


        public void OnDraw2D(IRenderer renderer)
        {

        }

        public Vector2D<float> GetMousePosition()
        {
            var currentPos = this.inputService.GetMousePosition();
            var viewportPosition = this.Position;
            var viewportSize = this.Size;

            return currentPos - viewportPosition;
        }

        public bool IsMouseInside()
        {
            var currentPos = this.inputService.GetMousePosition();
            var viewportEnd = this.Position + this.Size;

            if (currentPos.X >= this.Position.X && currentPos.Y >= this.Position.Y && currentPos.X < viewportEnd.X && currentPos.Y < viewportEnd.Y)
                return true;

            return false;
        }

        public void BeforeDraw(IRenderer renderer)
        {
            if (this.Size == Vector2D<float>.Zero)
            {
                return;
            }

            if (this.isDirty)
            {
                if (this.ActiveCamera != null)
                {
                    this.ActiveCamera.UpdateTransform();
                }

                this._isDirty = false;
            }

            if (this.ActiveCamera != null)
            {
                this._world3D.Update(renderer, this as IViewport);
            }

            this._world2D.Update(renderer, this as IViewport);
        }
    }
}
