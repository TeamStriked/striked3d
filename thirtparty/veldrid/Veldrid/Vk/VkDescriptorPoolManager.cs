using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Veldrid.Vk
{
    internal class VkDescriptorPoolManager
    {
        private readonly VkGraphicsDevice _gd;
        private readonly List<PoolInfo> _pools = new List<PoolInfo>();
        private readonly object _lock = new object();

        public VkDescriptorPoolManager(VkGraphicsDevice gd)
        {
            _gd = gd;
            _pools.Add(CreateNewPool());
        }

        public unsafe DescriptorAllocationToken Allocate(DescriptorResourceCounts counts, Silk.NET.Vulkan.DescriptorSetLayout setLayout)
        {
            lock (_lock)
            {
                Silk.NET.Vulkan.DescriptorPool pool = GetPool(counts);
                Silk.NET.Vulkan.DescriptorSetAllocateInfo dsAI = new Silk.NET.Vulkan.DescriptorSetAllocateInfo();
                dsAI.SType = Silk.NET.Vulkan.StructureType.DescriptorSetAllocateInfo;

                dsAI.DescriptorSetCount = 1;
                dsAI.PSetLayouts = &setLayout;
                dsAI.DescriptorPool = pool;
                var result = _gd.vk.AllocateDescriptorSets(_gd.Device, &dsAI, out Silk.NET.Vulkan.DescriptorSet set);
                VulkanUtil.CheckResult(result);

                return new DescriptorAllocationToken(set, pool);
            }
        }

        public void Free(DescriptorAllocationToken token, DescriptorResourceCounts counts)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Pool.Handle == token.Pool.Handle)
                    {
                        poolInfo.Free(_gd.vk, _gd.Device, token, counts);
                    }
                }
            }
        }

        private Silk.NET.Vulkan.DescriptorPool GetPool(DescriptorResourceCounts counts)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Allocate(counts))
                    {
                        return poolInfo.Pool;
                    }
                }

                PoolInfo newPool = CreateNewPool();
                _pools.Add(newPool);
                bool result = newPool.Allocate(counts);
                Debug.Assert(result);
                return newPool.Pool;
            }
        }

        private unsafe PoolInfo CreateNewPool()
        {
            uint totalSets = 1000;
            uint descriptorCount = 100;
            uint poolSizeCount = 7;
            Silk.NET.Vulkan.DescriptorPoolSize* sizes = stackalloc Silk.NET.Vulkan.DescriptorPoolSize[(int)poolSizeCount];
            sizes[0].Type = Silk.NET.Vulkan.DescriptorType.UniformBuffer;
            sizes[0].DescriptorCount = descriptorCount;
            sizes[1].Type = Silk.NET.Vulkan.DescriptorType.SampledImage;
            sizes[1].DescriptorCount = descriptorCount;
            sizes[2].Type = Silk.NET.Vulkan.DescriptorType.Sampler;
            sizes[2].DescriptorCount = descriptorCount;
            sizes[3].Type = Silk.NET.Vulkan.DescriptorType.StorageBuffer;
            sizes[3].DescriptorCount = descriptorCount;
            sizes[4].Type = Silk.NET.Vulkan.DescriptorType.StorageImage;
            sizes[4].DescriptorCount = descriptorCount;
            sizes[5].Type = Silk.NET.Vulkan.DescriptorType.UniformBufferDynamic;
            sizes[5].DescriptorCount = descriptorCount;
            sizes[6].Type = Silk.NET.Vulkan.DescriptorType.StorageBufferDynamic;
            sizes[6].DescriptorCount = descriptorCount;

            Silk.NET.Vulkan.DescriptorPoolCreateInfo poolCI = new Silk.NET.Vulkan.DescriptorPoolCreateInfo();
            poolCI.SType = Silk.NET.Vulkan.StructureType.DescriptorPoolCreateInfo;

            poolCI.Flags = Silk.NET.Vulkan.DescriptorPoolCreateFlags.DescriptorPoolCreateFreeDescriptorSetBit;
            poolCI.MaxSets = totalSets;
            poolCI.PPoolSizes = sizes;
            poolCI.PoolSizeCount = poolSizeCount;

            var result = _gd.vk.CreateDescriptorPool(_gd.Device, &poolCI, null, out Silk.NET.Vulkan.DescriptorPool descriptorPool);
            VulkanUtil.CheckResult(result);

            return new PoolInfo(descriptorPool, totalSets, descriptorCount);
        }

        internal unsafe void DestroyAll()
        {
            foreach (PoolInfo poolInfo in _pools)
            {
                _gd.vk.DestroyDescriptorPool(_gd.Device, poolInfo.Pool, null);
            }
        }

        private class PoolInfo
        {
            public readonly Silk.NET.Vulkan.DescriptorPool Pool;

            public uint RemainingSets;

            public uint UniformBufferCount;
            public uint SampledImageCount;
            public uint SamplerCount;
            public uint StorageBufferCount;
            public uint StorageImageCount;

            public PoolInfo(Silk.NET.Vulkan.DescriptorPool pool, uint totalSets, uint descriptorCount)
            {
                Pool = pool;
                RemainingSets = totalSets;
                UniformBufferCount = descriptorCount;
                SampledImageCount = descriptorCount;
                SamplerCount = descriptorCount;
                StorageBufferCount = descriptorCount;
                StorageImageCount = descriptorCount;
            }

            internal bool Allocate(DescriptorResourceCounts counts)
            {
                if (RemainingSets > 0
                    && UniformBufferCount >= counts.UniformBufferCount
                    && SampledImageCount >= counts.SampledImageCount
                    && SamplerCount >= counts.SamplerCount
                    && StorageBufferCount >= counts.SamplerCount
                    && StorageImageCount >= counts.StorageImageCount)
                {
                    RemainingSets -= 1;
                    UniformBufferCount -= counts.UniformBufferCount;
                    SampledImageCount -= counts.SampledImageCount;
                    SamplerCount -= counts.SamplerCount;
                    StorageBufferCount -= counts.StorageBufferCount;
                    StorageImageCount -= counts.StorageImageCount;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            internal void Free(Silk.NET.Vulkan.Vk vk, Silk.NET.Vulkan.Device device, DescriptorAllocationToken token, DescriptorResourceCounts counts)
            {
                Silk.NET.Vulkan.DescriptorSet set = token.Set;
                vk.FreeDescriptorSets(device, Pool, 1, set);

                RemainingSets += 1;

                UniformBufferCount += counts.UniformBufferCount;
                SampledImageCount += counts.SampledImageCount;
                SamplerCount += counts.SamplerCount;
                StorageBufferCount += counts.StorageBufferCount;
                StorageImageCount += counts.StorageImageCount;
            }
        }
    }

    internal struct DescriptorAllocationToken
    {
        public readonly Silk.NET.Vulkan.DescriptorSet Set;
        public readonly Silk.NET.Vulkan.DescriptorPool Pool;

        public DescriptorAllocationToken(Silk.NET.Vulkan.DescriptorSet set, Silk.NET.Vulkan.DescriptorPool pool)
        {
            Set = set;
            Pool = pool;
        }
    }
}
