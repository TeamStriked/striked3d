using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Graphics;
using System;
using Veldrid;

namespace Striked3D.Resources
{
    public class Transform3D : Resource, IDrawable3D
    {
        private Transform3DInfo info = new ();
        private Vector3D<float> _position = new (0, 0f, 0);
        private Quaternion<float> _rotation = new (0f, 0f, 0f, 1f);
        private Vector3D<float> _scale = new (0.1f, 0.1f, 0.1f);
        private Silk.NET.Maths.Matrix4X4<float> ModelMatrix { get; set; }
        private bool isInitialized = false;
        private bool isDirty = true;
        private DeviceBuffer modalBuffer;
        private ResourceSet _modalSet;
        public ResourceSet ModalSet => _modalSet;

        [Export]
        public Vector3D<float> Position
        {
            get { return _position; }
            set
            {
                var orig = _position;

                if (!orig.Equals(value))
                {
                    SetProperty("Position", ref _position, value);
                    this.Update();
                }
            }
        }

        [Export]
        public Vector3D<float> Scale
        {
            get { return _scale; }
            set
            {
                var orig = _scale;

                if (!orig.Equals(value))
                {
                    SetProperty("Scale", ref _scale, value);
                    this.Update();
                }
            }
        }

        [Export]
        public Quaternion<float> Rotation
        {
            get { return _rotation; }
            set
            {
                var orig = _rotation;

                if (!orig.Equals(value))
                {
                    SetProperty("Rotation", ref _rotation, value);
                    this.Update();
                }
            }
        }

        public bool IsVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IViewport IDrawable.Viewport => throw new NotImplementedException();

        public Transform3D() : base()
        {
            this.ModelMatrix = Matrix4X4<float>.Identity;
            this.Update();
        }

        private void Update()
        {
            this.ModelMatrix = Matrix4X4.CreateScale(this._scale) * Matrix4X4.CreateFromQuaternion<float>(this._rotation) * Matrix4X4.CreateTranslation<float>(_position);

            this.info.modelMatrix1 = this.ModelMatrix.Row1;
            this.info.modelMatrix2 = this.ModelMatrix.Row2;
            this.info.modelMatrix3 = this.ModelMatrix.Row3;
            this.info.modelMatrix4 = this.ModelMatrix.Row4;
            this.isDirty = true;
        }

        public static float DegreesToRadians(float degrees)
        {
            return degrees * (float)Math.PI / 180f;
        }

        public override void Dispose()
        {
            base.Dispose();

            this.modalBuffer?.Dispose();
            this._modalSet?.Dispose();

            this.modalBuffer = null;
        }


        public void BeforeDraw(IRenderer renderer)
        {
            if (!isInitialized)
            {
                this.modalBuffer = renderer.CreateBuffer(new BufferDescription
                (
                    Transform3DInfo.GetSizeInBytes(),
                    BufferUsage.UniformBuffer
                ));

                ResourceSetDescription resourceSetDescription = new (renderer.TransformLayout, this.modalBuffer);
                this._modalSet = renderer.CreateResourceSet(resourceSetDescription);

                isInitialized = true;
            }

            if (this.modalBuffer != null && this.isDirty)
            {
                renderer.UpdateBuffer(this.modalBuffer, 0, this.info);
            }
        }

        public void OnDraw3D(IRenderer renderer)
        {
        }
    }
}
