using Vortice;
using Vortice.Direct3D12;
using Vortice.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpGen.Runtime;

namespace Veldrid.D3D12
{
    internal class D3D12Swapchain : Swapchain
    {
        private readonly D3D12GraphicsDevice _gd;
        private readonly PixelFormat? _depthFormat;
        private readonly IDXGISwapChain _dxgiSwapChain;
        private bool _vsync;
        private int _syncInterval;
        private D3D12Framebuffer _framebuffer;
        private D3D12Texture _depthTexture;
        private float _pixelScale = 1f;
        private bool _disposed;

        private readonly object _referencedCLsLock = new object();
        private HashSet<D3D12CommandList> _referencedCLs = new HashSet<D3D12CommandList>();

        public override Framebuffer Framebuffer => _framebuffer;

        public override string Name
        {
            get
            {
                unsafe
                {
                    byte* pname = stackalloc byte[1024];
                    int size = 1024 - 1;
                    _dxgiSwapChain.GetPrivateData(CommonGuid.DebugObjectName, ref size, new IntPtr(pname));
                    pname[size] = 0;
                    return Marshal.PtrToStringAnsi(new IntPtr(pname));
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _dxgiSwapChain.SetPrivateData(CommonGuid.DebugObjectName, 0, IntPtr.Zero);
                }
                else
                {
                    var namePtr = Marshal.StringToHGlobalAnsi(value);
                    _dxgiSwapChain.SetPrivateData(CommonGuid.DebugObjectName, value.Length, namePtr);
                    Marshal.FreeHGlobal(namePtr);
                }
            }
        }

        public override bool SyncToVerticalBlank
        {
            get => _vsync; set
            {
                _vsync = value;
                _syncInterval = D3D12Util.GetSyncInterval(value);
            }
        }

        private readonly Format _colorFormat;

        public IDXGISwapChain DxgiSwapChain => _dxgiSwapChain;

        public int SyncInterval => _syncInterval;

        public D3D12Swapchain(D3D12GraphicsDevice gd, ref SwapchainDescription description)
        {
            _gd = gd;
            _depthFormat = description.DepthFormat;
            SyncToVerticalBlank = description.SyncToVerticalBlank;

            _colorFormat = description.ColorSrgb
                ? Format.B8G8R8A8_UNorm_SRgb
                : Format.B8G8R8A8_UNorm;

            if (description.Source is Win32SwapchainSource win32Source)
            {
                SwapChainDescription dxgiSCDesc = new SwapChainDescription
                {
                    BufferCount = 2,
                    Windowed = true,
                    BufferDescription = new ModeDescription(
                        (int)description.Width, (int)description.Height, _colorFormat),
                    OutputWindow = win32Source.Hwnd,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    BufferUsage = Usage.RenderTargetOutput
                };

                using (IDXGIFactory dxgiFactory = _gd.Adapter.GetParent<IDXGIFactory>())
                {
                    _dxgiSwapChain = dxgiFactory.CreateSwapChain(_gd.Device, dxgiSCDesc);
                    dxgiFactory.MakeWindowAssociation(win32Source.Hwnd, WindowAssociationFlags.IgnoreAltEnter);
                }
            }
            else if (description.Source is UwpSwapchainSource uwpSource)
            {
                _pixelScale = uwpSource.LogicalDpi / 96.0f;

                // Properties of the swap chain
                SwapChainDescription1 swapChainDescription = new SwapChainDescription1()
                {
                    AlphaMode = AlphaMode.Ignore,
                    BufferCount = 2,
                    Format = _colorFormat,
                    Height = (int)(description.Height * _pixelScale),
                    Width = (int)(description.Width * _pixelScale),
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.FlipSequential,
                    BufferUsage = Usage.RenderTargetOutput,
                };

                // Get the Vortice.DXGI factory automatically created when initializing the Direct3D device.
                using (IDXGIFactory2 dxgiFactory = _gd.Adapter.GetParent<IDXGIFactory2>())
                {
                    // Create the swap chain and get the highest version available.
                    using (IDXGISwapChain1 swapChain1 = dxgiFactory.CreateSwapChainForComposition(_gd.Device, swapChainDescription))
                    {
                        _dxgiSwapChain = swapChain1.QueryInterface<IDXGISwapChain2>();
                    }
                }

                ComObject co = new ComObject(uwpSource.SwapChainPanelNative);

                ISwapChainPanelNative swapchainPanelNative = co.QueryInterfaceOrNull<ISwapChainPanelNative>();
                if (swapchainPanelNative != null)
                {
                    swapchainPanelNative.SetSwapChain(_dxgiSwapChain);
                }
                else
                {
                    ISwapChainBackgroundPanelNative bgPanelNative = co.QueryInterfaceOrNull<ISwapChainBackgroundPanelNative>();
                    if (bgPanelNative != null)
                    {
                        bgPanelNative.SetSwapChain(_dxgiSwapChain);
                    }
                }
            }

            Resize(description.Width, description.Height);
        }

        public override void Resize(uint width, uint height)
        {
            lock (_referencedCLsLock)
            {
                foreach (D3D12CommandList cl in _referencedCLs)
                {
                    cl.Reset();
                }

                _referencedCLs.Clear();
            }

            bool resizeBuffers = false;

            if (_framebuffer != null)
            {
                resizeBuffers = true;
                if (_depthTexture != null)
                {
                    _depthTexture.Dispose();
                }

                _framebuffer.Dispose();
            }

            uint actualWidth = (uint)(width * _pixelScale);
            uint actualHeight = (uint)(height * _pixelScale);
            if (resizeBuffers)
            {
                _dxgiSwapChain.ResizeBuffers(2, (int)actualWidth, (int)actualHeight, _colorFormat, SwapChainFlags.None).CheckError();
            }

            // Get the backbuffer from the swapchain
            using (ID3D12Texture2D backBufferTexture = _dxgiSwapChain.GetBuffer<ID3D12Texture2D>(0))
            {
                if (_depthFormat != null)
                {
                    TextureDescription depthDesc = new TextureDescription(
                        actualWidth, actualHeight, 1, 1, 1,
                        _depthFormat.Value,
                        TextureUsage.DepthStencil,
                        TextureType.Texture2D);
                    _depthTexture = new D3D12Texture(_gd.Device, ref depthDesc);
                }

                D3D12Texture backBufferVdTexture = new D3D12Texture(
                    backBufferTexture,
                    TextureType.Texture2D,
                    D3D12Formats.ToVdFormat(_colorFormat));

                FramebufferDescription desc = new FramebufferDescription(_depthTexture, backBufferVdTexture);
                _framebuffer = new D3D12Framebuffer(_gd.Device, ref desc)
                {
                    Swapchain = this
                };
            }
        }

        public void AddCommandListReference(D3D12CommandList cl)
        {
            lock (_referencedCLsLock)
            {
                _referencedCLs.Add(cl);
            }
        }

        public void RemoveCommandListReference(D3D12CommandList cl)
        {
            lock (_referencedCLsLock)
            {
                _referencedCLs.Remove(cl);
            }
        }

        public override bool IsDisposed => _disposed;

        public override void Dispose()
        {
            if (!_disposed)
            {
                _depthTexture?.Dispose();
                _framebuffer.Dispose();
                _dxgiSwapChain.Dispose();

                _disposed = true;
            }
        }
    }
}
