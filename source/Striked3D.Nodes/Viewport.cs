using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Input;
using Striked3D.Graphics;
using Striked3D.Math;
using Striked3D.Types;

namespace Striked3D.Nodes
{
    public class Viewport : Node, IViewport, IDrawable3D, IDrawable2D
    {
        private bool _isVisible = true;

        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        private ICamera _activeCamera { get; set; }

        private readonly bool _enable2D = true;
        private readonly bool _enable3D = true;
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
            get => _Size;
            set
            {
                _Size = value;
                _isDirty = true;
            }
        }

        [Export]
        public Vector2D<float> Position
        {
            get => _Position;
            set
            {
                _Position = value;
                _isDirty = true;
            }
        }

        public ICamera ActiveCamera
        {
            get => _activeCamera;
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
            inputService = Root.Services.Get<InputService>();
        }


        public void OnDraw2D(IRenderer renderer)
        {

        }

        public Vector2D<float> GetMousePosition()
        {
            Vector2D<float> currentPos = inputService.GetMousePosition();
            Vector2D<float> viewportPosition = Position;
            Vector2D<float> viewportSize = Size;

            return currentPos - viewportPosition;
        }

        public bool IsMouseInside()
        {
            Vector2D<float> currentPos = inputService.GetMousePosition();
            Vector2D<float> viewportEnd = Position + Size;

            if (currentPos.X >= Position.X && currentPos.Y >= Position.Y && currentPos.X < viewportEnd.X && currentPos.Y < viewportEnd.Y)
            {
                return true;
            }

            return false;
        }

        public void BeforeDraw(IRenderer renderer)
        {
            if (Size == Vector2D<float>.Zero)
            {
                return;
            }

            if (isDirty)
            {
                if (ActiveCamera != null)
                {
                    ActiveCamera.UpdateCamera();
                }

                _isDirty = false;
            }

            if (ActiveCamera != null)
            {
                _world3D.Update(renderer, this);
            }

            _world2D.Update(renderer, this);
        }
    }
}
