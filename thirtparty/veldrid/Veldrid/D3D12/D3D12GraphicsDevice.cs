using Vortice;
using Vortice.Direct3D12;
using Vortice.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Vortice.Mathematics;
using Vortice.Direct3D12.Debug;
using VorticeDXGI = Vortice.DXGI.DXGI;
using VorticeD3D12 = Vortice.Direct3D12.D3D12;
using Vortice.DXGI.Debug;

namespace Veldrid.D3D12
{
    internal class D3D12GraphicsDevice : GraphicsDevice
    {
        private readonly IDXGIAdapter _dxgiAdapter;
        private readonly ID3D12Device _device;
        private readonly string _deviceName;
        private readonly string _vendorName;
        private readonly GraphicsApiVersion _apiVersion;
        private readonly int _deviceId;
        private readonly D3D12ResourceFactory _d3d11ResourceFactory;
        private readonly D3D12Swapchain _mainSwapchain;
        private readonly bool _supportsConcurrentResources;
        private readonly bool _supportsCommandLists;
        private readonly object _immediateContextLock = new object();
        private readonly BackendInfoD3D11 _d3d11Info;

        private readonly object _mappedResourceLock = new object();
        private readonly Dictionary<MappedResourceCacheKey, MappedResourceInfo> _mappedResources
            = new Dictionary<MappedResourceCacheKey, MappedResourceInfo>();

        private readonly object _stagingResourcesLock = new object();
        private readonly List<D3D12Buffer> _availableStagingBuffers = new List<D3D12Buffer>();

        public override string DeviceName => _deviceName;

        public override string VendorName => _vendorName;

        public override GraphicsApiVersion ApiVersion => _apiVersion;

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D11;

        public override bool IsUvOriginTopLeft => true;

        public override bool IsDepthRangeZeroToOne => true;

        public override bool IsClipSpaceYInverted => false;

        public override ResourceFactory ResourceFactory => _d3d11ResourceFactory;

        public ID3D12Device Device => _device;

        public IDXGIAdapter Adapter => _dxgiAdapter;

        public bool IsDebugEnabled { get; }

        public bool SupportsConcurrentResources => _supportsConcurrentResources;

        public bool SupportsCommandLists => _supportsCommandLists;

        public int DeviceId => _deviceId;

        public override Swapchain MainSwapchain => _mainSwapchain;

        public override GraphicsDeviceFeatures Features { get; }

        public D3D12GraphicsDevice(GraphicsDeviceOptions options, D3D12DeviceOptions D3D12DeviceOptions, SwapchainDescription? swapchainDesc)
            : this(MergeOptions(D3D12DeviceOptions, options), swapchainDesc)
        {
        }

        public D3D12GraphicsDevice(D3D12DeviceOptions options, SwapchainDescription? swapchainDesc)
        {
            var flags = (DeviceCreationFlags)options.DeviceCreationFlags;
#if DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif
            // If debug flag set but SDK layers aren't available we can't enable debug.
            if (0 != (flags & DeviceCreationFlags.Debug) && !Vortice.Direct3D12.D3D11.SdkLayersAvailable())
            {
                flags &= ~DeviceCreationFlags.Debug;
            }

            try
            {
                if (options.AdapterPtr != IntPtr.Zero)
                {
                    VorticeD3D12.D3D12CreateDevice(options.AdapterPtr,
                        Vortice.Direct3D.FeatureLevel.Level_12_0,
                        out _device).CheckError();
                }
                else
                {
                    VorticeD3D12.D3D12CreateDevice(IntPtr.Zero,
                            Vortice.Direct3D.FeatureLevel.Level_12_0,
                   
                        out _device).CheckError();
                }
            }
            catch
            {
                VorticeD3D12.D3D12CreateDevice(IntPtr.Zero,
                    null,
                    out _device).CheckError();
            }

            using (IDXGIDevice dxgiDevice = _device.QueryInterface<IDXGIDevice>())
            {
                // Store a pointer to the DXGI adapter.
                // This is for the case of no preferred DXGI adapter, or fallback to WARP.
                dxgiDevice.GetAdapter(out _dxgiAdapter).CheckError();

                AdapterDescription desc = _dxgiAdapter.Description;
                _deviceName = desc.Description;
                _vendorName = "id:" + ((uint)desc.VendorId).ToString("x8");
                _deviceId = desc.DeviceId;
            }

            switch (_device.FeatureLevel)
            {
                case Vortice.Direct3D.FeatureLevel.Level_10_0:
                    _apiVersion = new GraphicsApiVersion(10, 0, 0, 0);
                    break;

                case Vortice.Direct3D.FeatureLevel.Level_10_1:
                    _apiVersion = new GraphicsApiVersion(10, 1, 0, 0);
                    break;

                case Vortice.Direct3D.FeatureLevel.Level_11_0:
                    _apiVersion = new GraphicsApiVersion(11, 0, 0, 0);
                    break;

                case Vortice.Direct3D.FeatureLevel.Level_11_1:
                    _apiVersion = new GraphicsApiVersion(11, 1, 0, 0);
                    break;

                case Vortice.Direct3D.FeatureLevel.Level_12_0:
                    _apiVersion = new GraphicsApiVersion(12, 0, 0, 0);
                    break;

                case Vortice.Direct3D.FeatureLevel.Level_12_1:
                    _apiVersion = new GraphicsApiVersion(12, 1, 0, 0);
                    break;

                case Vortice.Direct3D.FeatureLevel.Level_12_2:
                    _apiVersion = new GraphicsApiVersion(12, 2, 0, 0);
                    break;
            }

            if (swapchainDesc != null)
            {
                SwapchainDescription desc = swapchainDesc.Value;
                _mainSwapchain = new D3D12Swapchain(this, ref desc);
            }
            _immediateContext = _device.ImmediateContext;
            _device.CheckThreadingSupport(out _supportsConcurrentResources, out _supportsCommandLists);

            IsDebugEnabled = (flags & DeviceCreationFlags.Debug) != 0;

            Features = new GraphicsDeviceFeatures(
                computeShader: true,
                geometryShader: true,
                tessellationShaders: true,
                multipleViewports: true,
                samplerLodBias: true,
                drawBaseVertex: true,
                drawBaseInstance: true,
                drawIndirect: true,
                drawIndirectBaseInstance: true,
                fillModeWireframe: true,
                samplerAnisotropy: true,
                depthClipDisable: true,
                texture1D: true,
                independentBlend: true,
                structuredBuffer: true,
                subsetTextureView: true,
                commandListDebugMarkers: _device.FeatureLevel >= Vortice.Direct3D.FeatureLevel.Level_12_1,
                bufferRangeBinding: _device.FeatureLevel >= Vortice.Direct3D.FeatureLevel.Level_12_1,
                shaderFloat64: _device.CheckFeatureSupport<FeatureDataDoubles>(Vortice.Direct3D12.Feature.Doubles).DoublePrecisionFloatShaderOps);

            _d3d11ResourceFactory = new D3D12ResourceFactory(this);
            _d3d11Info = new BackendInfoD3D11(this);

            PostDeviceCreated();
        }

        private static D3D12DeviceOptions MergeOptions(D3D12DeviceOptions D3D12DeviceOptions, GraphicsDeviceOptions options)
        {
            if (options.Debug)
            {
                D3D12DeviceOptions.DeviceCreationFlags |= (uint)DeviceCreationFlags.Debug;
            }

            return D3D12DeviceOptions;
        }

        private protected override void SubmitCommandsCore(CommandList cl, Fence fence)
        {
            D3D12CommandList D3D12CL = Util.AssertSubtype<CommandList, D3D12CommandList>(cl);
            lock (_immediateContextLock)
            {
                if (d3d11CL.DeviceCommandList != null) // CommandList may have been reset in the meantime (resized swapchain).
                {
                    _immediateContext.ExecuteCommandList(d3d11CL.DeviceCommandList, false);
                    D3D12CL.OnCompleted();
                }
            }

            if (fence is D3D12Fence D3D12Fence)
            {
                D3D12Fence.Set();
            }
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
            lock (_immediateContextLock)
            {
                D3D12Swapchain D3D12SC = Util.AssertSubtype<Swapchain, D3D12Swapchain>(swapchain);
                D3D12SC.DxgiSwapChain.Present(d3d11SC.SyncInterval, PresentFlags.None);
            }
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            Format dxgiFormat = D3D12Formats.ToDxgiFormat(format, depthFormat);
            if (CheckFormatMultisample(dxgiFormat, 32))
            {
                return TextureSampleCount.Count32;
            }
            else if (CheckFormatMultisample(dxgiFormat, 16))
            {
                return TextureSampleCount.Count16;
            }
            else if (CheckFormatMultisample(dxgiFormat, 8))
            {
                return TextureSampleCount.Count8;
            }
            else if (CheckFormatMultisample(dxgiFormat, 4))
            {
                return TextureSampleCount.Count4;
            }
            else if (CheckFormatMultisample(dxgiFormat, 2))
            {
                return TextureSampleCount.Count2;
            }

            return TextureSampleCount.Count1;
        }

        private bool CheckFormatMultisample(Format format, int sampleCount)
        {
            return _device.CheckMultisampleQualityLevels(format, sampleCount) != 0;
        }

        private protected override bool GetPixelFormatSupportCore(
            PixelFormat format,
            TextureType type,
            TextureUsage usage,
            out PixelFormatProperties properties)
        {
            if (D3D12Formats.IsUnsupportedFormat(format))
            {
                properties = default(PixelFormatProperties);
                return false;
            }

            Format dxgiFormat = D3D12Formats.ToDxgiFormat(format, (usage & TextureUsage.DepthStencil) != 0);

            FormatSupport1 fs1;
            FormatSupport2 fs2;

            _device.CheckFormatSupport(dxgiFormat, out fs1, out fs2);

            if ((usage & TextureUsage.RenderTarget) != 0 && (fs1 & FormatSupport1.RenderTarget) == 0
                || (usage & TextureUsage.DepthStencil) != 0 && (fs1 & FormatSupport1.DepthStencil) == 0
                || (usage & TextureUsage.Sampled) != 0 && (fs1 & FormatSupport1.ShaderSample) == 0
                || (usage & TextureUsage.Cubemap) != 0 && (fs1 & FormatSupport1.TextureCube) == 0
                || (usage & TextureUsage.Storage) != 0 && (fs1 & FormatSupport1.TypedUnorderedAccessView) == 0)
            {
                properties = default(PixelFormatProperties);
                return false;
            }

            const uint MaxTextureDimension = 16384;
            const uint MaxVolumeExtent = 2048;

            uint sampleCounts = 0;
            if (CheckFormatMultisample(dxgiFormat, 1)) { sampleCounts |= (1 << 0); }
            if (CheckFormatMultisample(dxgiFormat, 2)) { sampleCounts |= (1 << 1); }
            if (CheckFormatMultisample(dxgiFormat, 4)) { sampleCounts |= (1 << 2); }
            if (CheckFormatMultisample(dxgiFormat, 8)) { sampleCounts |= (1 << 3); }
            if (CheckFormatMultisample(dxgiFormat, 16)) { sampleCounts |= (1 << 4); }
            if (CheckFormatMultisample(dxgiFormat, 32)) { sampleCounts |= (1 << 5); }

            properties = new PixelFormatProperties(
                MaxTextureDimension,
                type == TextureType.Texture1D ? 1 : MaxTextureDimension,
                type != TextureType.Texture3D ? 1 : MaxVolumeExtent,
                uint.MaxValue,
                type == TextureType.Texture3D ? 1 : MaxVolumeExtent,
                sampleCounts);
            return true;
        }

        protected override MappedResource MapCore(MappableResource resource, MapMode mode, uint subresource)
        {
            MappedResourceCacheKey key = new MappedResourceCacheKey(resource, subresource);
            lock (_mappedResourceLock)
            {
                if (_mappedResources.TryGetValue(key, out MappedResourceInfo info))
                {
                    if (info.Mode != mode)
                    {
                        throw new VeldridException("The given resource was already mapped with a different MapMode.");
                    }

                    info.RefCount += 1;
                    _mappedResources[key] = info;
                }
                else
                {
                    // No current mapping exists -- create one.

                    if (resource is D3D12Buffer buffer)
                    {
                        lock (_immediateContextLock)
                        {
                            MappedSubresource msr = _immediateContext.Map(
                                buffer.Buffer,
                                0,
                                D3D12Formats.VdToD3D11MapMode((buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic, mode),
                                Vortice.Direct3D12.MapFlags.None);

                            info.MappedResource = new MappedResource(resource, mode, msr.DataPointer, buffer.SizeInBytes);
                            info.RefCount = 1;
                            info.Mode = mode;
                            _mappedResources.Add(key, info);
                        }
                    }
                    else
                    {
                        D3D12Texture texture = Util.AssertSubtype<MappableResource, D3D12Texture>(resource);
                        lock (_immediateContextLock)
                        {
                            Util.GetMipLevelAndArrayLayer(texture, subresource, out uint mipLevel, out uint arrayLayer);
                            MappedSubresource msr = _immediateContext.Map(
                                texture.DeviceTexture,
                                (int)mipLevel,
                                (int)arrayLayer,
                                D3D12Formats.VdToD3D11MapMode(false, mode),
                                Vortice.Direct3D12.MapFlags.None,
                                out int mipSize);

                            info.MappedResource = new MappedResource(
                                resource,
                                mode,
                                msr.DataPointer,
                                texture.Height * (uint)msr.RowPitch,
                                subresource,
                                (uint)msr.RowPitch,
                                (uint)msr.DepthPitch);
                            info.RefCount = 1;
                            info.Mode = mode;
                            _mappedResources.Add(key, info);
                        }
                    }
                }

                return info.MappedResource;
            }
        }

        protected override void UnmapCore(MappableResource resource, uint subresource)
        {
            MappedResourceCacheKey key = new MappedResourceCacheKey(resource, subresource);
            bool commitUnmap;

            lock (_mappedResourceLock)
            {
                if (!_mappedResources.TryGetValue(key, out MappedResourceInfo info))
                {
                    throw new VeldridException($"The given resource ({resource}) is not mapped.");
                }

                info.RefCount -= 1;
                commitUnmap = info.RefCount == 0;
                if (commitUnmap)
                {
                    lock (_immediateContextLock)
                    {
                        if (resource is D3D12Buffer buffer)
                        {
                            _immediateContext.Unmap(buffer.Buffer, 0);
                        }
                        else
                        {
                            D3D12Texture texture = Util.AssertSubtype<MappableResource, D3D12Texture>(resource);
                            texture.DeviceTexture.Unmap((int)subresource);
                        }

                        bool result = _mappedResources.Remove(key);
                        Debug.Assert(result);
                    }
                }
                else
                {
                    _mappedResources[key] = info;
                }
            }
        }

        private protected unsafe override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            D3D12Buffer d3dBuffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(buffer);
            if (sizeInBytes == 0)
            {
                return;
            }

            bool isDynamic = (buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            bool isStaging = (buffer.Usage & BufferUsage.Staging) == BufferUsage.Staging;
            bool isUniformBuffer = (buffer.Usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer;
            bool updateFullBuffer = bufferOffsetInBytes == 0 && sizeInBytes == buffer.SizeInBytes;
            bool useUpdateSubresource = (!isDynamic && !isStaging) && (!isUniformBuffer || updateFullBuffer);
            bool useMap = (isDynamic && updateFullBuffer) || isStaging;

            if (useUpdateSubresource)
            {
                Box? subregion = new Box((int)bufferOffsetInBytes, 0, 0, (int)(sizeInBytes + bufferOffsetInBytes), 1, 1);

                if (isUniformBuffer)
                {
                    subregion = null;
                }

                lock (_immediateContextLock)
                {
                    _immediateContext.UpdateSubresource(d3dBuffer.Buffer, 0, subregion, source, 0, 0);
                }
            }
            else if (useMap)
            {
                MappedResource mr = MapCore(buffer, MapMode.Write, 0);
                if (sizeInBytes < 1024)
                {
                    Unsafe.CopyBlock((byte*)mr.Data + bufferOffsetInBytes, source.ToPointer(), sizeInBytes);
                }
                else
                {
                    Buffer.MemoryCopy(
                        source.ToPointer(),
                        (byte*)mr.Data + bufferOffsetInBytes,
                        buffer.SizeInBytes,
                        sizeInBytes);
                }
                UnmapCore(buffer, 0);
            }
            else
            {
                D3D12Buffer staging = GetFreeStagingBuffer(sizeInBytes);
                UpdateBuffer(staging, 0, source, sizeInBytes);
                Box sourceRegion = new Box(0, 0, 0, (int)sizeInBytes, 1, 1);
                lock (_immediateContextLock)
                {
                    _immediateContext.CopySubresourceRegion(
                        d3dBuffer.Buffer, 0, (int)bufferOffsetInBytes, 0, 0,
                        staging.Buffer, 0,
                        sourceRegion);
                }

                lock (_stagingResourcesLock)
                {
                    _availableStagingBuffers.Add(staging);
                }
            }
        }

        private D3D12Buffer GetFreeStagingBuffer(uint sizeInBytes)
        {
            lock (_stagingResourcesLock)
            {
                foreach (D3D12Buffer buffer in _availableStagingBuffers)
                {
                    if (buffer.SizeInBytes >= sizeInBytes)
                    {
                        _availableStagingBuffers.Remove(buffer);
                        return buffer;
                    }
                }
            }

            DeviceBuffer staging = ResourceFactory.CreateBuffer(
                new BufferDescription(sizeInBytes, BufferUsage.Staging));

            return Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(staging);
        }

        private protected unsafe override void UpdateTextureCore(
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
            D3D12Texture d3dTex = Util.AssertSubtype<Texture, D3D12Texture>(texture);
            bool useMap = (texture.Usage & TextureUsage.Staging) == TextureUsage.Staging;
            if (useMap)
            {
                uint subresource = texture.CalculateSubresource(mipLevel, arrayLayer);
                MappedResourceCacheKey key = new MappedResourceCacheKey(texture, subresource);
                MappedResource map = MapCore(texture, MapMode.Write, subresource);

                uint denseRowSize = FormatHelpers.GetRowPitch(width, texture.Format);
                uint denseSliceSize = FormatHelpers.GetDepthPitch(denseRowSize, height, texture.Format);

                Util.CopyTextureRegion(
                    source.ToPointer(),
                    0, 0, 0,
                    denseRowSize, denseSliceSize,
                    map.Data.ToPointer(),
                    x, y, z,
                    map.RowPitch, map.DepthPitch,
                    width, height, depth,
                    texture.Format);

                UnmapCore(texture, subresource);
            }
            else
            {
                int subresource = D3D12Util.ComputeSubresource(mipLevel, texture.MipLevels, arrayLayer);
                Box resourceRegion = new Box(
                    left: (int)x,
                    right: (int)(x + width),
                    top: (int)y,
                    front: (int)z,
                    bottom: (int)(y + height),
                    back: (int)(z + depth));

                uint srcRowPitch = FormatHelpers.GetRowPitch(width, texture.Format);
                uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, texture.Format);
                lock (_immediateContextLock)
                {
                    _immediateContext.UpdateSubresource(
                        d3dTex.DeviceTexture,
                        subresource,
                        resourceRegion,
                        source,
                        (int)srcRowPitch,
                        (int)srcDepthPitch);
                }
            }
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            return Util.AssertSubtype<Fence, D3D12Fence>(fence).Wait(nanosecondTimeout);
        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            int msTimeout;
            if (nanosecondTimeout == ulong.MaxValue)
            {
                msTimeout = -1;
            }
            else
            {
                msTimeout = (int)Math.Min(nanosecondTimeout / 1_000_000, int.MaxValue);
            }

            ManualResetEvent[] events = GetResetEventArray(fences.Length);
            for (int i = 0; i < fences.Length; i++)
            {
                events[i] = Util.AssertSubtype<Fence, D3D12Fence>(fences[i]).ResetEvent;
            }
            bool result;
            if (waitAll)
            {
                result = WaitHandle.WaitAll(events, msTimeout);
            }
            else
            {
                int index = WaitHandle.WaitAny(events, msTimeout);
                result = index != WaitHandle.WaitTimeout;
            }

            ReturnResetEventArray(events);

            return result;
        }

        private readonly object _resetEventsLock = new object();
        private readonly List<ManualResetEvent[]> _resetEvents = new List<ManualResetEvent[]>();

        private ManualResetEvent[] GetResetEventArray(int length)
        {
            lock (_resetEventsLock)
            {
                for (int i = _resetEvents.Count - 1; i > 0; i--)
                {
                    ManualResetEvent[] array = _resetEvents[i];
                    if (array.Length == length)
                    {
                        _resetEvents.RemoveAt(i);
                        return array;
                    }
                }
            }

            ManualResetEvent[] newArray = new ManualResetEvent[length];
            return newArray;
        }

        private void ReturnResetEventArray(ManualResetEvent[] array)
        {
            lock (_resetEventsLock)
            {
                _resetEvents.Add(array);
            }
        }

        public override void ResetFence(Fence fence)
        {
            Util.AssertSubtype<Fence, D3D12Fence>(fence).Reset();
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore() => 256u;

        internal override uint GetStructuredBufferMinOffsetAlignmentCore() => 16;

        protected override void PlatformDispose()
        {
            // Dispose staging buffers
            foreach (DeviceBuffer buffer in _availableStagingBuffers)
            {
                buffer.Dispose();
            }
            _availableStagingBuffers.Clear();

            _d3d11ResourceFactory.Dispose();
            _mainSwapchain?.Dispose();
            _immediateContext.Dispose();

            if (IsDebugEnabled)
            {
                uint refCount = _device.Release();
                if (refCount > 0)
                {
                    ID3D12Debug deviceDebug = _device.QueryInterfaceOrNull<ID3D12Debug>();
                    if (deviceDebug != null)
                    {
                        deviceDebug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Summary | ReportLiveDeviceObjectFlags.Detail | ReportLiveDeviceObjectFlags.IgnoreInternal);
                        deviceDebug.Dispose();
                    }
                }

                _dxgiAdapter.Dispose();

                // Report live objects using DXGI if available (DXGIGetDebugInterface1 will fail on pre Windows 8 OS).
                if (VorticeDXGI.DXGIGetDebugInterface1(out IDXGIDebug1 dxgiDebug).Success)
                {
                    dxgiDebug.ReportLiveObjects(VorticeDXGI.DebugAll, ReportLiveObjectFlags.Summary | ReportLiveObjectFlags.IgnoreInternal);
                    dxgiDebug.Dispose();
                }
            }
            else
            {
                _device.Dispose();
                _dxgiAdapter.Dispose();
            }
        }

        private protected override void WaitForIdleCore()
        {
        }

        public override bool GetD3D11Info(out BackendInfoD3D11 info)
        {
            info = _d3d11Info;
            return true;
        }

    }
}
