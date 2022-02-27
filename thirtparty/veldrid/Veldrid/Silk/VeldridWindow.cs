// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Contexts;
using Veldrid;
using Veldrid.Vk;
using Veldrid.Vk.Xlib;

namespace Silk.NET.Windowing.Extensions.Veldrid
{
    /// <summary>
    /// Contains classes for creating
    /// </summary>
    public static class VeldridWindow
    {
        /// <summary>
        /// Creates a window and a graphics device with the given options.
        /// </summary>
        /// <param name="windowCI">The window options, used to create a window.</param>
        /// <param name="window">The new window.</param>
        /// <param name="gd">The new graphics device.</param>
        public static void CreateWindowAndGraphicsDevice
        (
            WindowOptions windowCI,
            out IWindow window,
            out GraphicsDevice gd
        )
            => CreateWindowAndGraphicsDevice
            (
                windowCI,
                new GraphicsDeviceOptions(),
                GetPlatformDefaultBackend(),
                out window,
                out gd
            );

        /// <summary>
        /// Creates a window and a graphics device with the given options.
        /// </summary>
        /// <param name="windowCI">The window options, used to create the window.</param>
        /// <param name="deviceOptions">The device options, used to create the graphics device.</param>
        /// <param name="window">The new window.</param>
        /// <param name="gd">The new graphics device.</param>
        public static void CreateWindowAndGraphicsDevice
        (
            WindowOptions windowCI,
            GraphicsDeviceOptions deviceOptions,
            out IWindow window,
            out GraphicsDevice gd
        ) => CreateWindowAndGraphicsDevice(windowCI, deviceOptions, GetPlatformDefaultBackend(), out window, out gd);

        /// <summary>
        /// Creates a window and a graphics device with the given options.
        /// </summary>
        /// <param name="windowCI">The window options, used to create the window.</param>
        /// <param name="deviceOptions">The device options, used to create the graphics device.</param>
        /// <param name="preferredBackend">The preferred graphics backend for the graphics device.</param>
        /// <param name="window">The new window.</param>
        /// <param name="gd">The new graphics device.</param>
        public static void CreateWindowAndGraphicsDevice
        (
            WindowOptions windowCI,
            GraphicsDeviceOptions deviceOptions,
            GraphicsBackend preferredBackend,
            out IWindow window,
            out GraphicsDevice gd
        )
        {
            var opts = new WindowOptions
            (
                windowCI.IsVisible,
                windowCI.Position,
                windowCI.Size,
                -1,
                -1,
                new GraphicsAPI
                (
                    ContextAPI.None,
                    ContextProfile.Core,
                    ContextFlags.ForwardCompatible,
                    new APIVersion(1, 0)
                ),
                windowCI.Title,
                WindowState.Normal,
                WindowBorder.Resizable,
                deviceOptions.SyncToVerticalBlank,
                false,
                VideoMode.Default,
                null
            );



            window = Window.Create(opts);
            window.Initialize();
            window.WindowState = windowCI.WindowState;
            gd = CreateGraphicsDevice(window, deviceOptions, preferredBackend);
        }

        /// <summary>
        /// Creates a graphics device from an existing window.
        /// </summary>
        /// <param name="window">The window to create a graphics device from.</param>
        /// <returns>The new graphics device.</returns>
        public static GraphicsDevice CreateGraphicsDevice(this IView window)
            => CreateGraphicsDevice(window, new GraphicsDeviceOptions(), GetPlatformDefaultBackend());

        /// <summary>
        /// Creates a graphics device from an existing window.
        /// </summary>
        /// <param name="window">The window to create a graphics device from.</param>
        /// <param name="options">The graphics device options.</param>
        /// <returns>The new graphics device.</returns>
        public static GraphicsDevice CreateGraphicsDevice(this IView window, GraphicsDeviceOptions options)
            => CreateGraphicsDevice(window, options, GetPlatformDefaultBackend());

        /// <summary>
        /// Creates a graphics device from an existing window.
        /// </summary>
        /// <param name="window">The window to create a graphics device from.</param>
        /// <param name="preferredBackend">The preferred graphics backend for the graphics device.</param>
        /// <returns>The new graphics device.</returns>
        public static GraphicsDevice CreateGraphicsDevice(this IView window, GraphicsBackend preferredBackend)
            => CreateGraphicsDevice(window, new GraphicsDeviceOptions(), preferredBackend);

        /// <summary>
        /// Creates a graphics device from an existing window.
        /// </summary>
        /// <param name="window">The window to create a graphics device from.</param>
        /// <param name="options">The graphics device options.</param>
        /// <param name="preferredBackend">The preferred graphics backend for the graphics device.</param>
        /// <returns>The new graphics device.</returns>
        public static GraphicsDevice CreateGraphicsDevice
        (
            this IView window,
            GraphicsDeviceOptions options,
            GraphicsBackend preferredBackend
        ) =>
            preferredBackend switch
            {
                GraphicsBackend.Direct3D11 => CreateDefaultD3D11GraphicsDevice(options, window),
                GraphicsBackend.Vulkan => CreateVulkanGraphicsDevice(options, window),
                GraphicsBackend.Metal => CreateMetalGraphicsDevice(options, window),
                _ => throw new VeldridException("Invalid GraphicsBackend: " + preferredBackend)
            };

        private static unsafe SwapchainSource GetSwapchainSource(INativeWindow view)
        {
            if (view.WinRT.HasValue)
            {
                ThrowWinRT();

                static void ThrowWinRT() => throw new NotSupportedException
                    ("Silk.NET only supports CoreWindow UWP views, which Veldrid does not support today");
            }

            if (view.Win32.HasValue)
            {
                return SwapchainSource.CreateWin32(view.Win32.Value.Hwnd, view.Win32.Value.HInstance);
            }

            if (view.X11.HasValue)
            {
                return SwapchainSource.CreateXlib(view.X11.Value.Display, (nint) view.X11.Value.Window);
            }

            if (view.Wayland.HasValue)
            {
                return SwapchainSource.CreateWayland(view.Wayland.Value.Display, view.Wayland.Value.Surface);
            }

            if (view.Android.HasValue)
            {
                return SwapchainSource.CreateAndroidSurface
                    (view.Android.Value.Surface, AndroidSupport.JNIEnv ?? ThrowJNIEnv());

                static nint ThrowJNIEnv()
                {
                    throw new InvalidOperationException
                    (
                        "Android applications must set the AndroidSupport.JNIEnv property" +
                        " (sourced from Android.Runtime.JNIEnv.Handle property in " +
                        "Mono.Android) to create swapchains from Android views."
                    );

                    return default;
                }
            }

            if (view.Cocoa.HasValue)
            {
                return SwapchainSource.CreateNSWindow(view.Cocoa.Value);
            }

            if (view.UIKit.HasValue)
            {
                return SwapchainSource.CreateUIView(view.UIKit.Value.Window);
            }

            Throw();
            return null!;

            static void Throw() => throw new PlatformNotSupportedException();
        }

        private static unsafe VkSurfaceSource GetSurfaceSource(INativeWindow window)
        {
            if (window.X11.HasValue)
            {
                return VkSurfaceSource.CreateXlib
                    ((Display*) window.X11.Value.Display, new() {Value = (nint) window.X11.Value.Window});
            }

            if (window.Win32.HasValue)
            {
                return VkSurfaceSource.CreateWin32(window.Win32.Value.HInstance, window.Win32.Value.Hwnd);
            }

            Throw();
            return null!;

            static void Throw() => throw new PlatformNotSupportedException();
        }

        public static unsafe GraphicsDevice CreateVulkanGraphicsDevice(GraphicsDeviceOptions options, IView window)
            => CreateVulkanGraphicsDevice(options, window, false);

        public static unsafe GraphicsDevice CreateVulkanGraphicsDevice
        (
            GraphicsDeviceOptions options,
            IView window,
            bool colorSrgb
        )
        {
            var scDesc = new SwapchainDescription
            (
                GetSwapchainSource(window.Native),
                (uint) window.Size.X,
                (uint) window.Size.Y,
                options.SwapchainDepthFormat,
                options.SyncToVerticalBlank,
                colorSrgb
            );
            var gd = GraphicsDevice.CreateVulkan(options, scDesc);

            return gd;
        }

        private static unsafe GraphicsDevice CreateMetalGraphicsDevice(GraphicsDeviceOptions options, IView window)
            => CreateMetalGraphicsDevice(options, window, false);

        private static unsafe GraphicsDevice CreateMetalGraphicsDevice
        (
            GraphicsDeviceOptions options,
            IView window,
            bool colorSrgb
        )
        {
            var source = GetSwapchainSource(window.Native);
            var swapchainDesc = new SwapchainDescription
            (
                source,
                (uint) window.Size.X, (uint) window.Size.Y,
                options.SwapchainDepthFormat,
                options.SyncToVerticalBlank,
                colorSrgb
            );

            return GraphicsDevice.CreateMetal(options, swapchainDesc);
        }

        /// <summary>
        /// Gets the default backend given the current runtime information.
        /// </summary>
        /// <returns>The default backend for this runtime/platform.</returns>
        public static GraphicsBackend GetPlatformDefaultBackend()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GraphicsBackend.Direct3D11;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal)
                    ? GraphicsBackend.Metal
                    : GraphicsBackend.Direct3D12;
            }

            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                ? GraphicsBackend.Vulkan
                : GraphicsBackend.Direct3D12;
        }
        
        private const int GlesMajor = 3;
        private const int GlesMinor = 1;
        private const int GlMajor = 4;
        private const int GlMinor = 5;

        public static GraphicsAPI ToGraphicsAPI(this GraphicsBackend backend) => backend switch
        {
            GraphicsBackend.Direct3D11 => GraphicsAPI.None, GraphicsBackend.Vulkan => GraphicsAPI.DefaultVulkan,
            GraphicsBackend.Metal => GraphicsAPI.None,
            _ => throw new ArgumentOutOfRangeException()
        };


        private static GraphicsDevice CreateDefaultD3D11GraphicsDevice
        (
            GraphicsDeviceOptions options,
            IView view
        )
        {
            var source = GetSwapchainSource(view.Native);
            var swapchainDesc = new SwapchainDescription
            (
                source,
                (uint) view.Size.X, (uint) view.Size.Y,
                options.SwapchainDepthFormat,
                options.SyncToVerticalBlank,
                options.SwapchainSrgbFormat
            );

            return GraphicsDevice.CreateD3D11(options, swapchainDesc);
        }

        private static unsafe string GetString(byte* stringStart)
        {
            var characters = 0;
            while (stringStart[characters] != 0)
            {
                characters++;
            }

            return Encoding.UTF8.GetString(stringStart, characters);
        }

        private static (int Major, int Minor) GetMaxGLVersion
            (bool gles) => gles ? (GlesMajor, GlesMinor) : (GlMajor, GlMinor);
    }
}
