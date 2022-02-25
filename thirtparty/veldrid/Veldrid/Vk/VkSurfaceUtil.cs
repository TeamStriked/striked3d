using static Veldrid.Vk.VulkanUtil;
using Veldrid.Android;
using System;
using Veldrid.MetalBindings;
using Veldrid.Vk.Wayland;
using Veldrid.Vk.Xlib;

namespace Veldrid.Vk
{
    internal static unsafe class VkSurfaceUtil
    {
        internal static Silk.NET.Vulkan.SurfaceKHR CreateSurface(VkGraphicsDevice gd, Silk.NET.Vulkan.Instance instance, SwapchainSource swapchainSource)
        {
            var doCheck = gd != null;

            if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME))
                throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME}");

            switch (swapchainSource)
            {
                case XlibSwapchainSource xlibSource:
                    if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME))
                    {
                        throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME}");
                    }
                    return CreateXlib(gd, instance, xlibSource);
                case WaylandSwapchainSource waylandSource:
                    if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME))
                    {
                        throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME}");
                    }
                    return CreateWayland(gd, instance, waylandSource);
                case Win32SwapchainSource win32Source:
                    if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME))
                    {
                        throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME}");
                    }
                    return CreateWin32(gd, instance, win32Source);
                case AndroidSurfaceSwapchainSource androidSource:
                    if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME))
                    {
                        throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME}");
                    }
                    return CreateAndroidSurface(gd, instance, androidSource);
                case NSWindowSwapchainSource nsWindowSource:
                    if (doCheck)
                    {
                        bool hasMetalExtension = gd.HasSurfaceExtension(CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME);
                        if (hasMetalExtension || gd.HasSurfaceExtension(CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME))
                        {
                            return CreateNSWindowSurface(gd, instance, nsWindowSource, hasMetalExtension);
                        }
                        else
                        {
                            throw new VeldridException($"Neither macOS surface extension was available: " +
                                $"{CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME}, {CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME}");
                        }
                    }

                    return CreateNSWindowSurface(gd, instance, nsWindowSource, false);
                case UIViewSwapchainSource uiViewSource:
                    if (doCheck)
                    {
                        bool hasMetalExtension = gd.HasSurfaceExtension(CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME);
                        if (hasMetalExtension || gd.HasSurfaceExtension(CommonStrings.VK_MVK_IOS_SURFACE_EXTENSION_NAME))
                        {
                            return CreateUIViewSurface(gd, instance, uiViewSource, hasMetalExtension);
                        }
                        else
                        {
                            throw new VeldridException($"Neither macOS surface extension was available: " +
                                $"{CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME}, {CommonStrings.VK_MVK_IOS_SURFACE_EXTENSION_NAME}");
                        }
                    }

                    return CreateUIViewSurface(gd, instance, uiViewSource, false);
                default:
                    throw new VeldridException($"The provided SwapchainSource cannot be used to create a Vulkan surface.");
            }
        }

        private static Silk.NET.Vulkan.SurfaceKHR CreateWin32(VkGraphicsDevice gd,  Silk.NET.Vulkan.Instance instance, Win32SwapchainSource win32Source)
        {
            if (!gd.vk.TryGetInstanceExtension(gd.Instance, out Silk.NET.Vulkan.Extensions.KHR.KhrWin32Surface _winSurface))
            {
                throw new NotSupportedException("KHR_Win32 extension not found.");
            }

            Silk.NET.Vulkan.Win32SurfaceCreateInfoKHR surfaceCI = new Silk.NET.Vulkan.Win32SurfaceCreateInfoKHR();
            surfaceCI.SType = Silk.NET.Vulkan.StructureType.Win32SurfaceCreateInfoKhr;

            surfaceCI.Hwnd = win32Source.Hwnd;
            surfaceCI.Hinstance = win32Source.Hinstance;
            var result = _winSurface.CreateWin32Surface(instance, &surfaceCI, null, out Silk.NET.Vulkan.SurfaceKHR surface);
            CheckResult(result);
            return surface;
        }

        private static Silk.NET.Vulkan.SurfaceKHR CreateXlib(VkGraphicsDevice gd, Silk.NET.Vulkan.Instance instance, XlibSwapchainSource xlibSource)
        {
            if (!gd.vk.TryGetInstanceExtension(gd.Instance, out Silk.NET.Vulkan.Extensions.KHR.KhrXlibSurface _xlibSurface))
            {
                throw new NotSupportedException("KVR_XLIB_surface extension not found.");
            }

            Silk.NET.Vulkan.XlibSurfaceCreateInfoKHR xsci = new Silk.NET.Vulkan.XlibSurfaceCreateInfoKHR();
            xsci.SType = Silk.NET.Vulkan.StructureType.XlibSurfaceCreateInfoKhr;

            xsci.Dpy = (nint*)xlibSource.Display;
            xsci.Window = xlibSource.Window ;
            var result = _xlibSurface.CreateXlibSurface(instance, &xsci, null, out Silk.NET.Vulkan.SurfaceKHR surface);
            CheckResult(result);
            return surface;
        }

        private static Silk.NET.Vulkan.SurfaceKHR CreateWayland(VkGraphicsDevice gd, Silk.NET.Vulkan.Instance instance, WaylandSwapchainSource waylandSource)
        {
            if (!gd.vk.TryGetInstanceExtension(gd.Instance, out Silk.NET.Vulkan.Extensions.KHR.KhrWaylandSurface _waylandSurface))
            {
                throw new NotSupportedException("KHR_WAYLAND_SURFACE extension not found.");
            }

            Silk.NET.Vulkan.WaylandSurfaceCreateInfoKHR wsci = new Silk.NET.Vulkan.WaylandSurfaceCreateInfoKHR();
            wsci.SType = Silk.NET.Vulkan.StructureType.WaylandSurfaceCreateInfoKhr;

            wsci.Display = (nint*)waylandSource.Display;
            wsci.Surface = (nint*)waylandSource.Surface;
            var result = _waylandSurface.CreateWaylandSurface(instance, &wsci, null, out Silk.NET.Vulkan.SurfaceKHR surface);
            CheckResult(result);
            return surface;
        }

        private static Silk.NET.Vulkan.SurfaceKHR CreateAndroidSurface(VkGraphicsDevice gd, Silk.NET.Vulkan.Instance instance, AndroidSurfaceSwapchainSource androidSource)
        {

            if (!gd.vk.TryGetInstanceExtension(gd.Instance, out Silk.NET.Vulkan.Extensions.KHR.KhrAndroidSurface _androidSurface))
            {
                throw new NotSupportedException("KHR_ANDROID_SURFACE extension not found.");
            }

            IntPtr aNativeWindow = AndroidRuntime.ANativeWindow_fromSurface(androidSource.JniEnv, androidSource.Surface);
            Silk.NET.Vulkan.AndroidSurfaceCreateInfoKHR androidSurfaceCI = new Silk.NET.Vulkan.AndroidSurfaceCreateInfoKHR();
            androidSurfaceCI.SType = Silk.NET.Vulkan.StructureType.AndroidSurfaceCreateInfoKhr;

            androidSurfaceCI.Window = (nint*)aNativeWindow;
            var result = _androidSurface.CreateAndroidSurface(instance, &androidSurfaceCI, null, out Silk.NET.Vulkan.SurfaceKHR surface);
            CheckResult(result);
            return surface;
        }

        private static unsafe Silk.NET.Vulkan.SurfaceKHR CreateNSWindowSurface(VkGraphicsDevice gd, Silk.NET.Vulkan.Instance instance, NSWindowSwapchainSource nsWindowSource, bool hasExtMetalSurface)
        {

            if (!gd.vk.TryGetInstanceExtension(gd.Instance, out Silk.NET.Vulkan.Extensions.MVK.MvkMacosSurface _mvkSurface))
            {
                throw new NotSupportedException("MVK_MACOS_surface extension not found.");
            }

            CAMetalLayer metalLayer = CAMetalLayer.New();
            NSWindow nswindow = new NSWindow(nsWindowSource.NSWindow);
            NSView contentView = nswindow.contentView;
            contentView.wantsLayer = true;
            contentView.layer = metalLayer.NativePtr;

            if (hasExtMetalSurface)
            {
                VkMetalSurfaceCreateInfoEXT surfaceCI = new VkMetalSurfaceCreateInfoEXT();
                surfaceCI.sType = VkMetalSurfaceCreateInfoEXT.VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT;
                surfaceCI.pLayer = metalLayer.NativePtr.ToPointer();
                Silk.NET.Vulkan.SurfaceKHR surface;
                var result = gd.CreateMetalSurfaceEXT(instance, &surfaceCI, null, &surface);
                CheckResult(result);
                return surface;
            }
            else
            {
                Silk.NET.Vulkan.MacOSSurfaceCreateInfoMVK surfaceCI = new Silk.NET.Vulkan.MacOSSurfaceCreateInfoMVK();
                surfaceCI.SType = Silk.NET.Vulkan.StructureType.MacosSurfaceCreateInfoMvk;

                surfaceCI.PView = contentView.NativePtr.ToPointer();
                var result = _mvkSurface.CreateMacOssurface(instance, &surfaceCI, null, out Silk.NET.Vulkan.SurfaceKHR surface);
                CheckResult(result);
                return surface;
            }
        }

        private static Silk.NET.Vulkan.SurfaceKHR CreateUIViewSurface(VkGraphicsDevice gd, Silk.NET.Vulkan.Instance instance, UIViewSwapchainSource uiViewSource, bool hasExtMetalSurface)
        {
            if (!gd.vk.TryGetInstanceExtension(gd.Instance, out Silk.NET.Vulkan.Extensions.MVK.MvkIosSurface _mvkSurface))
            {
                throw new NotSupportedException("MVK_IOS_surface extension not found.");
            }

            CAMetalLayer metalLayer = CAMetalLayer.New();
            UIView uiView = new UIView(uiViewSource.UIView);
            metalLayer.frame = uiView.frame;
            metalLayer.opaque = true;
            uiView.layer.addSublayer(metalLayer.NativePtr);

            if (hasExtMetalSurface)
            {
                VkMetalSurfaceCreateInfoEXT surfaceCI = new VkMetalSurfaceCreateInfoEXT();
                surfaceCI.sType = VkMetalSurfaceCreateInfoEXT.VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT;
                surfaceCI.pLayer = metalLayer.NativePtr.ToPointer();
                Silk.NET.Vulkan.SurfaceKHR surface;
                var result = gd.CreateMetalSurfaceEXT(instance, &surfaceCI, null, &surface);
                CheckResult(result);
                return surface;
            }
            else
            {
                Silk.NET.Vulkan.IOSSurfaceCreateInfoMVK surfaceCI = new Silk.NET.Vulkan.IOSSurfaceCreateInfoMVK();
                surfaceCI.SType = Silk.NET.Vulkan.StructureType.IosSurfaceCreateInfoMvk;

                surfaceCI.PView = uiView.NativePtr.ToPointer();
                var result = _mvkSurface.CreateIossurface(instance, &surfaceCI, null, out Silk.NET.Vulkan.SurfaceKHR surface);
                return surface;
            }
        }
    }
}
