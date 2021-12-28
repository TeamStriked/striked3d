using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class RenderQueue
    {
        protected Swapchain _swapchain;
        protected RenderingInstance _instance;

        public RenderQueue(RenderingInstance _instance, Swapchain _swapchain)
        {
            this._swapchain = _swapchain;
            this._instance = _instance;
        }
        private Semaphore _presentSemaphore;
        private Semaphore _renderSemaphore;
        private Fence _renderFence;

        public unsafe void Instanciate()
        {
            _presentSemaphore = new Semaphore();
            _renderSemaphore = new Semaphore();
            _renderFence = new Fence();

            SemaphoreCreateInfo semaphoreInfo = new SemaphoreCreateInfo();
            semaphoreInfo.SType = StructureType.SemaphoreCreateInfo;

            FenceCreateInfo fenceInfo = new FenceCreateInfo();
            fenceInfo.SType = StructureType.FenceCreateInfo;
            fenceInfo.Flags = FenceCreateFlags.FenceCreateSignaledBit;


            Semaphore imgAvSema, renderFinSema;
            Fence renderFence;
            if (this._instance.Api.CreateSemaphore(_swapchain.Device.NativeHandle, &semaphoreInfo, null, &imgAvSema) != Result.Success ||
                this._instance.Api.CreateSemaphore(_swapchain.Device.NativeHandle, &semaphoreInfo, null, &renderFinSema) != Result.Success ||
                this._instance.Api.CreateFence(_swapchain.Device.NativeHandle, &fenceInfo, null, &renderFence) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }

            _presentSemaphore = imgAvSema;
            _renderSemaphore = renderFinSema;
            _renderFence = renderFence;
        }


        public unsafe void Destroy()
        {
            this._instance.Api.DestroySemaphore(_swapchain.Device.NativeHandle, _renderSemaphore, null);
            this._instance.Api.DestroySemaphore(_swapchain.Device.NativeHandle, _presentSemaphore, null);
            this._instance.Api.DestroyFence(_swapchain.Device.NativeHandle, _renderFence, null);
        }


        public unsafe void BeginRenderPass(CommandBuffers buffers, FrameBuffers frameBuffer, RenderPass renderPass, uint imageIndex)
        {
            var renderPassInfo = new RenderPassBeginInfo
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = renderPass.NativeHandle,
                Framebuffer = frameBuffer.SwapchainFrameBuffers[imageIndex],
                RenderArea = {
                      Offset = new Offset2D { X = 0, Y = 0 },
                      Extent = renderPass.Swapchain.SwapchainExtent
                  }
            };


            Span<ClearValue> clearvalues = stackalloc ClearValue[2];
            clearvalues[0] = new ClearValue { Color = new ClearColorValue { Float32_0 = 0.0f, Float32_1 = 0.0f, Float32_2 = 0.1f, Float32_3 = 0.2f} };

            var depthStencil = new ClearDepthStencilValue();
            depthStencil.Depth = 1;
            depthStencil.Stencil = 0;

            clearvalues[1] = new ClearValue  { DepthStencil = depthStencil };

            renderPassInfo.ClearValueCount = (uint) clearvalues.Length;
            renderPassInfo.PClearValues = (ClearValue*)Unsafe.AsPointer(ref clearvalues[0]);

            _instance.Api.CmdBeginRenderPass(buffers.Buffer, &renderPassInfo, SubpassContents.Inline);
        }

        public unsafe Result AquireNextImages(out uint newIndex)
        {
            uint imageIndex;
            Result result = _swapchain.NativeKhrSwapChain.AcquireNextImage
          (_swapchain.Device.NativeHandle, _swapchain.NativeHandleSwapChain, ulong.MaxValue, _presentSemaphore, default, &imageIndex);

            newIndex = imageIndex;

            return result;
        }


        public unsafe void BeginCommandBuffer(CommandBuffers buffers)
        {
            var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo, Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit };

            if (_instance.Api.BeginCommandBuffer(buffers.Buffer, &beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }
        }

        public unsafe void EndCommandBuffer(CommandBuffers buffers)
        {
            if (_instance.Api.EndCommandBuffer(buffers.Buffer) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
        }

        public unsafe void EndRenderPass(CommandBuffers buffers)
        {
            _instance.Api.CmdEndRenderPass(buffers.Buffer);

        }

        public unsafe void WaitFor()
        {
            this._instance.Api.WaitForFences(_swapchain.Device.NativeHandle, 1, in _renderFence, Vk.True, ulong.MaxValue);
            this._instance.Api.ResetFences(_swapchain.Device.NativeHandle, 1, in _renderFence);
        }

   

        public unsafe void QueueSubmit(CommandBuffers buffers)
        {
            SubmitInfo submitInfo = new SubmitInfo { SType = StructureType.SubmitInfo };

            Semaphore[] waitSemaphores = { _presentSemaphore };
            PipelineStageFlags[] waitStages = { PipelineStageFlags.PipelineStageColorAttachmentOutputBit };
            submitInfo.WaitSemaphoreCount = 1;
            var signalSemaphore = _renderSemaphore;
            var renderFence = _renderFence;
            fixed (Semaphore* waitSemaphoresPtr = waitSemaphores)
            {
                fixed (PipelineStageFlags* waitStagesPtr = waitStages)
                {
                    submitInfo.PWaitSemaphores = waitSemaphoresPtr;
                    submitInfo.PWaitDstStageMask = waitStagesPtr;

                   submitInfo.CommandBufferCount = 1;
                   var buffer = buffers.Buffer;
                    submitInfo.PCommandBuffers = &buffer;

                    submitInfo.SignalSemaphoreCount = 1;
                    submitInfo.PSignalSemaphores = &signalSemaphore;

                    this._instance.Api.ResetFences(_swapchain.Device.NativeHandle, 1, &renderFence);

                    if (this._instance.Api.QueueSubmit
                            (_swapchain.Device.NativeGraphicsQueue, 1, &submitInfo, _renderFence) != Result.Success)
                    {
                        throw new Exception("failed to submit draw command buffer!");
                    }
                }
            }

        }

        public unsafe Result QueuePresent(uint imageIndex)
        {
            var signalSemaphore = _renderSemaphore;
            var swapchain = _swapchain.NativeHandleSwapChain;
          
            PresentInfoKHR presentInfo = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &signalSemaphore,
                SwapchainCount = 1,
                PSwapchains = &swapchain,
                PImageIndices = &imageIndex
            };

            return _swapchain.NativeKhrSwapChain.QueuePresent(_swapchain.Device.NativePresentQueue, &presentInfo);
        }

    }
}
