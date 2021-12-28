using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Text;
using VKCommandPool = Silk.NET.Vulkan.CommandPool;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class CommandPool
    {
        private VKCommandPool _commandPool;

        private RenderingInstance _instance;
        private LogicalDevice _device;

        public VKCommandPool NativeHandle
        {
            get { return _commandPool; }    
        }
        public CommandPool(RenderingInstance _instance, LogicalDevice _device)
        {
            this._instance = _instance;
            this._device = _device;
        }

        public unsafe void Destroy()
        {
            _instance.Api.DestroyCommandPool(_device.NativeHandle, _commandPool, null);
        }

        public unsafe void Instanciate()
        {
            var queueFamilyIndices = _device.PhysicalRenderingDevice.FindQueueFamilies();

            var poolInfo = new CommandPoolCreateInfo
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamilyIndices.GraphicsFamily.Value,
                Flags = CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit
            };

            fixed (VKCommandPool* commandPool = &_commandPool)
            {
                if (_instance.Api.CreateCommandPool(_device.NativeHandle, &poolInfo, null, commandPool) != Result.Success)
                {
                    throw new Exception("failed to create command pool!");
                }
            }
        }

    }
}
