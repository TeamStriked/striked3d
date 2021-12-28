using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class TextureBuffer
    {
        private Image image = new Image();
        private Sampler sampler = new Sampler();
        private ImageView imageView = new ImageView();

        private DeviceMemory imageMemory = new DeviceMemory();
        private ImageLayout imageLayout = ImageLayout.Preinitialized;
        private ImageViewType typeFormat = ImageViewType.ImageViewType2D;
        private Format format = Format.R8G8B8A8Unorm;
        private bool antisoptryEnabled = false;

        RenderingInstance instance;
        LogicalDevice logical;
        CommandPool commandPool;

        public ImageView NativeViewHandle
        {
            get { return imageView;}
        }
        public TextureBuffer(RenderingInstance _instance, LogicalDevice _logical, CommandPool _commandPool)
        {
            this.instance = _instance;
            this.logical = _logical;
            this.commandPool = _commandPool;
        }
        public unsafe void Create( Format format, ImageTiling tiling, ImageUsageFlags  usage, MemoryPropertyFlags properties)
        {
            var imageInfo = new ImageCreateInfo();
            imageInfo.SType = StructureType.ImageCreateInfo;
            imageInfo.ImageType = ImageType.ImageType2D;
            imageInfo.Extent = new Extent3D();
            imageInfo.Extent.Width = (uint) instance.WindowHandle.FramebufferSize.X;
            imageInfo.Extent.Height = (uint) instance.WindowHandle.FramebufferSize.Y;
            imageInfo.Extent.Depth = 1;

            imageInfo.MipLevels = 1;
            imageInfo.ArrayLayers = 1;
            imageInfo.Format = format;
            imageInfo.Tiling = tiling;
            imageInfo.InitialLayout = ImageLayout.Undefined;
            imageInfo.Usage = usage;
            //imageInfo.samples = this.renderInstance.mssaLevel;
            imageInfo.Samples = this.logical.MsaaLevel;
            imageInfo.SharingMode = SharingMode.Exclusive;

            if (this.imageMemory.Handle != default || image.Handle != default)
            {
                this.FreeImage();
            }

            var newImage = new Image();
            var imageMemory = new DeviceMemory();

            var result = instance.Api.CreateImage(logical.NativeHandle, &imageInfo, null, &newImage);
            if(result != Result.Success)
            {
                throw new Exception("Cant create image.");
            }

            this.image = newImage;

            var memRequirements = new MemoryRequirements();
            instance.Api.GetImageMemoryRequirements(logical.NativeHandle, this.image, &memRequirements);

            var allocInfo = new MemoryAllocateInfo();
            allocInfo.SType = StructureType.MemoryAllocateInfo;
            allocInfo.AllocationSize = memRequirements.Size;
            allocInfo.MemoryTypeIndex = logical.GetMemoryTypeIndex(properties, memRequirements.MemoryTypeBits);

            result = instance.Api.AllocateMemory(logical.NativeHandle, &allocInfo, null, &imageMemory);
            if (result != Result.Success)
            {
                throw new Exception("Cant create image memory.");
            }

            this.imageMemory = imageMemory;
            instance.Api.BindImageMemory(logical.NativeHandle, this.image, this.imageMemory, 0);
            this.imageLayout = ImageLayout.Undefined;
        }

        public unsafe void createImageView(Format format, ImageAspectFlags aspectFlags)
        {
            var viewInfo = new ImageViewCreateInfo();
            viewInfo.SType = StructureType.ImageViewCreateInfo;
            viewInfo.Image = this.image;
            viewInfo.ViewType = ImageViewType.ImageViewType2D;
            viewInfo.Format = format;
            viewInfo.SubresourceRange = new ImageSubresourceRange();
            viewInfo.SubresourceRange.AspectMask = aspectFlags;
            viewInfo.SubresourceRange.BaseMipLevel = 0;
            viewInfo.SubresourceRange.LevelCount = 1;
            viewInfo.SubresourceRange.BaseArrayLayer = 0;
            viewInfo.SubresourceRange.LayerCount = 1;

            this.format = format;

            var imageView = new ImageView();
            var result = instance.Api.CreateImageView(logical.NativeHandle, &viewInfo, null, &imageView);
            if (result != Result.Success)
            {
                throw new Exception("Cant create image view.");
            }

            this.imageView = imageView;
        }
        public unsafe void setNewLayout(ImageLayout imageLayout)
        {
            var subresourceRange = new ImageSubresourceRange();
            subresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
            subresourceRange.BaseMipLevel = 0;
            subresourceRange.LevelCount = 1;
            subresourceRange.BaseArrayLayer = 0;
            subresourceRange.LayerCount = 1;

            setNewLayout(imageLayout, subresourceRange);
        }
        public unsafe void setNewLayout(ImageLayout imageLayout, ImageSubresourceRange subresourceRange)
        {
            CommandBuffer _cmdBuffer = new CommandBuffer();

            var cmdBufferAllocInfo = new CommandBufferAllocateInfo();
            cmdBufferAllocInfo.SType = StructureType.CommandBufferAllocateInfo;
            cmdBufferAllocInfo.CommandPool = commandPool.NativeHandle;
            cmdBufferAllocInfo.Level = CommandBufferLevel.Primary;
            cmdBufferAllocInfo.CommandBufferCount = 1;

    
            var result = instance.Api.AllocateCommandBuffers(logical.NativeHandle, &cmdBufferAllocInfo, &_cmdBuffer);
            if (result != Result.Success)
            {
                throw new Exception("Cant allocate image command buffer");
            }

            var cmdBufferBeginInfo = new CommandBufferBeginInfo();
            cmdBufferBeginInfo.SType = StructureType.CommandBufferBeginInfo;
            cmdBufferBeginInfo.Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit;
            cmdBufferBeginInfo.PInheritanceInfo = null;

            result = instance.Api.BeginCommandBuffer(_cmdBuffer, cmdBufferBeginInfo);
            if (result != Result.Success)
            {
                throw new Exception("Cant begin command buffer");
            }

            if (imageLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                subresourceRange.AspectMask = ImageAspectFlags.ImageAspectDepthBit;

                if (this.format == Format.D32SfloatS8Uint  || this.format == Format.D24UnormS8Uint)
                {
                    subresourceRange.AspectMask |= ImageAspectFlags.ImageAspectStencilBit;
                }
            }

            if (imageLayout == ImageLayout.ColorAttachmentOptimal)
            {
                subresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
            }

            AccessFlags srcAccessMask = 0;
            AccessFlags dstAccessMask = 0;
            PipelineStageFlags srcStage = 0;
            PipelineStageFlags dstStage = 0;
            if (imageLayout == ImageLayout.TransferDstOptimal && this.imageLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                dstAccessMask = AccessFlags.AccessTransferWriteBit;
                srcAccessMask = AccessFlags.AccessShaderReadBit;
                dstStage =  PipelineStageFlags.PipelineStageTransferBit;
                srcStage = PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            if (imageLayout == ImageLayout.TransferDstOptimal && this.imageLayout == ImageLayout.Preinitialized)
            {
                srcAccessMask = 0;
                dstAccessMask = AccessFlags.AccessTransferWriteBit;
                srcStage =  PipelineStageFlags.PipelineStageTopOfPipeBit;
                dstStage = PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (imageLayout == ImageLayout.ShaderReadOnlyOptimal && this.imageLayout == ImageLayout.TransferDstOptimal)
            {
                srcAccessMask = AccessFlags.AccessTransferWriteBit;
                dstAccessMask = AccessFlags.AccessShaderReadBit;
                srcStage = PipelineStageFlags.PipelineStageTransferBit;
                dstStage = PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (imageLayout == ImageLayout.DepthStencilAttachmentOptimal && this.imageLayout == ImageLayout.Undefined)
            {
                srcAccessMask = 0;
                dstAccessMask = AccessFlags.AccessDepthStencilAttachmentReadBit | AccessFlags.AccessDepthStencilAttachmentWriteBit;
                srcStage = PipelineStageFlags.PipelineStageTopOfPipeBit;
                dstStage =  PipelineStageFlags.PipelineStageEarlyFragmentTestsBit;
            }
            else if (imageLayout == ImageLayout.ColorAttachmentOptimal && this.imageLayout == ImageLayout.Undefined)
            {
                srcAccessMask = 0;
                dstAccessMask = AccessFlags.AccessColorAttachmentReadBit | AccessFlags.AccessColorAttachmentWriteBit;
                srcStage =  PipelineStageFlags.PipelineStageTopOfPipeBit;
                dstStage =  PipelineStageFlags.PipelineStageColorAttachmentOutputBit; //100
            }

            var imageMemoryBarrier = new ImageMemoryBarrier();
            imageMemoryBarrier.SType = StructureType.ImageMemoryBarrier;
            imageMemoryBarrier.SrcAccessMask = srcAccessMask;
            imageMemoryBarrier.DstAccessMask = dstAccessMask;
            imageMemoryBarrier.OldLayout = this.imageLayout;
            imageMemoryBarrier.NewLayout = imageLayout;
            imageMemoryBarrier.SrcQueueFamilyIndex = 0;
            imageMemoryBarrier.DstQueueFamilyIndex = 0;
            imageMemoryBarrier.Image = this.image;
            imageMemoryBarrier.SubresourceRange = subresourceRange;

            instance.Api.CmdPipelineBarrier(_cmdBuffer, srcStage, dstStage, 0, 0, null, 0, null, 1, &imageMemoryBarrier);
            result = instance.Api.EndCommandBuffer(_cmdBuffer);
            if (result != Result.Success)
            {
                throw new Exception("Cant end command buffer");
            }

            var submitInfo = new SubmitInfo();
            submitInfo.SType = StructureType.SubmitInfo;
            submitInfo.WaitSemaphoreCount = 0;
            submitInfo.PWaitSemaphores = null;
            submitInfo.PWaitDstStageMask = null;
            submitInfo.CommandBufferCount = 1;
            submitInfo.PCommandBuffers = &_cmdBuffer;
            submitInfo.SignalSemaphoreCount = 0;
            submitInfo.PSignalSemaphores = null;

            instance.Api.QueueSubmit(logical.NativeGraphicsQueue, 1, &submitInfo, default);
            instance.Api.QueueWaitIdle(logical.NativeGraphicsQueue);

            instance.Api.ResetCommandBuffer(_cmdBuffer, 0);


            this.imageLayout = imageLayout;
        }

        public unsafe void FreeImage()
        {
            if (this.image.Handle != default)
            {
                instance.Api.DestroyImage(this.logical.NativeHandle, this.image, default);
            }
            if (this.imageMemory.Handle != default)
            {
                instance.Api.FreeMemory(this.logical.NativeHandle, this.imageMemory, default);
            }

            this.image = new Image();
            this.imageMemory = new DeviceMemory();
        }

        public unsafe void Destroy()
        {
            this.FreeImage();
            if (this.imageView.Handle != default)
            {
                instance.Api.DestroyImageView(this.logical.NativeHandle, this.imageView, null);
            }
            if (this.sampler.Handle != default)
            {
                instance.Api.DestroySampler(this.logical.NativeHandle, this.sampler, null);
            }

            this.imageView = new ImageView();
            this.sampler = new Sampler();
        }

    }
}
