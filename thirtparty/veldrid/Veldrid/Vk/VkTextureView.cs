using static Veldrid.Vk.VulkanUtil;

namespace Veldrid.Vk
{
    internal unsafe class VkTextureView : TextureView
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.ImageView _imageView;
        private bool _destroyed;
        private string _name;

        public Silk.NET.Vulkan.ImageView ImageView => _imageView;

        public new VkTexture Target => (VkTexture)base.Target;

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => _destroyed;

        public VkTextureView(VkGraphicsDevice gd, ref TextureViewDescription description)
            : base(ref description)
        {
            _gd = gd;
            Silk.NET.Vulkan.ImageViewCreateInfo imageViewCI = new Silk.NET.Vulkan.ImageViewCreateInfo();
            imageViewCI.SType = Silk.NET.Vulkan.StructureType.ImageViewCreateInfo;

            VkTexture tex = Util.AssertSubtype<Texture, VkTexture>(description.Target);
            imageViewCI.Image = tex.OptimalDeviceImage;
            imageViewCI.Format = VkFormats.VdToVkPixelFormat(Format, (Target.Usage & TextureUsage.DepthStencil) != 0);

            Silk.NET.Vulkan.ImageAspectFlags aspectFlags;
            if ((description.Target.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil)
            {
                aspectFlags = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit;
            }
            else
            {
                aspectFlags = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit;
            }

            imageViewCI.SubresourceRange = new Silk.NET.Vulkan.ImageSubresourceRange(
                aspectFlags,
                description.BaseMipLevel,
                description.MipLevels,
                description.BaseArrayLayer,
                description.ArrayLayers);

            if ((tex.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap)
            {
                imageViewCI.ViewType = description.ArrayLayers == 1 ? Silk.NET.Vulkan.ImageViewType.Cube : Silk.NET.Vulkan.ImageViewType.CubeArray;
                imageViewCI.SubresourceRange.LayerCount *= 6;
            }
            else
            {
                switch (tex.Type)
                {
                    case TextureType.Texture1D:
                        imageViewCI.ViewType = description.ArrayLayers == 1
                            ? Silk.NET.Vulkan.ImageViewType.ImageViewType1D
                            : Silk.NET.Vulkan.ImageViewType.ImageViewType1DArray;
                        break;
                    case TextureType.Texture2D:
                        imageViewCI.ViewType = description.ArrayLayers == 1
                            ? Silk.NET.Vulkan.ImageViewType.ImageViewType2D
                            : Silk.NET.Vulkan.ImageViewType.ImageViewType2DArray;
                        break;
                    case TextureType.Texture3D:
                        imageViewCI.ViewType = Silk.NET.Vulkan.ImageViewType.ImageViewType3D;
                        break;
                }
            }

            gd.vk.CreateImageView(_gd.Device, &imageViewCI, null, out _imageView);
            RefCount = new ResourceRefCount(DisposeCore);
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                _gd.SetResourceName(this, value);
            }
        }

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        private void DisposeCore()
        {
            if (!_destroyed)
            {
                _destroyed = true;
                _gd.vk.DestroyImageView(_gd.Device, ImageView, null);
            }
        }
    }
}
