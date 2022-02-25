
namespace Veldrid.Vk
{
    internal unsafe class VkFence : Fence
    {
        private readonly VkGraphicsDevice _gd;
        private Silk.NET.Vulkan.Fence _fence;
        private string _name;
        private bool _destroyed;

        public Silk.NET.Vulkan.Fence DeviceFence => _fence;

        public VkFence(VkGraphicsDevice gd, bool signaled)
        {
            _gd = gd;
            Silk.NET.Vulkan.FenceCreateInfo fenceCI =  new Silk.NET.Vulkan.FenceCreateInfo();
            fenceCI.SType = Silk.NET.Vulkan.StructureType.FenceCreateInfo;

            fenceCI.Flags = signaled ? Silk.NET.Vulkan.FenceCreateFlags.FenceCreateSignaledBit : 0;
            var result = _gd.vk.CreateFence(_gd.Device, &fenceCI, null, out _fence);
            VulkanUtil.CheckResult(result);
        }

        public override void Reset()
        {
            _gd.ResetFence(this);
        }

        public override bool Signaled => _gd.vk.GetFenceStatus(_gd.Device, _fence) == Silk.NET.Vulkan.Result.Success;
        public override bool IsDisposed => _destroyed;

        public override string Name
        {
            get => _name;
            set
            {
                _name = value; _gd.SetResourceName(this, value);
            }
        }

        public override void Dispose()
        {
            if (!_destroyed)
            {
                _gd.vk.DestroyFence(_gd.Device, _fence, null);
                _destroyed = true;
            }
        }
    }
}
