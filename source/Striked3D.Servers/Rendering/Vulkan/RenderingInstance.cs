using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Vulkan.Extensions.EXT;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Striked3D.Core;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class RenderingInstance
    {
        private string[] _instanceExtensions = { ExtDebugUtils.ExtensionName };
        protected Vk _vk;
        protected bool enableValidationLayers  = true;
        private string[] _validationLayers;
        private Instance _instance;
        private KhrSurface _vkSurface;
        protected SurfaceKHR _surface;

        public Silk.NET.Windowing.IWindow WindowHandle
        {
            get { return _window; }
        }

        public Instance NativeHandle
        {
            get { return _instance; }
        }
        public KhrSurface NativeHandleKhrSurface
        {
            get { return _vkSurface; }
        }

        public SurfaceKHR NativeHandleSurface
        {
            get { return _surface; }
        }

        public bool ValidationEnabled
        {
            get
            {
                return this.enableValidationLayers;
            }
        }
        public string[] Validationlayer
        {
            get
            {
                return this._validationLayers;
            }
        }

        public Vk Api
        {
           get {
                return this._vk;
            }   
        }


        private string[][] _validationLayerNamesPriorityList =
       {
            new [] { "VK_LAYER_KHRONOS_validation" },
            new [] { "VK_LAYER_LUNARG_standard_validation" },
            new []
            {
                "VK_LAYER_GOOGLE_threading",
                "VK_LAYER_LUNARG_parameter_validation",
                "VK_LAYER_LUNARG_object_tracker",
                "VK_LAYER_LUNARG_core_validation",
                "VK_LAYER_GOOGLE_unique_objects",
            }
        };

        private Silk.NET.Windowing.IWindow _window;


        public RenderingInstance(Silk.NET.Windowing.IWindow _window)
        {
            this._window = _window;

            Logger.Debug(this, "Create vulkan instance");
            _vk = Vk.GetApi();

            if (enableValidationLayers)
            {
                _validationLayers = GetOptimalValidationLayers();
                if (_validationLayers is null)
                {
                    throw new NotSupportedException("Validation layers requested, but not available!");
                }
            }
        }

        public unsafe void Destroy()
        {
            _vkSurface.DestroySurface(_instance, _surface, null);
            _vk.DestroyInstance(_instance, null);
        }
        public unsafe void CreateSurface()
        {
            Logger.Debug(this, "Create surface");
            _surface = _window.VkSurface!.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();
        }

        public unsafe void Instanciate()
        {
            var appInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
                EngineVersion = new Version32(1, 0, 0),
                ApiVersion = Vk.Version11
            };

            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            var extensions = _window.VkSurface!.GetRequiredExtensions(out var extCount);
            var newExtensions = stackalloc byte*[(int)(extCount + _instanceExtensions.Length)];
            for (var i = 0; i < extCount; i++)
            {
                newExtensions[i] = extensions[i];
            }

            for (var i = 0; i < _instanceExtensions.Length; i++)
            {
                newExtensions[extCount + i] = (byte*)SilkMarshal.StringToPtr(_instanceExtensions[i]);
            }

            extCount += (uint)_instanceExtensions.Length;
            createInfo.EnabledExtensionCount = extCount;
            createInfo.PpEnabledExtensionNames = newExtensions;

            if (enableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            fixed (Instance* instance = &_instance)
            {
                if (_vk.CreateInstance(&createInfo, null, instance) != Result.Success)
                {
                    throw new Exception("Failed to create instance!");
                }
            }

            _vk.CurrentInstance = _instance;

            if (!_vk.TryGetInstanceExtension(_instance, out _vkSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

            Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
            Marshal.FreeHGlobal((nint)appInfo.PEngineName);

            if (enableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }
        }

        private unsafe string[]? GetOptimalValidationLayers()
        {
            var layerCount = 0u;
            _vk.EnumerateInstanceLayerProperties(&layerCount, (LayerProperties*)0);

            var availableLayers = new LayerProperties[layerCount];
            fixed (LayerProperties* availableLayersPtr = availableLayers)
            {
                _vk.EnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
            }

            var availableLayerNames = availableLayers.Select(availableLayer => Marshal.PtrToStringAnsi((nint)availableLayer.LayerName)).ToArray();
            foreach (var validationLayerNameSet in _validationLayerNamesPriorityList)
            {
                if (validationLayerNameSet.All(validationLayerName => availableLayerNames.Contains(validationLayerName)))
                {
                    return validationLayerNameSet;
                }
            }

            return null;
        }

    }
}
