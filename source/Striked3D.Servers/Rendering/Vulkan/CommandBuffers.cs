using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Text;
using VKCommandPool = Silk.NET.Vulkan.CommandPool;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class CommandBuffers
    {

        private RenderingInstance _instance;
        private LogicalDevice _device;
        private CommandPool _commandPool;
        private CommandBuffer _commandBuffer;

        public CommandBuffer Buffer { get { return _commandBuffer; } set { _commandBuffer = value; } }

        public CommandBuffers(RenderingInstance _instance, LogicalDevice _device, CommandPool _commandPool)
        {
            this._instance = _instance;
            this._device = _device;
            this._commandPool = _commandPool;

        }
        public unsafe void Destroy()
        {
            fixed (CommandBuffer* buffers = &_commandBuffer)
            {
                _instance.Api.FreeCommandBuffers(
                    _device.NativeHandle, 
                    _commandPool.NativeHandle, 1, buffers);
            }
        }
        public unsafe void Instanciate()
        {
            _commandBuffer = new CommandBuffer();

            var allocInfo = new CommandBufferAllocateInfo
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = _commandPool.NativeHandle,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = 1
            };

            fixed (CommandBuffer* commandBuffers = &_commandBuffer)
            {
                if (_instance.Api.AllocateCommandBuffers(
                    _device.NativeHandle, 
                    &allocInfo, commandBuffers) != Result.Success)
                {
                    throw new Exception("failed to allocate command buffers!");
                }
            }

         
        }
    }
}
