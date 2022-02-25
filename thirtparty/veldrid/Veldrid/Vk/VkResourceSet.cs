using System.Collections.Generic;
using static Veldrid.Vk.VulkanUtil;

namespace Veldrid.Vk
{
    internal unsafe class VkResourceSet : ResourceSet
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.Vk _vk;
        private readonly DescriptorResourceCounts _descriptorCounts;
        private readonly DescriptorAllocationToken _descriptorAllocationToken;
        private readonly List<ResourceRefCount> _refCounts = new List<ResourceRefCount>();
        private bool _destroyed;
        private string _name;

        public Silk.NET.Vulkan.DescriptorSet DescriptorSet => _descriptorAllocationToken.Set;

        private readonly List<VkTexture> _sampledTextures = new List<VkTexture>();
        public List<VkTexture> SampledTextures => _sampledTextures;
        private readonly List<VkTexture> _storageImages = new List<VkTexture>();
        public List<VkTexture> StorageTextures => _storageImages;

        public ResourceRefCount RefCount { get; }
        public List<ResourceRefCount> RefCounts => _refCounts;

        public override bool IsDisposed => _destroyed;

        public VkResourceSet(VkGraphicsDevice gd, ref ResourceSetDescription description)
            : base(ref description)
        {
            _gd = gd;
            _vk = gd.vk;

            RefCount = new ResourceRefCount(DisposeCore);
            VkResourceLayout vkLayout = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(description.Layout);

            Silk.NET.Vulkan.DescriptorSetLayout dsl = vkLayout.DescriptorSetLayout;
            _descriptorCounts = vkLayout.DescriptorResourceCounts;
            _descriptorAllocationToken = _gd.DescriptorPoolManager.Allocate(_descriptorCounts, dsl);

            BindableResource[] boundResources = description.BoundResources;
            uint descriptorWriteCount = (uint)boundResources.Length;
            Silk.NET.Vulkan.WriteDescriptorSet* descriptorWrites = stackalloc Silk.NET.Vulkan.WriteDescriptorSet[(int)descriptorWriteCount];
            Silk.NET.Vulkan.DescriptorBufferInfo* bufferInfos = stackalloc Silk.NET.Vulkan.DescriptorBufferInfo[(int)descriptorWriteCount];
            Silk.NET.Vulkan.DescriptorImageInfo* imageInfos = stackalloc Silk.NET.Vulkan.DescriptorImageInfo[(int)descriptorWriteCount];

            for (int i = 0; i < descriptorWriteCount; i++)
            {
                Silk.NET.Vulkan.DescriptorType type = vkLayout.DescriptorTypes[i];

                descriptorWrites[i].SType = Silk.NET.Vulkan.StructureType.WriteDescriptorSet;
                descriptorWrites[i].DescriptorCount = 1;
                descriptorWrites[i].DescriptorType = type;
                descriptorWrites[i].DstBinding = (uint)i;
                descriptorWrites[i].DstSet = _descriptorAllocationToken.Set;

                if (type == Silk.NET.Vulkan.DescriptorType.UniformBuffer || type == Silk.NET.Vulkan.DescriptorType.UniformBufferDynamic
                    || type == Silk.NET.Vulkan.DescriptorType.StorageBuffer || type == Silk.NET.Vulkan.DescriptorType.StorageBufferDynamic)
                {
                    DeviceBufferRange range = Util.GetBufferRange(boundResources[i], 0);
                    VkBuffer rangedVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(range.Buffer);
                    bufferInfos[i].Buffer = rangedVkBuffer.DeviceBuffer;
                    bufferInfos[i].Offset = range.Offset;
                    bufferInfos[i].Range = range.SizeInBytes;
                    descriptorWrites[i].PBufferInfo = &bufferInfos[i];
                    _refCounts.Add(rangedVkBuffer.RefCount);
                }
                else if (type == Silk.NET.Vulkan.DescriptorType.SampledImage)
                {
                    TextureView texView = Util.GetTextureView(_gd, boundResources[i]);
                    VkTextureView vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                    imageInfos[i].ImageView = vkTexView.ImageView;
                    imageInfos[i].ImageLayout = Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal;
                    descriptorWrites[i].PImageInfo = &imageInfos[i];
                    _sampledTextures.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                    _refCounts.Add(vkTexView.RefCount);
                }
                else if (type == Silk.NET.Vulkan.DescriptorType.StorageImage)
                {
                    TextureView texView = Util.GetTextureView(_gd, boundResources[i]);
                    VkTextureView vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                    imageInfos[i].ImageView = vkTexView.ImageView;
                    imageInfos[i].ImageLayout = Silk.NET.Vulkan.ImageLayout.General;
                    descriptorWrites[i].PImageInfo = &imageInfos[i];
                    _storageImages.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                    _refCounts.Add(vkTexView.RefCount);
                }
                else if (type == Silk.NET.Vulkan.DescriptorType.Sampler)
                {
                    VkSampler sampler = Util.AssertSubtype<BindableResource, VkSampler>(boundResources[i]);
                    imageInfos[i].Sampler = sampler.DeviceSampler;
                    descriptorWrites[i].PImageInfo = &imageInfos[i];
                    _refCounts.Add(sampler.RefCount);
                }
            }

            _vk.UpdateDescriptorSets(_gd.Device, descriptorWriteCount, descriptorWrites, 0, null);
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
                _gd.DescriptorPoolManager.Free(_descriptorAllocationToken, _descriptorCounts);
            }
        }
    }
}
