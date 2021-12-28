using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Striked3D.Core;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class Debugger
    {
        protected RenderingInstance _instance;
        private ExtDebugUtils _debugUtils;
        private DebugUtilsMessengerEXT _debugMessenger;

        public Debugger(RenderingInstance _instance)
        {
            this._instance = _instance;
            Logger.Debug(this, "Create vulkan debugger");

            if (!_instance.ValidationEnabled) return;
            if (!_instance.Api.TryGetInstanceExtension(_instance.NativeHandle, out _debugUtils)) return;
        }

        public unsafe void Destroy()
        {
            if (_instance.ValidationEnabled)
            {
                _debugUtils.DestroyDebugUtilsMessenger(_instance.NativeHandle, _debugMessenger, null);
            }
        }

        public unsafe void Instanciate()
        {
            var createInfo = new DebugUtilsMessengerCreateInfoEXT();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            fixed (DebugUtilsMessengerEXT* debugMessenger = &_debugMessenger)
            {
                if (_debugUtils.CreateDebugUtilsMessenger
                        (this._instance.NativeHandle, &createInfo, null, debugMessenger) != Result.Success)
                {
                    throw new Exception("Failed to create debug messenger.");
                }
            }
        }

        private unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
            createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt;
            createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt;
            createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
        }


        private unsafe uint DebugCallback
       (
           DebugUtilsMessageSeverityFlagsEXT messageSeverity,
           DebugUtilsMessageTypeFlagsEXT messageTypes,
           DebugUtilsMessengerCallbackDataEXT* pCallbackData,
           void* pUserData
       )
        {
            if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt)
            {
                Logger.Debug(this,
                    ($"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage)));

            }

            return Vk.False;
        }
    }
}
