using Striked3D.Core;
using Striked3D.Graphics;
using Striked3D.Types;
using System;
using Veldrid;

namespace Striked3D.Resources
{
    public class Transform3D : Resource, IDrawable3D
    {
        private Transform3DInfo info = new();
        private Vector3D<float> _position = new(0, 0f, 0);
        private Quaternion<float> _rotation = new(0f, 0f, 0f, 1f);
        private Vector3D<float> _scale = new(0.1f, 0.1f, 0.1f);
        private Striked3D.Types.Matrix4X4<float> ModelMatrix { get; set; }
        private bool isInitialized = false;
        private bool isDirty = true;
        private DeviceBuffer modalBuffer;
        private ResourceSet _modalSet;
        public ResourceSet ModalSet => _modalSet;

        [Export]
        public Vector3D<float> Position
        {
            get => _position;
            set
            {
                Vector3D<float> orig = _position;

                if (!orig.Equals(value))
                {
                    SetProperty("Position", ref _position, value);
                    Update();
                }
            }
        }

        [Export]
        public Vector3D<float> Scale
        {
            get => _scale;
            set
            {
                Vector3D<float> orig = _scale;

                if (!orig.Equals(value))
                {
                    SetProperty("Scale", ref _scale, value);
                    Update();
                }
            }
        }

        [Export]
        public Quaternion<float> Rotation
        {
            get => _rotation;
            set
            {
                Quaternion<float> orig = _rotation;

                if (!orig.Equals(value))
                {
                    SetProperty("Rotation", ref _rotation, value);
                    Update();
                }
            }
        }

        public bool IsVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IViewport IDrawable.Viewport => throw new NotImplementedException();

        public Transform3D() : base()
        {
            ModelMatrix = Matrix4X4<float>.Identity;
            Update();
        }

        private void Update()
        {
            ModelMatrix = Matrix4X4.CreateScale(_scale) * Matrix4X4.CreateFromQuaternion<float>(_rotation) * Matrix4X4.CreateTranslation<float>(_position);

            info.modelMatrix1 = ModelMatrix.Row1;
            info.modelMatrix2 = ModelMatrix.Row2;
            info.modelMatrix3 = ModelMatrix.Row3;
            info.modelMatrix4 = ModelMatrix.Row4;

            isDirty = true;
        }

        public static float DegreesToRadians(float degrees)
        {
            return degrees * (float)Math.PI / 180f;
        }

        public override void Dispose()
        {
            base.Dispose();

            modalBuffer?.Dispose();
            _modalSet?.Dispose();

            modalBuffer = null;
        }


        public void BeforeDraw(IRenderer renderer)
        {
            if (!isInitialized)
            {
                modalBuffer = renderer.CreateBuffer(new BufferDescription
                (
                    Transform3DInfo.GetSizeInBytes(),
                    BufferUsage.UniformBuffer
                ));

                ResourceSetDescription resourceSetDescription = new(renderer.TransformLayout, modalBuffer);
                _modalSet = renderer.CreateResourceSet(resourceSetDescription);

                isInitialized = true;
            }

            if (modalBuffer != null && isDirty)
            {
                renderer.UpdateBuffer(modalBuffer, 0, info);
                isDirty = false;

            }
        }

        public void OnDraw3D(IRenderer renderer)
        {
        }
    }
}
