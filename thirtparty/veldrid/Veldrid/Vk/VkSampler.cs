
namespace Veldrid.Vk
{
    internal unsafe class VkSampler : Sampler
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.Sampler _sampler;
        private bool _disposed;
        private string _name;

        public Silk.NET.Vulkan.Sampler DeviceSampler => _sampler;

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => _disposed;

        public VkSampler(VkGraphicsDevice gd, ref SamplerDescription description)
        {
            _gd = gd;
            VkFormats.GetFilterParams(description.Filter, out Silk.NET.Vulkan.Filter minFilter, out Silk.NET.Vulkan.Filter magFilter, out Silk.NET.Vulkan.SamplerMipmapMode mipmapMode);

            Silk.NET.Vulkan.SamplerCreateInfo samplerCI = new Silk.NET.Vulkan.SamplerCreateInfo
            {
                SType = Silk.NET.Vulkan.StructureType.SamplerCreateInfo,
                AddressModeU = VkFormats.VdToVkSamplerAddressMode(description.AddressModeU),
                AddressModeV = VkFormats.VdToVkSamplerAddressMode(description.AddressModeV),
                AddressModeW = VkFormats.VdToVkSamplerAddressMode(description.AddressModeW),
                MinFilter = minFilter,
                MagFilter = magFilter,
                MipmapMode = mipmapMode,
                CompareEnable = description.ComparisonKind != null,
                CompareOp = description.ComparisonKind != null
                    ? VkFormats.VdToVkCompareOp(description.ComparisonKind.Value)
                    : Silk.NET.Vulkan.CompareOp.Never,
                AnisotropyEnable = description.Filter == SamplerFilter.Anisotropic,
                MaxAnisotropy = description.MaximumAnisotropy,
                MinLod = description.MinimumLod,
                MaxLod = description.MaximumLod,
                MipLodBias = description.LodBias,
                BorderColor = VkFormats.VdToVkSamplerBorderColor(description.BorderColor)
            };
            samplerCI.SType = Silk.NET.Vulkan.StructureType.SamplerCreateInfo;

            gd.vk.CreateSampler(_gd.Device, &samplerCI, null, out _sampler);
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
            if (!_disposed)
            {
                _gd.vk.DestroySampler(_gd.Device, _sampler, null);
                _disposed = true;
            }
        }
    }
}
