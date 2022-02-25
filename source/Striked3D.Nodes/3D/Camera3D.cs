using Striked3D.Math;
using Striked3D.Core;
using Striked3D.Core.Input;
using Striked3D.Types;
using System;

namespace Striked3D.Nodes
{
    public class Camera3D : Node3D, ICamera, IInputable
    {
        private float _fov = 45f;
        private float _near = 0.01f;
        private float _far = 100000f;

        private Matrix4X4<float> _viewMatrix = Matrix4X4<float>.Identity;
        private Matrix4X4<float> _projectionMatrix = Matrix4X4<float>.Identity;

        private Vector3D<float> _position = new Vector3D<float>(0, 3, 0);
        private Vector3D<float> _lookDirection = new Vector3D<float>(0, -3f, -1f);
        private float _moveSpeed = 5f;
        private readonly float _mouseSpeed = 0.003f;

        public Matrix4X4<float> ViewMatrix => _viewMatrix;
        public Matrix4X4<float> ProjectionMatrix => _projectionMatrix;

        private float _yaw;
        private float _pitch;

        public Vector3D<float> Position { get => _position; set { _position = value; UpdateCamera(); } }

        public float FarDistance { get => _far; set { _far = value; UpdateCamera(); } }
        public float FieldOfView { get => _fov; set { _fov = value; UpdateCamera(); } }
        public float NearDistance { get => _near; set { _near = value; UpdateCamera(); } }

        public float Yaw { get => _yaw; set { _yaw = value; UpdateCamera(); } }
        public float Pitch { get => _pitch; set { _pitch = value; UpdateCamera(); } }
        public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }

        private InputService service;
        private Vector2D<float> _previousMousePos = Vector2D<float>.Zero;

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            UpdateCamera();
            service = Root.Services.Get<InputService>();
        }

        private float DegreesToRadians(float degrees)
        {
            return degrees * (float)System.Math.PI / 180f;
        }

        public override void Update(double delta)
        {
            base.Update(delta);

            float sprintFactor = service.IsKeyPressed(Silk.NET.Input.Key.ControlLeft)
                ? 0.1f
                : service.IsKeyPressed(Silk.NET.Input.Key.ShiftLeft)
                    ? 2.5f
                    : 1f;

            Vector3D<float> motionDir = Vector3D<float>.Zero;
            if (service.IsKeyPressed(Silk.NET.Input.Key.A))
            {
                motionDir += -Vector3D<float>.UnitX;
            }
            if (service.IsKeyPressed(Silk.NET.Input.Key.D))
            {
                motionDir += Vector3D<float>.UnitX;
            }
            if (service.IsKeyPressed(Silk.NET.Input.Key.W))
            {
                motionDir += -Vector3D<float>.UnitZ;
            }
            if (service.IsKeyPressed(Silk.NET.Input.Key.S))
            {
                motionDir += Vector3D<float>.UnitZ;
            }
            if (service.IsKeyPressed(Silk.NET.Input.Key.Q))
            {
                motionDir += -Vector3D<float>.UnitY;
            }
            if (service.IsKeyPressed(Silk.NET.Input.Key.E))
            {
                motionDir += Vector3D<float>.UnitY;
            }

            if (motionDir != Vector3D<float>.Zero)
            {
                Quaternion<float> lookRotation = Quaternion<float>.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
                motionDir = Vector3D.Transform<float>(motionDir, lookRotation);
                _position += motionDir * MoveSpeed * sprintFactor * (float)delta;

                UpdateCamera();
            }

            if (Viewport.IsMouseInside() && service.IseMouseButtonPressed(Silk.NET.Input.MouseButton.Right) && !activated)
            {
                activated = true;
                _previousMousePos = service.GetMousePosition();
                mousePositionBeforeActivate = _previousMousePos;
                service.SetCursorMode(Silk.NET.Input.CursorMode.Raw);
            }
            else if (activated && service.IseMouseButtonPressed(Silk.NET.Input.MouseButton.Right))
            {
                Vector2D<float> mousePos = service.GetMousePosition();
                Vector2D<float> mouseDelta = mousePos - _previousMousePos;

                _previousMousePos = mousePos;

                Yaw += -mouseDelta.X * _mouseSpeed;
                Pitch += -mouseDelta.Y * _mouseSpeed;
                Pitch = Clamp(Pitch, -1.55f, 1.55f);

                UpdateCamera();
            }
            else if (activated)
            {
                activated = false;
                service.SetMousePosition(mousePositionBeforeActivate);
                service.SetCursorMode(Silk.NET.Input.CursorMode.Normal);
            }

        }
        private Vector2D<float> mousePositionBeforeActivate;
        private bool activated = false;

        public void UpdateCamera()
        {
            Quaternion<float> lookRotation = Quaternion<float>.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
            Vector3D<float> lookDir = Vector3D.Transform(-Vector3D<float>.UnitZ, lookRotation);

            _lookDirection = lookDir;
            _viewMatrix = Matrix4X4.CreateLookAt<float>(_position, _position + _lookDirection, Vector3D<float>.UnitY);
            if (Viewport.Size.Y > 0)
            {
                _projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>(DegreesToRadians(FieldOfView), Viewport.Size.X / Viewport.Size.Y, _near, _far);
            }
        }

        public bool IsActive
        {
            get => (Viewport.ActiveCamera != null && Viewport.ActiveCamera.Id == Id);
            set => Viewport.ActiveCamera = this;
        }

        private float Clamp(float value, float min, float max)
        {
            return value > max
                ? max
                : value < min
                    ? min
                    : value;
        }

        public CameraInfo CameraInfo => new CameraInfo
        {
            projectionMatrix0 = _projectionMatrix.Row1,
            projectionMatrix1 = _projectionMatrix.Row2,
            projectionMatrix2 = _projectionMatrix.Row3,
            projectionMatrix3 = _projectionMatrix.Row4,

            viewMatrix0 = _viewMatrix.Row1,
            viewMatrix1 = _viewMatrix.Row2,
            viewMatrix2 = _viewMatrix.Row3,
            viewMatrix3 = _viewMatrix.Row4,

            far = _far,
            near = _near,
            fov = _fov,
        };

        public override void Dispose()
        {
        }

        public void OnInput(InputEvent e)
        {
            if (e is MouseInputWheelEvent)
            {
                MouseInputWheelEvent ev = e as MouseInputWheelEvent;
                if (ev.Position.Y >= 1)
                {
                    MoveSpeed = Clamp(MoveSpeed * 1.1f, 0.01f, 100f);
                }
                else if (ev.Position.Y <= -1)
                {
                    MoveSpeed = Clamp(MoveSpeed / 1.1f, 0.01f, 100f);
                }
            }
        }
    }

}
