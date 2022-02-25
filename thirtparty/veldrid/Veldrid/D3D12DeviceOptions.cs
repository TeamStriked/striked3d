using System;

namespace Veldrid
{
    /// <summary>
    /// A structure describing Direct3D11-specific device creation options.
    /// </summary>
    public struct D3D12DeviceOptions
    {
        /// <summary>
        /// Native pointer to an adapter.
        /// </summary>
        public IntPtr AdapterPtr;

        /// <summary>
        /// Set of device specific flags.
        /// See <see cref="Vortice.Direct3D12.flags"/> for details.
        /// </summary>
        public uint DeviceCreationFlags;
    }
}
