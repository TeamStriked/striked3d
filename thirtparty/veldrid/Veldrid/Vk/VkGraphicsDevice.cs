using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Veldrid.Vk.VulkanUtil;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Veldrid.Vk
{
    internal unsafe class VkGraphicsDevice : GraphicsDevice
    {
        public const uint MaxPhysicalDeviceNameSize = 256;

        private static readonly FixedUtf8String s_name = "Veldrid-VkGraphicsDevice";
        private static readonly Lazy<bool> s_isSupported = new Lazy<bool>(CheckIsSupported, isThreadSafe: true);

        private Silk.NET.Vulkan.Instance _instance;
        private Silk.NET.Vulkan.PhysicalDevice _physicalDevice;
        private string _deviceName;
        private string _vendorName;
        private GraphicsApiVersion _apiVersion;
        private string _driverName;
        private string _driverInfo;
        private VkDeviceMemoryManager _memoryManager;
        private Silk.NET.Vulkan.PhysicalDeviceProperties _physicalDeviceProperties;
        private Silk.NET.Vulkan.PhysicalDeviceFeatures _physicalDeviceFeatures;
        private Silk.NET.Vulkan.PhysicalDeviceMemoryProperties _physicalDeviceMemProperties;
        private Silk.NET.Vulkan.Device _device;
        private uint _graphicsQueueIndex;
        private uint _presentQueueIndex;
        private Silk.NET.Vulkan.CommandPool _graphicsCommandPool;
        private readonly object _graphicsCommandPoolLock = new object();
        private Silk.NET.Vulkan.Queue _graphicsQueue;
        private readonly object _graphicsQueueLock = new object();
        private Silk.NET.Vulkan.DebugReportCallbackEXT _debugCallbackHandle;
        private Silk.NET.Vulkan.PfnDebugReportCallbackEXT _debugCallbackFunc;
        private bool _debugMarkerEnabled;
        private vkDebugMarkerSetObjectNameEXT_t _setObjectNameDelegate;
        private vkCmdDebugMarkerBeginEXT_t _markerBegin;
        private vkCmdDebugMarkerEndEXT_t _markerEnd;
        private vkCmdDebugMarkerInsertEXT_t _markerInsert;
        private readonly ConcurrentDictionary<Silk.NET.Vulkan.Format, Silk.NET.Vulkan.Filter> _filters = new ConcurrentDictionary<Silk.NET.Vulkan.Format, Silk.NET.Vulkan.Filter>();
        private readonly BackendInfoVulkan _vulkanInfo;

        private const int SharedCommandPoolCount = 4;
        private Stack<SharedCommandPool> _sharedGraphicsCommandPools = new Stack<SharedCommandPool>();
        private VkDescriptorPoolManager _descriptorPoolManager;
        private bool _standardValidationSupported;
        private bool _khronosValidationSupported;
        private bool _standardClipYDirection;
        private vkGetBufferMemoryRequirements2_t _getBufferMemoryRequirements2;
        private vkGetImageMemoryRequirements2_t _getImageMemoryRequirements2;
        private vkGetPhysicalDeviceProperties2_t _getPhysicalDeviceProperties2;
        private vkCreateMetalSurfaceEXT_t _createMetalSurfaceEXT;

        // Staging Resources
        private const uint MinStagingBufferSize = 64;
        private const uint MaxStagingBufferSize = 512;

        private readonly object _stagingResourcesLock = new object();
        private readonly List<VkTexture> _availableStagingTextures = new List<VkTexture>();
        private readonly List<VkBuffer> _availableStagingBuffers = new List<VkBuffer>();

        private readonly Dictionary<Silk.NET.Vulkan.CommandBuffer, VkTexture> _submittedStagingTextures
            = new Dictionary<Silk.NET.Vulkan.CommandBuffer, VkTexture>();
        private readonly Dictionary<Silk.NET.Vulkan.CommandBuffer, VkBuffer> _submittedStagingBuffers
            = new Dictionary<Silk.NET.Vulkan.CommandBuffer, VkBuffer>();
        private readonly Dictionary<Silk.NET.Vulkan.CommandBuffer, SharedCommandPool> _submittedSharedCommandPools
            = new Dictionary<Silk.NET.Vulkan.CommandBuffer, SharedCommandPool>();

        public override string DeviceName => _deviceName;

        public override string VendorName => _vendorName;

        public override GraphicsApiVersion ApiVersion => _apiVersion;

        public override GraphicsBackend BackendType => GraphicsBackend.Vulkan;

        public override bool IsUvOriginTopLeft => true;

        public override bool IsDepthRangeZeroToOne => true;

        public override bool IsClipSpaceYInverted => !_standardClipYDirection;

        public override Swapchain MainSwapchain => _mainSwapchain;

        public override GraphicsDeviceFeatures Features { get; }

        public override bool GetVulkanInfo(out BackendInfoVulkan info)
        {
            info = _vulkanInfo;
            return true;
        }

        private readonly Silk.NET.Vulkan.Vk _vk;
        public Silk.NET.Vulkan.Vk vk => _vk;
        public Silk.NET.Vulkan.Instance Instance => _instance;
        public Silk.NET.Vulkan.Device Device => _device;
        public Silk.NET.Vulkan.PhysicalDevice PhysicalDevice => _physicalDevice;
        public Silk.NET.Vulkan.PhysicalDeviceMemoryProperties PhysicalDeviceMemProperties => _physicalDeviceMemProperties;
        public Silk.NET.Vulkan.Queue GraphicsQueue => _graphicsQueue;
        public uint GraphicsQueueIndex => _graphicsQueueIndex;
        public uint PresentQueueIndex => _presentQueueIndex;
        public string DriverName => _driverName;
        public string DriverInfo => _driverInfo;
        public VkDeviceMemoryManager MemoryManager => _memoryManager;
        public VkDescriptorPoolManager DescriptorPoolManager => _descriptorPoolManager;
        public vkCmdDebugMarkerBeginEXT_t MarkerBegin => _markerBegin;
        public vkCmdDebugMarkerEndEXT_t MarkerEnd => _markerEnd;
        public vkCmdDebugMarkerInsertEXT_t MarkerInsert => _markerInsert;
        public vkGetBufferMemoryRequirements2_t GetBufferMemoryRequirements2 => _getBufferMemoryRequirements2;
        public vkGetImageMemoryRequirements2_t GetImageMemoryRequirements2 => _getImageMemoryRequirements2;
        public vkCreateMetalSurfaceEXT_t CreateMetalSurfaceEXT => _createMetalSurfaceEXT;

        private readonly object _submittedFencesLock = new object();
        private readonly ConcurrentQueue<Silk.NET.Vulkan.Fence> _availableSubmissionFences = new ConcurrentQueue<Silk.NET.Vulkan.Fence>();
        private readonly List<FenceSubmissionInfo> _submittedFences = new List<FenceSubmissionInfo>();
        private readonly VkSwapchain _mainSwapchain;

        private readonly List<FixedUtf8String> _surfaceExtensions = new List<FixedUtf8String>();


        private KhrSwapchain _vkSwapchain;
        public KhrSwapchain vkSwapchain => _vkSwapchain;


        private KhrSurface _vkSurface;
        public KhrSurface vkSurface => _vkSurface;


        public VkGraphicsDevice(Silk.NET.Vulkan.Vk vk, GraphicsDeviceOptions options, SwapchainDescription? scDesc)
            : this(vk, options, scDesc, new VulkanDeviceOptions())
        {
        }

        public VkGraphicsDevice(Silk.NET.Vulkan.Vk vk, GraphicsDeviceOptions options, SwapchainDescription? scDesc, VulkanDeviceOptions vkOptions)
        {
            this._vk = vk;

            CreateInstance(options.Debug, vkOptions);

            Silk.NET.Vulkan.SurfaceKHR surface = new Silk.NET.Vulkan.SurfaceKHR();


            if (!_vk.TryGetInstanceExtension(_instance, out _vkSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

            if (scDesc != null)
            {
                surface = VkSurfaceUtil.CreateSurface(this, _instance, scDesc.Value.Source);
            }

            CreatePhysicalDevice();
            CreateLogicalDevice(surface, options.PreferStandardClipSpaceYDirection, vkOptions);

            _memoryManager = new VkDeviceMemoryManager(
                _vk,
                _device,
                _physicalDevice,
                _physicalDeviceProperties.Limits.BufferImageGranularity,
                _getBufferMemoryRequirements2,
                _getImageMemoryRequirements2);

            Features = new GraphicsDeviceFeatures(
                computeShader: true,
                geometryShader: _physicalDeviceFeatures.GeometryShader,
                tessellationShaders: _physicalDeviceFeatures.TessellationShader,
                multipleViewports: _physicalDeviceFeatures.MultiViewport,
                samplerLodBias: true,
                drawBaseVertex: true,
                drawBaseInstance: true,
                drawIndirect: true,
                drawIndirectBaseInstance: _physicalDeviceFeatures.DrawIndirectFirstInstance,
                fillModeWireframe: _physicalDeviceFeatures.FillModeNonSolid,
                samplerAnisotropy: _physicalDeviceFeatures.SamplerAnisotropy,
                depthClipDisable: _physicalDeviceFeatures.DepthClamp,
                texture1D: true,
                independentBlend: _physicalDeviceFeatures.IndependentBlend,
                structuredBuffer: true,
                subsetTextureView: true,
                commandListDebugMarkers: _debugMarkerEnabled,
                bufferRangeBinding: true,
                shaderFloat64: _physicalDeviceFeatures.ShaderFloat64);

            ResourceFactory = new VkResourceFactory(this);

            if (!_vk.TryGetDeviceExtension(_instance, _vk.CurrentDevice.Value, out _vkSwapchain))
            {
                throw new NotSupportedException("KHR_swapchain extension not found.");
            }

            if (scDesc != null)
            {
                SwapchainDescription desc = scDesc.Value;
                _mainSwapchain = new VkSwapchain(this, ref desc, surface);
            }

            CreateDescriptorPool();
            CreateGraphicsCommandPool();
            for (int i = 0; i < SharedCommandPoolCount; i++)
            {
                _sharedGraphicsCommandPools.Push(new SharedCommandPool(this, true));
            }

            _vulkanInfo = new BackendInfoVulkan(this);

            PostDeviceCreated();
        }

        public override ResourceFactory ResourceFactory { get; }

        private protected override void SubmitCommandsCore(CommandList cl, Fence fence)
        {
            SubmitCommandList(cl, 0, null, 0, null, fence);
        }

        private void SubmitCommandList(
            CommandList cl,
            uint waitSemaphoreCount,
            Silk.NET.Vulkan.Semaphore* waitSemaphoresPtr,
            uint signalSemaphoreCount,
            Silk.NET.Vulkan.Semaphore* signalSemaphoresPtr,
            Fence fence)
        {
            VkCommandList vkCL = Util.AssertSubtype<CommandList, VkCommandList>(cl);
            Silk.NET.Vulkan.CommandBuffer vkCB = vkCL.CommandBuffer;

            vkCL.CommandBufferSubmitted(vkCB);
            SubmitCommandBuffer(vkCL, vkCB, waitSemaphoreCount, waitSemaphoresPtr, signalSemaphoreCount, signalSemaphoresPtr, fence);
        }

        private void SubmitCommandBuffer(
            VkCommandList vkCL,
            Silk.NET.Vulkan.CommandBuffer vkCB,
            uint waitSemaphoreCount,
            Silk.NET.Vulkan.Semaphore* waitSemaphoresPtr,
            uint signalSemaphoreCount,
            Silk.NET.Vulkan.Semaphore* signalSemaphoresPtr,
            Fence fence)
        {
            CheckSubmittedFences();

            bool useExtraFence = fence != null;
            Silk.NET.Vulkan.SubmitInfo si = new Silk.NET.Vulkan.SubmitInfo();
            si.SType = Silk.NET.Vulkan.StructureType.SubmitInfo;

            si.CommandBufferCount = 1;
            si.PCommandBuffers = &vkCB;
            Silk.NET.Vulkan.PipelineStageFlags waitDstStageMask = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            si.PWaitDstStageMask = &waitDstStageMask;

            si.PWaitSemaphores = waitSemaphoresPtr;
            si.WaitSemaphoreCount = waitSemaphoreCount;
            si.PSignalSemaphores = signalSemaphoresPtr;
            si.SignalSemaphoreCount = signalSemaphoreCount;

            Silk.NET.Vulkan.Fence vkFence = new Silk.NET.Vulkan.Fence();
            Silk.NET.Vulkan.Fence submissionFence = new Silk.NET.Vulkan.Fence();
            if (useExtraFence)
            {
                vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
                submissionFence = GetFreeSubmissionFence();
            }
            else
            {
                vkFence = GetFreeSubmissionFence();
                submissionFence = vkFence;
            }

            lock (_graphicsQueueLock)
            {
                var result = _vk.QueueSubmit(_graphicsQueue, 1, &si, vkFence);
                CheckResult(result);
                if (useExtraFence)
                {
                    result =  _vk.QueueSubmit(_graphicsQueue, 0, null, submissionFence);
                    CheckResult(result);
                }
            }

            lock (_submittedFencesLock)
            {
                _submittedFences.Add(new FenceSubmissionInfo(submissionFence, vkCL, vkCB));
            }
        }

        private void CheckSubmittedFences()
        {
            lock (_submittedFencesLock)
            {
                for (int i = 0; i < _submittedFences.Count; i++)
                {
                    FenceSubmissionInfo fsi = _submittedFences[i];
                    if (_vk.GetFenceStatus(_device, fsi.Fence) == Silk.NET.Vulkan.Result.Success)
                    {
                        CompleteFenceSubmission(fsi);
                        _submittedFences.RemoveAt(i);
                        i -= 1;
                    }
                    else
                    {
                        break; // Submissions are in order; later submissions cannot complete if this one hasn't.
                    }
                }
            }
        }

        private void CompleteFenceSubmission(FenceSubmissionInfo fsi)
        {
            Silk.NET.Vulkan.Fence fence = fsi.Fence;
            Silk.NET.Vulkan.CommandBuffer completedCB = fsi.CommandBuffer;
            fsi.CommandList?.CommandBufferCompleted(completedCB);
            var resetResult = _vk.ResetFences(_device, 1, &fence);
            CheckResult(resetResult);
            ReturnSubmissionFence(fence);
            lock (_stagingResourcesLock)
            {
                if (_submittedStagingTextures.TryGetValue(completedCB, out VkTexture stagingTex))
                {
                    _submittedStagingTextures.Remove(completedCB);
                    _availableStagingTextures.Add(stagingTex);
                }
                if (_submittedStagingBuffers.TryGetValue(completedCB, out VkBuffer stagingBuffer))
                {
                    _submittedStagingBuffers.Remove(completedCB);
                    if (stagingBuffer.SizeInBytes <= MaxStagingBufferSize)
                    {
                        _availableStagingBuffers.Add(stagingBuffer);
                    }
                    else
                    {
                        stagingBuffer.Dispose();
                    }
                }
                if (_submittedSharedCommandPools.TryGetValue(completedCB, out SharedCommandPool sharedPool))
                {
                    _submittedSharedCommandPools.Remove(completedCB);
                    lock (_graphicsCommandPoolLock)
                    {
                        if (sharedPool.IsCached)
                        {
                            _sharedGraphicsCommandPools.Push(sharedPool);
                        }
                        else
                        {
                            sharedPool.Destroy();
                        }
                    }
                }
            }
        }

        private void ReturnSubmissionFence(Silk.NET.Vulkan.Fence fence)
        {
            _availableSubmissionFences.Enqueue(fence);
        }

        private Silk.NET.Vulkan.Fence GetFreeSubmissionFence()
        {
            if (_availableSubmissionFences.TryDequeue(out Silk.NET.Vulkan.Fence availableFence))
            {
                return availableFence;
            }
            else
            {
                Silk.NET.Vulkan.FenceCreateInfo fenceCI = new Silk.NET.Vulkan.FenceCreateInfo();
                fenceCI.SType = Silk.NET.Vulkan.StructureType.FenceCreateInfo;

                var result = _vk.CreateFence(_device, &fenceCI, null, out Silk.NET.Vulkan.Fence newFence);
                CheckResult(result);
                return newFence;
            }
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
            VkSwapchain vkSC = Util.AssertSubtype<Swapchain, VkSwapchain>(swapchain);
            Silk.NET.Vulkan.SwapchainKHR deviceSwapchain = vkSC.DeviceSwapchain;
            Silk.NET.Vulkan.PresentInfoKHR presentInfo = new Silk.NET.Vulkan.PresentInfoKHR();
            presentInfo.SType = Silk.NET.Vulkan.StructureType.PresentInfoKhr;

            presentInfo.SwapchainCount = 1;
            presentInfo.PSwapchains = &deviceSwapchain;
            uint imageIndex = vkSC.ImageIndex;
            presentInfo.PImageIndices = &imageIndex;

            object presentLock = vkSC.PresentQueueIndex == _graphicsQueueIndex ? _graphicsQueueLock : vkSC;
            lock (presentLock)
            {
                var result = _vkSwapchain.QueuePresent(vkSC.PresentQueue, &presentInfo);
                if (result == Silk.NET.Vulkan.Result.ErrorOutOfDateKhr || result == Silk.NET.Vulkan.Result.SuboptimalKhr)
                {
                    throw new VeldridException("Resize needed");
                }

                if (vkSC.AcquireNextImage(_device, default, vkSC.ImageAvailableFence))
                {
                    Silk.NET.Vulkan.Fence fence = vkSC.ImageAvailableFence;
                    _vk.WaitForFences(_device, 1, &fence, true, ulong.MaxValue);
                    _vk.ResetFences(_device, 1, &fence);
                }
            }
        }

        internal void SetResourceName(DeviceResource resource, string name)
        {
            if (_debugMarkerEnabled)
            {
                switch (resource)
                {
                    case VkBuffer buffer:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeBufferExt, buffer.DeviceBuffer.Handle, name);
                        break;
                    case VkCommandList commandList:
                        SetDebugMarkerName(
                            Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeCommandBufferExt,
                            (ulong)commandList.CommandBuffer.Handle,
                            string.Format("{0}_CommandBuffer", name));
                        SetDebugMarkerName(
                            Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeCommandPoolExt,
                            commandList.CommandPool.Handle,
                            string.Format("{0}_CommandPool", name));
                        break;
                    case VkFramebuffer framebuffer:
                        SetDebugMarkerName(
                            Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeFramebufferExt,
                            framebuffer.CurrentFramebuffer.Handle,
                            name);
                        break;
                    case VkPipeline pipeline:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypePipelineExt, pipeline.DevicePipeline.Handle, name);
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypePipelineLayoutExt, pipeline.PipelineLayout.Handle, name);
                        break;
                    case VkResourceLayout resourceLayout:
                        SetDebugMarkerName(
                            Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeDescriptorSetLayoutExt,
                            resourceLayout.DescriptorSetLayout.Handle,
                            name);
                        break;
                    case VkResourceSet resourceSet:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeDescriptorSetExt, resourceSet.DescriptorSet.Handle, name);
                        break;
                    case VkSampler sampler:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeSamplerExt, sampler.DeviceSampler.Handle, name);
                        break;
                    case VkShader shader:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeShaderModuleExt, shader.ShaderModule.Handle, name);
                        break;
                    case VkTexture tex:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeImageViewExt, tex.OptimalDeviceImage.Handle, name);
                        break;
                    case VkTextureView texView:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeImageViewExt, texView.ImageView.Handle, name);
                        break;
                    case VkFence fence:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeFenceExt, fence.DeviceFence.Handle, name);
                        break;
                    case VkSwapchain sc:
                        SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT.DebugReportObjectTypeSwapchainKhrExt, sc.DeviceSwapchain.Handle, name);
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetDebugMarkerName(Silk.NET.Vulkan.DebugReportObjectTypeEXT type, ulong target, string name)
        {
            Debug.Assert(_setObjectNameDelegate != null);

            Silk.NET.Vulkan.DebugMarkerObjectNameInfoEXT nameInfo = new Silk.NET.Vulkan.DebugMarkerObjectNameInfoEXT();
            nameInfo.ObjectType = type;
            nameInfo.Object = target;
            nameInfo.SType = Silk.NET.Vulkan.StructureType.DebugMarkerObjectNameInfoExt;

            int byteCount = Encoding.UTF8.GetByteCount(name);
            byte* utf8Ptr = stackalloc byte[byteCount + 1];
            fixed (char* namePtr = name)
            {
                Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
            }
            utf8Ptr[byteCount] = 0;

            nameInfo.PObjectName = utf8Ptr;
            var result = _setObjectNameDelegate(_device, &nameInfo);
            CheckResult(result);
        }

        private void CreateInstance(bool debug, VulkanDeviceOptions options)
        {
            HashSet<string> availableInstanceLayers = new HashSet<string>(EnumerateInstanceLayers());
            HashSet<string> availableInstanceExtensions = new HashSet<string>(GetInstanceExtensions());

            Silk.NET.Vulkan.InstanceCreateInfo instanceCI = new Silk.NET.Vulkan.InstanceCreateInfo();
            instanceCI.SType = Silk.NET.Vulkan.StructureType.InstanceCreateInfo;

            Silk.NET.Vulkan.ApplicationInfo applicationInfo = new Silk.NET.Vulkan.ApplicationInfo();
            applicationInfo.ApiVersion = new VkVersion(1, 1, 0);
            applicationInfo.ApplicationVersion = new VkVersion(1, 0, 0);
            applicationInfo.EngineVersion = new VkVersion(1, 0, 0);
            applicationInfo.PApplicationName = s_name;
            applicationInfo.PEngineName = s_name;
            applicationInfo.SType = Silk.NET.Vulkan.StructureType.ApplicationInfo;

            instanceCI.PApplicationInfo = &applicationInfo;

            StackList<IntPtr, Size64Bytes> instanceExtensions = new StackList<IntPtr, Size64Bytes>();
            StackList<IntPtr, Size64Bytes> instanceLayers = new StackList<IntPtr, Size64Bytes>();

            if (availableInstanceExtensions.Contains(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME))
            {
                _surfaceExtensions.Add(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (availableInstanceExtensions.Contains(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME))
                {
                    _surfaceExtensions.Add(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
                }
            }
            else if (
#if NET5_0_OR_GREATER
                OperatingSystem.IsAndroid() ||
#endif
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (availableInstanceExtensions.Contains(CommonStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME))
                {
                    _surfaceExtensions.Add(CommonStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
                }
                if (availableInstanceExtensions.Contains(CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME))
                {
                    _surfaceExtensions.Add(CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
                }
                if (availableInstanceExtensions.Contains(CommonStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME))
                {
                    _surfaceExtensions.Add(CommonStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (availableInstanceExtensions.Contains(CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME))
                {
                    _surfaceExtensions.Add(CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME);
                }
                else // Legacy MoltenVK extensions
                {
                    if (availableInstanceExtensions.Contains(CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME))
                    {
                        _surfaceExtensions.Add(CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME);
                    }
                    if (availableInstanceExtensions.Contains(CommonStrings.VK_MVK_IOS_SURFACE_EXTENSION_NAME))
                    {
                        _surfaceExtensions.Add(CommonStrings.VK_MVK_IOS_SURFACE_EXTENSION_NAME);
                    }
                }
            }

            foreach (var ext in _surfaceExtensions)
            {
                instanceExtensions.Add(ext);
            }

            bool hasDeviceProperties2 = availableInstanceExtensions.Contains(CommonStrings.VK_KHR_get_physical_device_properties2);
            if (hasDeviceProperties2)
            {
                instanceExtensions.Add(CommonStrings.VK_KHR_get_physical_device_properties2);
            }

            string[] requestedInstanceExtensions = options.InstanceExtensions ?? Array.Empty<string>();
            List<FixedUtf8String> tempStrings = new List<FixedUtf8String>();
            foreach (string requiredExt in requestedInstanceExtensions)
            {
                if (!availableInstanceExtensions.Contains(requiredExt))
                {
                    throw new VeldridException($"The required instance extension was not available: {requiredExt}");
                }

                FixedUtf8String utf8Str = new FixedUtf8String(requiredExt);
                instanceExtensions.Add(utf8Str);
                tempStrings.Add(utf8Str);
            }

            bool debugReportExtensionAvailable = false;
            if (debug)
            {
                if (availableInstanceExtensions.Contains(CommonStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME))
                {
                    debugReportExtensionAvailable = true;
                    instanceExtensions.Add(CommonStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
                }
                if (availableInstanceLayers.Contains(CommonStrings.StandardValidationLayerName))
                {
                    _standardValidationSupported = true;
                    instanceLayers.Add(CommonStrings.StandardValidationLayerName);
                }
                if (availableInstanceLayers.Contains(CommonStrings.KhronosValidationLayerName))
                {
                    _khronosValidationSupported = true;
                    instanceLayers.Add(CommonStrings.KhronosValidationLayerName);
                }
            }

            instanceCI.EnabledExtensionCount = instanceExtensions.Count;
            instanceCI.PpEnabledExtensionNames = (byte**)instanceExtensions.Data;

            instanceCI.EnabledLayerCount = instanceLayers.Count;
            if (instanceLayers.Count > 0)
            {
                instanceCI.PpEnabledLayerNames = (byte**)instanceLayers.Data;
            }

            var result = _vk.CreateInstance(&instanceCI, null, out _instance);
            CheckResult(result);

            if (HasSurfaceExtension(CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME))
            {
                _createMetalSurfaceEXT = GetInstanceProcAddr<vkCreateMetalSurfaceEXT_t>("vkCreateMetalSurfaceEXT");
            }

            if (debug && debugReportExtensionAvailable)
            {
                EnableDebugCallback();
            }

            if (hasDeviceProperties2)
            {
                _getPhysicalDeviceProperties2 = GetInstanceProcAddr<vkGetPhysicalDeviceProperties2_t>("vkGetPhysicalDeviceProperties2")
                    ?? GetInstanceProcAddr<vkGetPhysicalDeviceProperties2_t>("vkGetPhysicalDeviceProperties2KHR");

            }

            foreach (FixedUtf8String tempStr in tempStrings)
            {
                tempStr.Dispose();
            }
        }

        public bool HasSurfaceExtension(FixedUtf8String extension)
        {
            return _surfaceExtensions.Contains(extension);
        }

        public void EnableDebugCallback(Silk.NET.Vulkan.DebugReportFlagsEXT flags = Silk.NET.Vulkan.DebugReportFlagsEXT.DebugReportWarningBitExt | Silk.NET.Vulkan.DebugReportFlagsEXT.DebugReportErrorBitExt)
        {
            Debug.WriteLine("Enabling Vulkan Debug callbacks.");
            _debugCallbackFunc = new Silk.NET.Vulkan.PfnDebugReportCallbackEXT(DebugCallback);
        //    IntPtr debugFunctionPtr = Marshal.GetFunctionPointerForDelegate(_debugCallbackFunc);
            Silk.NET.Vulkan.DebugReportCallbackCreateInfoEXT debugCallbackCI = new Silk.NET.Vulkan.DebugReportCallbackCreateInfoEXT();
            debugCallbackCI.Flags = flags;
            debugCallbackCI.PfnCallback = new Silk.NET.Vulkan.PfnDebugReportCallbackEXT(_debugCallbackFunc);
            debugCallbackCI.SType = Silk.NET.Vulkan.StructureType.DebugReportCallbackCreateInfoExt;
            IntPtr createFnPtr;
        
            createFnPtr = _vk.GetInstanceProcAddr(_instance, "vkCreateDebugReportCallbackEXT");
            
            if (createFnPtr == IntPtr.Zero)
            {
                return;
            }

            vkCreateDebugReportCallbackEXT_d createDelegate = Marshal.GetDelegateForFunctionPointer<vkCreateDebugReportCallbackEXT_d>(createFnPtr);
            var result = createDelegate(_instance, &debugCallbackCI, IntPtr.Zero, out _debugCallbackHandle);
            CheckResult(result);
        }

        private uint DebugCallback(
            uint flags,
            Silk.NET.Vulkan.DebugReportObjectTypeEXT objectType,
            ulong @object,
            UIntPtr location,
            int messageCode,
            byte* pLayerPrefix,
            byte* pMessage,
            void* pUserData)
        {
            string message = Util.GetString(pMessage);
            Silk.NET.Vulkan.DebugReportFlagsEXT debugReportFlags = (Silk.NET.Vulkan.DebugReportFlagsEXT)flags;

            string fullMessage = $"[{debugReportFlags}] ({objectType}) {message}";

            Console.WriteLine(fullMessage);


#if DEBUG
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
#endif
            if (debugReportFlags == Silk.NET.Vulkan.DebugReportFlagsEXT.DebugReportErrorBitExt)
            {
             //   throw new VeldridException("A Vulkan validation error was encountered: " + fullMessage);
            }

            return 0;
        }

        private void CreatePhysicalDevice()
        {
            uint deviceCount = 0;
            _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, null);
            if (deviceCount == 0)
            {
                throw new InvalidOperationException("No physical devices exist.");
            }

            Silk.NET.Vulkan.PhysicalDevice[] physicalDevices = new Silk.NET.Vulkan.PhysicalDevice[deviceCount];
            _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, ref physicalDevices[0]);
            // Just use the first one.
            _physicalDevice = physicalDevices[0];

            _vk.GetPhysicalDeviceProperties(_physicalDevice, out _physicalDeviceProperties);
            fixed (byte* utf8NamePtr = _physicalDeviceProperties.DeviceName)
            {
                _deviceName = Encoding.UTF8.GetString(utf8NamePtr, (int)MaxPhysicalDeviceNameSize).TrimEnd('\0');
            }

            _vendorName = "id:" + _physicalDeviceProperties.VendorID.ToString("x8");
            _apiVersion = GraphicsApiVersion.Unknown;
            _driverInfo = "version:" + _physicalDeviceProperties.DriverVersion.ToString("x8");

            _vk.GetPhysicalDeviceFeatures(_physicalDevice, out _physicalDeviceFeatures);

            _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out _physicalDeviceMemProperties);
        }

        public Silk.NET.Vulkan.ExtensionProperties[] GetDeviceExtensionProperties()
        {
            uint propertyCount = 0;
            var result = _vk.EnumerateDeviceExtensionProperties(_physicalDevice, (byte*)null, &propertyCount, null);
            CheckResult(result);
            Silk.NET.Vulkan.ExtensionProperties[] props = new Silk.NET.Vulkan.ExtensionProperties[(int)propertyCount];
            fixed (Silk.NET.Vulkan.ExtensionProperties* properties = props)
            {
                result = _vk.EnumerateDeviceExtensionProperties(_physicalDevice, (byte*)null, &propertyCount, properties);
                CheckResult(result);
            }
            return props;
        }

        private void CreateLogicalDevice(Silk.NET.Vulkan.SurfaceKHR surface, bool preferStandardClipY, VulkanDeviceOptions options)
        {
            GetQueueFamilyIndices(surface);

            HashSet<uint> familyIndices = new HashSet<uint> { _graphicsQueueIndex, _presentQueueIndex };
            Silk.NET.Vulkan.DeviceQueueCreateInfo* queueCreateInfos = stackalloc Silk.NET.Vulkan.DeviceQueueCreateInfo[familyIndices.Count];
            uint queueCreateInfosCount = (uint)familyIndices.Count;

            int i = 0;
            foreach (uint index in familyIndices)
            {
                Silk.NET.Vulkan.DeviceQueueCreateInfo queueCreateInfo = new Silk.NET.Vulkan.DeviceQueueCreateInfo();
                queueCreateInfo.SType = Silk.NET.Vulkan.StructureType.DeviceQueueCreateInfo;

                queueCreateInfo.QueueFamilyIndex = _graphicsQueueIndex;
                queueCreateInfo.QueueCount = 1;
                float priority = 1f;
                queueCreateInfo.PQueuePriorities = &priority;
                queueCreateInfos[i] = queueCreateInfo;
                i += 1;
            }

            Silk.NET.Vulkan.PhysicalDeviceFeatures deviceFeatures = _physicalDeviceFeatures;

            Silk.NET.Vulkan.ExtensionProperties[] props = GetDeviceExtensionProperties();

            HashSet<string> requiredInstanceExtensions = new HashSet<string>(options.DeviceExtensions ?? Array.Empty<string>());

            bool hasMemReqs2 = false;
            bool hasDedicatedAllocation = false;
            bool hasDriverProperties = false;
            IntPtr[] activeExtensions = new IntPtr[props.Length];
            uint activeExtensionCount = 0;

            fixed (Silk.NET.Vulkan.ExtensionProperties* properties = props)
            {
                for (int property = 0; property < props.Length; property++)
                {
                    string extensionName = Util.GetString(properties[property].ExtensionName);
                    if (extensionName == "VK_EXT_debug_marker")
                    {
                        activeExtensions[activeExtensionCount++] = CommonStrings.VK_EXT_DEBUG_MARKER_EXTENSION_NAME;
                        requiredInstanceExtensions.Remove(extensionName);
                        _debugMarkerEnabled = true;
                    }
                    else if (extensionName == "VK_KHR_swapchain")
                    {
                        activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].ExtensionName;
                        requiredInstanceExtensions.Remove(extensionName);
                    }
                    else if (preferStandardClipY && extensionName == "VK_KHR_maintenance1")
                    {
                        activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].ExtensionName;
                        requiredInstanceExtensions.Remove(extensionName);
                        _standardClipYDirection = true;
                    }
                    else if (extensionName == "VK_KHR_get_memory_requirements2")
                    {
                        activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].ExtensionName;
                        requiredInstanceExtensions.Remove(extensionName);
                        hasMemReqs2 = true;
                    }
                    else if (extensionName == "VK_KHR_dedicated_allocation")
                    {
                        activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].ExtensionName;
                        requiredInstanceExtensions.Remove(extensionName);
                        hasDedicatedAllocation = true;
                    }
                    else if (extensionName == "VK_KHR_driver_properties")
                    {
                        activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].ExtensionName;
                        requiredInstanceExtensions.Remove(extensionName);
                        hasDriverProperties = true;
                    }
                    else if (requiredInstanceExtensions.Remove(extensionName))
                    {
                        activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].ExtensionName;
                    }
                }
            }

            if (requiredInstanceExtensions.Count != 0)
            {
                string missingList = string.Join(", ", requiredInstanceExtensions);
                throw new VeldridException(
                    $"The following Vulkan device extensions were not available: {missingList}");
            }

            Silk.NET.Vulkan.DeviceCreateInfo deviceCreateInfo = new Silk.NET.Vulkan.DeviceCreateInfo();
            deviceCreateInfo.SType = Silk.NET.Vulkan.StructureType.DeviceCreateInfo;

            deviceCreateInfo.QueueCreateInfoCount = queueCreateInfosCount;
            deviceCreateInfo.PQueueCreateInfos = queueCreateInfos;

            deviceCreateInfo.PEnabledFeatures = &deviceFeatures;

            StackList<IntPtr> layerNames = new StackList<IntPtr>();
            if (_standardValidationSupported)
            {
                layerNames.Add(CommonStrings.StandardValidationLayerName);
            }
            if (_khronosValidationSupported)
            {
                layerNames.Add(CommonStrings.KhronosValidationLayerName);
            }
            deviceCreateInfo.EnabledLayerCount = layerNames.Count;
            deviceCreateInfo.PpEnabledLayerNames = (byte**)layerNames.Data;

            fixed (IntPtr* activeExtensionsPtr = activeExtensions)
            {
                deviceCreateInfo.EnabledExtensionCount = activeExtensionCount;
                deviceCreateInfo.PpEnabledExtensionNames = (byte**)activeExtensionsPtr;

                var result = _vk.CreateDevice(_physicalDevice, &deviceCreateInfo, null, out _device);
                CheckResult(result);
            }

            _vk.GetDeviceQueue(_device, _graphicsQueueIndex, 0, out _graphicsQueue);

            if (_debugMarkerEnabled)
            {
                _setObjectNameDelegate = Marshal.GetDelegateForFunctionPointer<vkDebugMarkerSetObjectNameEXT_t>(
                    GetInstanceProcAddr("vkDebugMarkerSetObjectNameEXT"));
                _markerBegin = Marshal.GetDelegateForFunctionPointer<vkCmdDebugMarkerBeginEXT_t>(
                    GetInstanceProcAddr("vkCmdDebugMarkerBeginEXT"));
                _markerEnd = Marshal.GetDelegateForFunctionPointer<vkCmdDebugMarkerEndEXT_t>(
                    GetInstanceProcAddr("vkCmdDebugMarkerEndEXT"));
                _markerInsert = Marshal.GetDelegateForFunctionPointer<vkCmdDebugMarkerInsertEXT_t>(
                    GetInstanceProcAddr("vkCmdDebugMarkerInsertEXT"));
            }
            if (hasDedicatedAllocation && hasMemReqs2)
            {
                _getBufferMemoryRequirements2 = GetDeviceProcAddr<vkGetBufferMemoryRequirements2_t>("vkGetBufferMemoryRequirements2")
                    ?? GetDeviceProcAddr<vkGetBufferMemoryRequirements2_t>("vkGetBufferMemoryRequirements2KHR");
                _getImageMemoryRequirements2 = GetDeviceProcAddr<vkGetImageMemoryRequirements2_t>("vkGetImageMemoryRequirements2")
                    ?? GetDeviceProcAddr<vkGetImageMemoryRequirements2_t>("vkGetImageMemoryRequirements2KHR");
            }

            if (_getPhysicalDeviceProperties2 != null && hasDriverProperties)
            {
                Silk.NET.Vulkan.PhysicalDeviceProperties2KHR deviceProps = new Silk.NET.Vulkan.PhysicalDeviceProperties2KHR();
                deviceProps.SType = Silk.NET.Vulkan.StructureType.PhysicalDeviceProperties2Khr;

                Silk.NET.Vulkan.PhysicalDeviceDriverProperties driverProps = new Silk.NET.Vulkan.PhysicalDeviceDriverProperties();
                driverProps.SType = Silk.NET.Vulkan.StructureType.PhysicalDeviceDriverProperties;

                deviceProps.PNext = &driverProps;
                _getPhysicalDeviceProperties2(_physicalDevice, &deviceProps);

                string driverName = Encoding.UTF8.GetString(
                    driverProps.DriverName, VkPhysicalDeviceDriverProperties.DriverNameLength).TrimEnd('\0');

                string driverInfo = Encoding.UTF8.GetString(
                    driverProps.DriverInfo, VkPhysicalDeviceDriverProperties.DriverInfoLength).TrimEnd('\0');

                var conforming = driverProps.ConformanceVersion;
                _apiVersion = new GraphicsApiVersion(conforming.Major, conforming.Minor, conforming.Subminor, conforming.Patch);
                _driverName = driverName;
                _driverInfo = driverInfo;
            }
        }

        private IntPtr GetInstanceProcAddr(string name)
        {
            int byteCount = Encoding.UTF8.GetByteCount(name);
            byte* utf8Ptr = stackalloc byte[byteCount + 1];

            fixed (char* namePtr = name)
            {
                Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
            }
            utf8Ptr[byteCount] = 0;

            return _vk.GetInstanceProcAddr(_instance, utf8Ptr);
        }

        private T GetInstanceProcAddr<T>(string name)
        {
            IntPtr funcPtr = GetInstanceProcAddr(name);
            if (funcPtr != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
            }
            return default;
        }

        private IntPtr GetDeviceProcAddr(string name)
        {
            int byteCount = Encoding.UTF8.GetByteCount(name);
            byte* utf8Ptr = stackalloc byte[byteCount + 1];

            fixed (char* namePtr = name)
            {
                Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
            }
            utf8Ptr[byteCount] = 0;

            return _vk.GetDeviceProcAddr(_device, utf8Ptr);
        }

        private T GetDeviceProcAddr<T>(string name)
        {
            IntPtr funcPtr = GetDeviceProcAddr(name);
            if (funcPtr != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
            }
            return default;
        }

        private void GetQueueFamilyIndices(Silk.NET.Vulkan.SurfaceKHR surface)
        {
            uint queueFamilyCount = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, null);
            Silk.NET.Vulkan.QueueFamilyProperties[] qfp = new Silk.NET.Vulkan.QueueFamilyProperties[queueFamilyCount];
            _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, out qfp[0]);

            bool foundGraphics = false;
            bool foundPresent = surface.Handle == default;

            for (uint i = 0; i < qfp.Length; i++)
            {
                if ((qfp[i].QueueFlags & Silk.NET.Vulkan.QueueFlags.QueueGraphicsBit) != 0)
                {
                    _graphicsQueueIndex = i;
                    foundGraphics = true;
                }

                if (!foundPresent)
                {
                    vkSurface.GetPhysicalDeviceSurfaceSupport(_physicalDevice, i, surface, out Silk.NET.Core.Bool32 presentSupported);
                    if (presentSupported)
                    {
                        _presentQueueIndex = i;
                        foundPresent = true;
                    }
                }

                if (foundGraphics && foundPresent)
                {
                    return;
                }
            }
        }

        private void CreateDescriptorPool()
        {
            _descriptorPoolManager = new VkDescriptorPoolManager(this);
        }

        private void CreateGraphicsCommandPool()
        {
            Silk.NET.Vulkan.CommandPoolCreateInfo commandPoolCI = new Silk.NET.Vulkan.CommandPoolCreateInfo();
            commandPoolCI.SType = Silk.NET.Vulkan.StructureType.CommandPoolCreateInfo;

            commandPoolCI.Flags = Silk.NET.Vulkan.CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit;
            commandPoolCI.QueueFamilyIndex = _graphicsQueueIndex;
            var result = _vk.CreateCommandPool(_device, &commandPoolCI, null, out _graphicsCommandPool);
            CheckResult(result);
        }

        protected override MappedResource MapCore(MappableResource resource, MapMode mode, uint subresource)
        {
            VkMemoryBlock memoryBlock = default(VkMemoryBlock);
            IntPtr mappedPtr = IntPtr.Zero;
            uint sizeInBytes;
            uint offset = 0;
            uint rowPitch = 0;
            uint depthPitch = 0;
            if (resource is VkBuffer buffer)
            {
                memoryBlock = buffer.Memory;
                sizeInBytes = buffer.SizeInBytes;
            }
            else
            {
                VkTexture texture = Util.AssertSubtype<MappableResource, VkTexture>(resource);
                Silk.NET.Vulkan.SubresourceLayout layout = texture.GetSubresourceLayout(subresource);
                memoryBlock = texture.Memory;
                sizeInBytes = (uint)layout.Size;
                offset = (uint)layout.Offset;
                rowPitch = (uint)layout.RowPitch;
                depthPitch = (uint)layout.DepthPitch;
            }

            if (memoryBlock.DeviceMemory.Handle != 0)
            {
                if (memoryBlock.IsPersistentMapped)
                {
                    mappedPtr = (IntPtr)memoryBlock.BlockMappedPointer;
                }
                else
                {
                    mappedPtr = _memoryManager.Map(memoryBlock);
                }
            }

            byte* dataPtr = (byte*)mappedPtr.ToPointer() + offset;
            return new MappedResource(
                resource,
                mode,
                (IntPtr)dataPtr,
                sizeInBytes,
                subresource,
                rowPitch,
                depthPitch);
        }

        protected override void UnmapCore(MappableResource resource, uint subresource)
        {
            VkMemoryBlock memoryBlock = default(VkMemoryBlock);
            if (resource is VkBuffer buffer)
            {
                memoryBlock = buffer.Memory;
            }
            else
            {
                VkTexture tex = Util.AssertSubtype<MappableResource, VkTexture>(resource);
                memoryBlock = tex.Memory;
            }

            if (memoryBlock.DeviceMemory.Handle != 0 && !memoryBlock.IsPersistentMapped)
            {
                _vk.UnmapMemory(_device, memoryBlock.DeviceMemory);
            }
        }

        protected override void PlatformDispose()
        {
            Debug.Assert(_submittedFences.Count == 0);
            foreach (Silk.NET.Vulkan.Fence fence in _availableSubmissionFences)
            {
                _vk.DestroyFence(_device, fence, null);
            }

            _mainSwapchain?.Dispose();
            if (_debugCallbackFunc.Handle != default)
            {
                _debugCallbackFunc = default;
                IntPtr destroyFuncPtr = _vk.GetInstanceProcAddr(_instance, "vkDestroyDebugReportCallbackEXT");
                vkDestroyDebugReportCallbackEXT_d destroyDel
                    = Marshal.GetDelegateForFunctionPointer<vkDestroyDebugReportCallbackEXT_d>(destroyFuncPtr);
                destroyDel(_instance, _debugCallbackHandle, null);
            }

            _descriptorPoolManager.DestroyAll();
            _vk.DestroyCommandPool(_device, _graphicsCommandPool, null);

            Debug.Assert(_submittedStagingTextures.Count == 0);
            foreach (VkTexture tex in _availableStagingTextures)
            {
                tex.Dispose();
            }

            Debug.Assert(_submittedStagingBuffers.Count == 0);
            foreach (VkBuffer buffer in _availableStagingBuffers)
            {
                buffer.Dispose();
            }

            lock (_graphicsCommandPoolLock)
            {
                while (_sharedGraphicsCommandPools.Count > 0)
                {
                    SharedCommandPool sharedPool = _sharedGraphicsCommandPools.Pop();
                    sharedPool.Destroy();
                }
            }

            _memoryManager.Dispose();

            var result = _vk.DeviceWaitIdle(_device);
            CheckResult(result);
            _vk.DestroyDevice(_device, null);
            _vk.DestroyInstance(_instance, null);
        }

        private protected override void WaitForIdleCore()
        {
            lock (_graphicsQueueLock)
            {
                _vk.QueueWaitIdle(_graphicsQueue);
            }

            CheckSubmittedFences();
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            Silk.NET.Vulkan.ImageUsageFlags usageFlags = Silk.NET.Vulkan.ImageUsageFlags.ImageUsageSampledBit;
            usageFlags |= depthFormat ? Silk.NET.Vulkan.ImageUsageFlags.ImageUsageDepthStencilAttachmentBit : Silk.NET.Vulkan.ImageUsageFlags.ImageUsageColorAttachmentBit;

           _vk.GetPhysicalDeviceImageFormatProperties(
                _physicalDevice,
                VkFormats.VdToVkPixelFormat(format),
                 Silk.NET.Vulkan.ImageType.ImageType2D,
                 Silk.NET.Vulkan.ImageTiling.Optimal,
                usageFlags,
                 0,
                out Silk.NET.Vulkan.ImageFormatProperties formatProperties);

            Silk.NET.Vulkan.SampleCountFlags vkSampleCounts = formatProperties.SampleCounts;
            if ((vkSampleCounts & Silk.NET.Vulkan.SampleCountFlags.SampleCount32Bit) == Silk.NET.Vulkan.SampleCountFlags.SampleCount32Bit)
            {
                return TextureSampleCount.Count32;
            }
            else if ((vkSampleCounts & Silk.NET.Vulkan.SampleCountFlags.SampleCount16Bit) == Silk.NET.Vulkan.SampleCountFlags.SampleCount16Bit)
            {
                return TextureSampleCount.Count16;
            }
            else if ((vkSampleCounts & Silk.NET.Vulkan.SampleCountFlags.SampleCount8Bit) == Silk.NET.Vulkan.SampleCountFlags.SampleCount8Bit)
            {
                return TextureSampleCount.Count8;
            }
            else if ((vkSampleCounts & Silk.NET.Vulkan.SampleCountFlags.SampleCount4Bit) == Silk.NET.Vulkan.SampleCountFlags.SampleCount4Bit)
            {
                return TextureSampleCount.Count4;
            }
            else if ((vkSampleCounts & Silk.NET.Vulkan.SampleCountFlags.SampleCount2Bit) == Silk.NET.Vulkan.SampleCountFlags.SampleCount2Bit)
            {
                return TextureSampleCount.Count2;
            }

            return TextureSampleCount.Count1;
        }

        private protected override bool GetPixelFormatSupportCore(
            PixelFormat format,
            TextureType type,
            TextureUsage usage,
            out PixelFormatProperties properties)
        {
            Silk.NET.Vulkan.Format vkFormat = VkFormats.VdToVkPixelFormat(format, (usage & TextureUsage.DepthStencil) != 0);
            Silk.NET.Vulkan.ImageType vkType = VkFormats.VdToVkTextureType(type);
            Silk.NET.Vulkan.ImageTiling tiling = usage == TextureUsage.Staging ? Silk.NET.Vulkan.ImageTiling.Linear : Silk.NET.Vulkan.ImageTiling.Optimal;
            Silk.NET.Vulkan.ImageUsageFlags vkUsage = VkFormats.VdToVkTextureUsage(usage);

            var result =_vk.GetPhysicalDeviceImageFormatProperties(
                _physicalDevice,
                vkFormat,
                vkType,
                tiling,
                vkUsage,
                0,
                out Silk.NET.Vulkan.ImageFormatProperties vkProps);

            if (result == Silk.NET.Vulkan.Result.ErrorFormatNotSupported)
            {
                properties = default(PixelFormatProperties);
                return false;
            }
            CheckResult(result);

            properties = new PixelFormatProperties(
               vkProps.MaxExtent.Width,
               vkProps.MaxExtent.Height,
               vkProps.MaxExtent.Depth,
               vkProps.MaxMipLevels,
               vkProps.MaxArrayLayers,
               (uint)vkProps.SampleCounts);
            return true;
        }

        internal Silk.NET.Vulkan.Filter GetFormatFilter(Silk.NET.Vulkan.Format format)
        {
            if (!_filters.TryGetValue(format, out Silk.NET.Vulkan.Filter filter))
            {
                _vk.GetPhysicalDeviceFormatProperties(_physicalDevice, format, out Silk.NET.Vulkan.FormatProperties vkFormatProps);
                filter = (vkFormatProps.OptimalTilingFeatures & Silk.NET.Vulkan.FormatFeatureFlags.FormatFeatureSampledImageFilterLinearBit) != 0
                    ? Silk.NET.Vulkan.Filter.Linear
                    : Silk.NET.Vulkan.Filter.Nearest;
                _filters.TryAdd(format, filter);
            }

            return filter;
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
            VkBuffer copySrcVkBuffer = null;
            IntPtr mappedPtr;
            byte* destPtr;
            bool isPersistentMapped = vkBuffer.Memory.IsPersistentMapped;
            if (isPersistentMapped)
            {
                mappedPtr = (IntPtr)vkBuffer.Memory.BlockMappedPointer;
                destPtr = (byte*)mappedPtr + bufferOffsetInBytes;
            }
            else
            {
                copySrcVkBuffer = GetFreeStagingBuffer(sizeInBytes);
                mappedPtr = (IntPtr)copySrcVkBuffer.Memory.BlockMappedPointer;
                destPtr = (byte*)mappedPtr;
            }

            Unsafe.CopyBlock(destPtr, source.ToPointer(), sizeInBytes);

            if (!isPersistentMapped)
            {
                SharedCommandPool pool = GetFreeCommandPool();
                Silk.NET.Vulkan.CommandBuffer cb = pool.BeginNewCommandBuffer();

                Silk.NET.Vulkan.BufferCopy copyRegion = new Silk.NET.Vulkan.BufferCopy
                {
                    DstOffset = bufferOffsetInBytes,
                    Size = sizeInBytes
                };
                _vk.CmdCopyBuffer(cb, copySrcVkBuffer.DeviceBuffer, vkBuffer.DeviceBuffer, 1, &copyRegion);

                pool.EndAndSubmit(cb);
                lock (_stagingResourcesLock)
                {
                    _submittedStagingBuffers.Add(cb, copySrcVkBuffer);
                }
            }
        }

        private SharedCommandPool GetFreeCommandPool()
        {
            SharedCommandPool sharedPool = null;
            lock (_graphicsCommandPoolLock)
            {
                if (_sharedGraphicsCommandPools.Count > 0)
                    sharedPool = _sharedGraphicsCommandPools.Pop();
            }

            if (sharedPool == null)
                sharedPool = new SharedCommandPool(this, false);

            return sharedPool;
        }

        private IntPtr MapBuffer(VkBuffer buffer, uint numBytes)
        {
            if (buffer.Memory.IsPersistentMapped)
            {
                return (IntPtr)buffer.Memory.BlockMappedPointer;
            }
            else
            {
                void* mappedPtr;
                Silk.NET.Vulkan.Result result = _vk.MapMemory(Device, buffer.Memory.DeviceMemory, buffer.Memory.Offset, numBytes, 0, &mappedPtr);
                CheckResult(result);
                return (IntPtr)mappedPtr;
            }
        }

        private void UnmapBuffer(VkBuffer buffer)
        {
            if (!buffer.Memory.IsPersistentMapped)
            {
                _vk.UnmapMemory(Device, buffer.Memory.DeviceMemory);
            }
        }

        private protected override void UpdateTextureCore(
            Texture texture,
            IntPtr source,
            uint sizeInBytes,
            uint x,
            uint y,
            uint z,
            uint width,
            uint height,
            uint depth,
            uint mipLevel,
            uint arrayLayer)
        {
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(texture);
            bool isStaging = (vkTex.Usage & TextureUsage.Staging) != 0;
            if (isStaging)
            {
                VkMemoryBlock memBlock = vkTex.Memory;
                uint subresource = texture.CalculateSubresource(mipLevel, arrayLayer);
                Silk.NET.Vulkan.SubresourceLayout layout = vkTex.GetSubresourceLayout(subresource);
                byte* imageBasePtr = (byte*)memBlock.BlockMappedPointer + layout.Offset;

                uint srcRowPitch = FormatHelpers.GetRowPitch(width, texture.Format);
                uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, texture.Format);
                Util.CopyTextureRegion(
                    source.ToPointer(),
                    0, 0, 0,
                    srcRowPitch, srcDepthPitch,
                    imageBasePtr,
                    x, y, z,
                    (uint)layout.RowPitch, (uint)layout.DepthPitch,
                    width, height, depth,
                    texture.Format);
            }
            else
            {
                VkTexture stagingTex = GetFreeStagingTexture(width, height, depth, texture.Format);
                UpdateTexture(stagingTex, source, sizeInBytes, 0, 0, 0, width, height, depth, 0, 0);
                SharedCommandPool pool = GetFreeCommandPool();
                Silk.NET.Vulkan.CommandBuffer cb = pool.BeginNewCommandBuffer();
                VkCommandList.CopyTextureCore_VkCommandBuffer(
                    _vk,
                    cb,
                    stagingTex, 0, 0, 0, 0, 0,
                    texture, x, y, z, mipLevel, arrayLayer,
                    width, height, depth, 1);
                lock (_stagingResourcesLock)
                {
                    _submittedStagingTextures.Add(cb, stagingTex);
                }
                pool.EndAndSubmit(cb);
            }
        }

        private VkTexture GetFreeStagingTexture(uint width, uint height, uint depth, PixelFormat format)
        {
            uint totalSize = FormatHelpers.GetRegionSize(width, height, depth, format);
            lock (_stagingResourcesLock)
            {
                for (int i = 0; i < _availableStagingTextures.Count; i++)
                {
                    VkTexture tex = _availableStagingTextures[i];
                    if (tex.Memory.Size >= totalSize)
                    {
                        _availableStagingTextures.RemoveAt(i);
                        tex.SetStagingDimensions(width, height, depth, format);
                        return tex;
                    }
                }
            }

            uint texWidth = Math.Max(256, width);
            uint texHeight = Math.Max(256, height);
            VkTexture newTex = (VkTexture)ResourceFactory.CreateTexture(TextureDescription.Texture3D(
                texWidth, texHeight, depth, 1, format, TextureUsage.Staging));
            newTex.SetStagingDimensions(width, height, depth, format);

            return newTex;
        }

        private VkBuffer GetFreeStagingBuffer(uint size)
        {
            lock (_stagingResourcesLock)
            {
                for (int i = 0; i < _availableStagingBuffers.Count; i++)
                {
                    VkBuffer buffer = _availableStagingBuffers[i];
                    if (buffer.SizeInBytes >= size)
                    {
                        _availableStagingBuffers.RemoveAt(i);
                        return buffer;
                    }
                }
            }

            uint newBufferSize = Math.Max(MinStagingBufferSize, size);
            VkBuffer newBuffer = (VkBuffer)ResourceFactory.CreateBuffer(
                new BufferDescription(newBufferSize, BufferUsage.Staging));
            return newBuffer;
        }

        public override void ResetFence(Fence fence)
        {
            Silk.NET.Vulkan.Fence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
            _vk.ResetFences(_device, 1, &vkFence);
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            Silk.NET.Vulkan.Fence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
            var result = _vk.WaitForFences(_device, 1, &vkFence, true, nanosecondTimeout);
            return result == Silk.NET.Vulkan.Result.Success;
        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            int fenceCount = fences.Length;
            Silk.NET.Vulkan.Fence* fencesPtr = stackalloc Silk.NET.Vulkan.Fence[fenceCount];
            for (int i = 0; i < fenceCount; i++)
            {
                fencesPtr[i] = Util.AssertSubtype<Fence, VkFence>(fences[i]).DeviceFence;
            }

            var result = _vk.WaitForFences(_device, (uint)fenceCount, fencesPtr, waitAll, nanosecondTimeout);
            return result == Silk.NET.Vulkan.Result.Success;
        }

        internal static bool IsSupported()
        {
            return s_isSupported.Value;
        }

        private static bool CheckIsSupported()
        {
            if (!IsVulkanLoaded())
            {
                return false;
            }
            Silk.NET.Vulkan.Vk _vk = Silk.NET.Vulkan.Vk.GetApi();

            Silk.NET.Vulkan.InstanceCreateInfo instanceCI =  new Silk.NET.Vulkan.InstanceCreateInfo();
            instanceCI.SType = Silk.NET.Vulkan.StructureType.InstanceCreateInfo;

            Silk.NET.Vulkan.ApplicationInfo applicationInfo = new Silk.NET.Vulkan.ApplicationInfo();
            applicationInfo.ApiVersion = new VkVersion(1, 0, 0);
            applicationInfo.ApplicationVersion = new VkVersion(1, 0, 0);
            applicationInfo.EngineVersion = new VkVersion(1, 0, 0);
            applicationInfo.PApplicationName = s_name;
            applicationInfo.PEngineName = s_name;
            applicationInfo.SType = Silk.NET.Vulkan.StructureType.ApplicationInfo;

            instanceCI.PApplicationInfo = &applicationInfo;

            var result = _vk.CreateInstance(&instanceCI, null, out Silk.NET.Vulkan.Instance testInstance);
            if (result != Silk.NET.Vulkan.Result.Success)
            {
                return false;
            }

            uint physicalDeviceCount = 0;
            result = _vk.EnumeratePhysicalDevices(testInstance, ref physicalDeviceCount, null);
            if (result != Silk.NET.Vulkan.Result.Success || physicalDeviceCount == 0)
            {
                _vk.DestroyInstance(testInstance, null);
                return false;
            }

            _vk.DestroyInstance(testInstance, null);

            HashSet<string> instanceExtensions = new HashSet<string>(GetInstanceExtensions());
            if (!instanceExtensions.Contains(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME))
            {
                return false;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return instanceExtensions.Contains(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
            }
#if NET5_0_OR_GREATER
            else if (OperatingSystem.IsAndroid())
            {
                return instanceExtensions.Contains(CommonStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
            }
#endif
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.OSDescription.Contains("Unix")) // Android
                {
                    return instanceExtensions.Contains(CommonStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
                }
                else
                {
                    return instanceExtensions.Contains(CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.OSDescription.Contains("Darwin")) // macOS
                {
                    return instanceExtensions.Contains(CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME);
                }
                else // iOS
                {
                    return instanceExtensions.Contains(CommonStrings.VK_MVK_IOS_SURFACE_EXTENSION_NAME);
                }
            }

            return false;
        }

        internal void ClearColorTexture(VkTexture texture, Silk.NET.Vulkan.ClearColorValue color)
        {
            uint effectiveLayers = texture.ArrayLayers;
            if ((texture.Usage & TextureUsage.Cubemap) != 0)
            {
                effectiveLayers *= 6;
            }
            Silk.NET.Vulkan.ImageSubresourceRange range = new Silk.NET.Vulkan.ImageSubresourceRange(
                 Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                 0,
                 texture.MipLevels,
                 0,
                 effectiveLayers);

            SharedCommandPool pool = GetFreeCommandPool();
            Silk.NET.Vulkan.CommandBuffer cb = pool.BeginNewCommandBuffer();
            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, Silk.NET.Vulkan.ImageLayout.TransferDstOptimal);
            _vk.CmdClearColorImage(cb, texture.OptimalDeviceImage, Silk.NET.Vulkan.ImageLayout.TransferDstOptimal, &color, 1, &range);
            Silk.NET.Vulkan.ImageLayout colorLayout = texture.IsSwapchainTexture ? Silk.NET.Vulkan.ImageLayout.PresentSrcKhr : Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal;
            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, colorLayout);
            pool.EndAndSubmit(cb);
        }

        internal void ClearDepthTexture(VkTexture texture, Silk.NET.Vulkan.ClearDepthStencilValue clearValue)
        {
            uint effectiveLayers = texture.ArrayLayers;
            if ((texture.Usage & TextureUsage.Cubemap) != 0)
            {
                effectiveLayers *= 6;
            }
            Silk.NET.Vulkan.ImageAspectFlags aspect = FormatHelpers.IsStencilFormat(texture.Format)
                ? Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit | Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit
                : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit;
            Silk.NET.Vulkan.ImageSubresourceRange range = new Silk.NET.Vulkan.ImageSubresourceRange(
                aspect,
                0,
                texture.MipLevels,
                0,
                effectiveLayers);



            SharedCommandPool pool = GetFreeCommandPool();
            Silk.NET.Vulkan.CommandBuffer cb = pool.BeginNewCommandBuffer();
            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, Silk.NET.Vulkan.ImageLayout.TransferDstOptimal);
            _vk.CmdClearDepthStencilImage(
                cb,
                texture.OptimalDeviceImage,
                Silk.NET.Vulkan.ImageLayout.TransferDstOptimal,
                &clearValue,
                1,
                &range);
            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal);
            pool.EndAndSubmit(cb);
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore()
            => (uint)_physicalDeviceProperties.Limits.MinUniformBufferOffsetAlignment;

        internal override uint GetStructuredBufferMinOffsetAlignmentCore()
            => (uint)_physicalDeviceProperties.Limits.MinStorageBufferOffsetAlignment;

        internal void TransitionImageLayout(VkTexture texture, Silk.NET.Vulkan.ImageLayout layout)
        {
            SharedCommandPool pool = GetFreeCommandPool();
            Silk.NET.Vulkan.CommandBuffer cb = pool.BeginNewCommandBuffer();
            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, texture.ArrayLayers, layout);
            pool.EndAndSubmit(cb);
        }


        private class SharedCommandPool
        {
            private readonly VkGraphicsDevice _gd;
            private readonly Silk.NET.Vulkan.Vk _vk;
            private readonly Silk.NET.Vulkan.CommandPool _pool;
            private readonly Silk.NET.Vulkan.CommandBuffer _cb;

            public bool IsCached { get; }

            public SharedCommandPool(VkGraphicsDevice gd, bool isCached)
            {
                _gd = gd;
                _vk = gd.vk;

                IsCached = isCached;

                Silk.NET.Vulkan.CommandPoolCreateInfo commandPoolCI = new Silk.NET.Vulkan.CommandPoolCreateInfo();
                commandPoolCI.SType = Silk.NET.Vulkan.StructureType.CommandPoolCreateInfo;

                commandPoolCI.Flags = Silk.NET.Vulkan.CommandPoolCreateFlags.CommandPoolCreateTransientBit | Silk.NET.Vulkan.CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit;
                commandPoolCI.QueueFamilyIndex = _gd.GraphicsQueueIndex;
                var result = _vk.CreateCommandPool(_gd.Device, &commandPoolCI, null, out _pool);
                CheckResult(result);

                Silk.NET.Vulkan.CommandBufferAllocateInfo allocateInfo = new Silk.NET.Vulkan.CommandBufferAllocateInfo();
                allocateInfo.SType = Silk.NET.Vulkan.StructureType.CommandBufferAllocateInfo;

                allocateInfo.CommandBufferCount = 1;
                allocateInfo.Level = Silk.NET.Vulkan.CommandBufferLevel.Primary;
                allocateInfo.CommandPool = _pool;
                result = _vk.AllocateCommandBuffers(_gd.Device, &allocateInfo, out _cb);
                CheckResult(result);
            }

            public Silk.NET.Vulkan.CommandBuffer BeginNewCommandBuffer()
            {
                Silk.NET.Vulkan.CommandBufferBeginInfo beginInfo = new Silk.NET.Vulkan.CommandBufferBeginInfo();
                beginInfo.SType = Silk.NET.Vulkan.StructureType.CommandBufferBeginInfo;

                beginInfo.Flags = Silk.NET.Vulkan.CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit;
                var result = _vk.BeginCommandBuffer(_cb, &beginInfo);
                CheckResult(result);

                return _cb;
            }

            public void EndAndSubmit(Silk.NET.Vulkan.CommandBuffer cb)
            {
                var result = _vk.EndCommandBuffer(cb);
                CheckResult(result);
                _gd.SubmitCommandBuffer(null, cb, 0, null, 0, null, null);
                lock (_gd._stagingResourcesLock)
                {
                    _gd._submittedSharedCommandPools.Add(cb, this);
                }
            }

            internal void Destroy()
            {
                _vk.DestroyCommandPool(_gd.Device, _pool, null);
            }
        }

        private struct FenceSubmissionInfo
        {
            public Silk.NET.Vulkan.Fence Fence;
            public VkCommandList CommandList;
            public Silk.NET.Vulkan.CommandBuffer CommandBuffer;
            public FenceSubmissionInfo(Silk.NET.Vulkan.Fence fence, VkCommandList commandList, Silk.NET.Vulkan.CommandBuffer commandBuffer)
            {
                Fence = fence;
                CommandList = commandList;
                CommandBuffer = commandBuffer;
            }
        }
    }

    internal unsafe delegate Silk.NET.Vulkan.Result vkCreateDebugReportCallbackEXT_d(
        Silk.NET.Vulkan.Instance instance,
        Silk.NET.Vulkan.DebugReportCallbackCreateInfoEXT* createInfo,
        IntPtr allocatorPtr,
        out Silk.NET.Vulkan.DebugReportCallbackEXT ret);

    internal unsafe delegate void vkDestroyDebugReportCallbackEXT_d(
        Silk.NET.Vulkan.Instance instance,
       Silk.NET.Vulkan.DebugReportCallbackEXT callback,
        Silk.NET.Vulkan.AllocationCallbacks* pAllocator);

    internal unsafe delegate Silk.NET.Vulkan.Result vkDebugMarkerSetObjectNameEXT_t(Silk.NET.Vulkan.Device device, Silk.NET.Vulkan.DebugMarkerObjectNameInfoEXT* pNameInfo);
    internal unsafe delegate void vkCmdDebugMarkerBeginEXT_t(Silk.NET.Vulkan.CommandBuffer commandBuffer, Silk.NET.Vulkan.DebugMarkerMarkerInfoEXT* pMarkerInfo);
    internal unsafe delegate void vkCmdDebugMarkerEndEXT_t(Silk.NET.Vulkan.CommandBuffer commandBuffer);
    internal unsafe delegate void vkCmdDebugMarkerInsertEXT_t(Silk.NET.Vulkan.CommandBuffer commandBuffer, Silk.NET.Vulkan.DebugMarkerMarkerInfoEXT* pMarkerInfo);

    internal unsafe delegate void vkGetBufferMemoryRequirements2_t(Silk.NET.Vulkan.Device device, Silk.NET.Vulkan.BufferMemoryRequirementsInfo2KHR* pInfo, Silk.NET.Vulkan.MemoryRequirements2KHR* pMemoryRequirements);
    internal unsafe delegate void vkGetImageMemoryRequirements2_t(Silk.NET.Vulkan.Device device, Silk.NET.Vulkan.ImageMemoryRequirementsInfo2KHR* pInfo, Silk.NET.Vulkan.MemoryRequirements2KHR* pMemoryRequirements);

    internal unsafe delegate void vkGetPhysicalDeviceProperties2_t(Silk.NET.Vulkan.PhysicalDevice physicalDevice, void* properties);

    // VK_EXT_metal_surface

    internal unsafe delegate Silk.NET.Vulkan.Result vkCreateMetalSurfaceEXT_t(
        Silk.NET.Vulkan.Instance instance,
        VkMetalSurfaceCreateInfoEXT* pCreateInfo,
        Silk.NET.Vulkan.AllocationCallbacks* pAllocator,
        Silk.NET.Vulkan.SurfaceKHR* pSurface);

    internal unsafe struct VkMetalSurfaceCreateInfoEXT
    {
        public const Silk.NET.Vulkan.StructureType VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT = (Silk.NET.Vulkan.StructureType)1000217000;

        public Silk.NET.Vulkan.StructureType sType;
        public void* pNext;
        public uint flags;
        public void* pLayer;
    }

    internal unsafe struct VkPhysicalDeviceDriverProperties
    {
        public const int DriverNameLength = 256;
        public const int DriverInfoLength = 256;
        public const Silk.NET.Vulkan.StructureType VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES = (Silk.NET.Vulkan.StructureType)1000196000;

        public Silk.NET.Vulkan.StructureType sType;
        public void* pNext;
        public VkDriverId driverID;
        public fixed byte driverName[DriverNameLength];
        public fixed byte driverInfo[DriverInfoLength];
        public Silk.NET.Vulkan.ConformanceVersion conformanceVersion;

        public static Silk.NET.Vulkan.PhysicalDeviceDriverProperties New()
        {
            return new Silk.NET.Vulkan.PhysicalDeviceDriverProperties() { SType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES };
        }
    }

    internal enum VkDriverId
    {
    }

}
