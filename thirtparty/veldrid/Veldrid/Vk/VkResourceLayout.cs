using static Veldrid.Vk.VulkanUtil;

namespace Veldrid.Vk
{
    internal unsafe class VkResourceLayout : ResourceLayout
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.DescriptorSetLayout _dsl;
        private readonly Silk.NET.Vulkan.DescriptorType[] _descriptorTypes;
        private bool _disposed;
        private string _name;

        public Silk.NET.Vulkan.DescriptorSetLayout DescriptorSetLayout => _dsl;
        public Silk.NET.Vulkan.DescriptorType[] DescriptorTypes => _descriptorTypes;
        public DescriptorResourceCounts DescriptorResourceCounts { get; }
        public new int DynamicBufferCount { get; }

        public override bool IsDisposed => _disposed;

        public VkResourceLayout(VkGraphicsDevice gd, ref ResourceLayoutDescription description)
            : base(ref description)
        {
            _gd = gd;
            Silk.NET.Vulkan.DescriptorSetLayoutCreateInfo dslCI = new Silk.NET.Vulkan.DescriptorSetLayoutCreateInfo();
            dslCI.SType = Silk.NET.Vulkan.StructureType.DescriptorSetLayoutCreateInfo;

            ResourceLayoutElementDescription[] elements = description.Elements;
            _descriptorTypes = new Silk.NET.Vulkan.DescriptorType[elements.Length];
            Silk.NET.Vulkan.DescriptorSetLayoutBinding* bindings = stackalloc Silk.NET.Vulkan.DescriptorSetLayoutBinding[elements.Length];

            uint uniformBufferCount = 0;
            uint sampledImageCount = 0;
            uint samplerCount = 0;
            uint storageBufferCount = 0;
            uint storageImageCount = 0;

            for (uint i = 0; i < elements.Length; i++)
            {
                bindings[i].Binding = i;
                bindings[i].DescriptorCount = 1;
                Silk.NET.Vulkan.DescriptorType descriptorType = VkFormats.VdToVkDescriptorType(elements[i].Kind, elements[i].Options);
                bindings[i].DescriptorType = descriptorType;
                bindings[i].StageFlags = VkFormats.VdToVkShaderStages(elements[i].Stages);
                if ((elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0)
                {
                    DynamicBufferCount += 1;
                }

                _descriptorTypes[i] = descriptorType;

                switch (descriptorType)
                {
                    case Silk.NET.Vulkan.DescriptorType.Sampler:
                        samplerCount += 1;
                        break;
                    case Silk.NET.Vulkan.DescriptorType.SampledImage:
                        sampledImageCount += 1;
                        break;
                    case Silk.NET.Vulkan.DescriptorType.StorageImage:
                        storageImageCount += 1;
                        break;
                    case Silk.NET.Vulkan.DescriptorType.UniformBuffer:
                        uniformBufferCount += 1;
                        break;
                    case Silk.NET.Vulkan.DescriptorType.StorageBuffer:
                        storageBufferCount += 1;
                        break;
                }
            }

            DescriptorResourceCounts = new DescriptorResourceCounts(
                uniformBufferCount,
                sampledImageCount,
                samplerCount,
                storageBufferCount,
                storageImageCount);

            dslCI.BindingCount = (uint)elements.Length;
            dslCI.PBindings = bindings;

            var result = _gd.vk.CreateDescriptorSetLayout(_gd.Device, &dslCI, null, out _dsl);
            CheckResult(result);
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
            if (!_disposed)
            {
                _disposed = true;
                _gd.vk.DestroyDescriptorSetLayout(_gd.Device, _dsl, null);
            }
        }
    }
}
