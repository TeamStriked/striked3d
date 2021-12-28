using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Striked3D.Core;
using Striked3D.Servers.Rendering;
using Striked3D.Types;

namespace Striked3D.Nodes
{
    public class Camera : BaseNode
    {
        Guid cameraId;
        Servers.CameraServer cameraServer = null;
        Servers.InputServer inputServer = null;

        private Vector3D<float> CameraPosition = new Vector3D<float>(0.0f, 0.0f, 3.0f);
        private  Vector3D<float> CameraFront = new Vector3D<float>(0.0f, 0.0f, -1.0f);
        private  Vector3D<float> CameraUp = Vector3D<float>.UnitY;
        private  Vector3D<float> CameraDirection = Vector3D<float>.Zero;
        private  float CameraYaw = -90f;
        private  float CameraPitch = 0f;
        private float CameraZoom = 45f;
        private float near = 0.01f;
        private float far = 1000f;
        private Vector2D<float> LastMousePosition;

        public override void OnEnterTree()
        {
            base.OnEnterTree();

            cameraServer = window.GetService<Servers.CameraServer>();
            cameraServer.CreateCamera(SetCamera);

            inputServer = window.GetService<Servers.InputServer>();
        }

        public override void Render(double delta)
        {
            if (cameraId != Guid.Empty)
            {
                cameraServer.SetView(cameraId, CameraPosition, CameraFront, CameraUp);
                cameraServer.SetPerspective(cameraId, CameraZoom,(float) this.window.Size.X / this.window.Size.Y, near, far);
            }
        }
        public override void Update(double delta)
        {
            var moveSpeed = 2.5f * (float)delta;
            if (inputServer.PrimaryKeyboard.IsKeyPressed(Key.W))
            {
                //Move forwards
                CameraPosition += moveSpeed * CameraFront;
            }
            if (inputServer.PrimaryKeyboard.IsKeyPressed(Key.S))
            {
                //Move backwards
                CameraPosition -= moveSpeed * CameraFront;
            }
            if (inputServer.PrimaryKeyboard.IsKeyPressed(Key.A))
            {
                //Move left
                CameraPosition -= Vector3D.Normalize(Vector3D.Cross(CameraFront, CameraUp)) * moveSpeed;
            }
            if (inputServer.PrimaryKeyboard.IsKeyPressed(Key.D))
            {
                //Move right
                CameraPosition += Vector3D.Normalize(Vector3D.Cross(CameraFront, CameraUp)) * moveSpeed;
            }
        }
        private float DegreesToRadians(float degrees)
        {
            return (float)(Math.PI / 180f) * degrees;
        }

        public bool canMove = false;
        public override void OnInput(InputEvent e)
        {
            if(e is MouseButtonEvent)
            {
                var ev = e as MouseButtonEvent;
                canMove = !ev.IsUp;
            }

            if (!canMove)
            {
                LastMousePosition = default;
                return;
            }

            if (e is MouseInputEvent)
            {
                var ev = e as MouseInputEvent;
                Logger.Debug("Mouse input event");

                var lookSensitivity = 0.1f;
                if (LastMousePosition == default) { LastMousePosition = ev.Position; }
                else
                {
                    var xOffset = (ev.Position.X - LastMousePosition.X) * lookSensitivity;
                    var yOffset = (ev.Position.Y - LastMousePosition.Y) * lookSensitivity;
                    LastMousePosition = ev.Position;

                    CameraYaw += xOffset;
                    CameraPitch += yOffset;

                    //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
                    CameraPitch = Math.Clamp(CameraPitch, -89.0f, 89.0f);

                    CameraDirection.X = MathF.Cos(DegreesToRadians(CameraYaw)) * MathF.Cos(DegreesToRadians(CameraPitch));
                    CameraDirection.Y = MathF.Sin(DegreesToRadians(CameraPitch));
                    CameraDirection.Z = MathF.Sin(DegreesToRadians(CameraYaw)) * MathF.Cos(DegreesToRadians(CameraPitch));
                    CameraFront = Vector3D.Normalize(CameraDirection);
                }
            }
            else if (e is MouseInputWheelEvent)
            {
                var ev = e as MouseInputWheelEvent;
                Logger.Debug("Mouse input wheel event");
                CameraZoom = Math.Clamp(CameraZoom - ev.Position.Y, 1.0f, 45f);
            }
        }

        private void SetCamera(EngineCommandResult res)
        {
            cameraId = (Guid)res.result;
            Logger.Debug(this, cameraId.ToString());
        }
    }
}
