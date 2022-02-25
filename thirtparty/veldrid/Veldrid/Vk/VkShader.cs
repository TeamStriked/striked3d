using static Veldrid.Vk.VulkanUtil;
using System;

namespace Veldrid.Vk
{
    internal unsafe class VkShader : Shader
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.ShaderModule _shaderModule;
        private bool _disposed;
        private string _name;

        public Silk.NET.Vulkan.ShaderModule ShaderModule => _shaderModule;

        public override bool IsDisposed => _disposed;

        public VkShader(VkGraphicsDevice gd, ref ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            _gd = gd;

            Silk.NET.Vulkan.ShaderModuleCreateInfo shaderModuleCI = new Silk.NET.Vulkan.ShaderModuleCreateInfo();
            shaderModuleCI.SType = Silk.NET.Vulkan.StructureType.ShaderModuleCreateInfo;

            fixed (byte* codePtr = description.ShaderBytes)
            {
                shaderModuleCI.CodeSize = (UIntPtr)description.ShaderBytes.Length;
                shaderModuleCI.PCode = (uint*)codePtr;
                var result = gd.vk.CreateShaderModule(gd.Device, &shaderModuleCI, null, out _shaderModule);
                CheckResult(result);
            }
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                _gd.SetResourceName(this, value);
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _gd.vk.DestroyShaderModule(_gd.Device, ShaderModule, null);
            }
        }
    }
}
