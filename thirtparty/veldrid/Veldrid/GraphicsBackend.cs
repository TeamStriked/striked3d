﻿namespace Veldrid
{
    /// <summary>
    /// The specific graphics API used by the <see cref="GraphicsDevice"/>.
    /// </summary>
    public enum GraphicsBackend : byte
    {
        /// <summary>
        /// Direct3D 11.
        /// </summary>
        Direct3D11,
        /// <summary>
        /// Direct3D 11.
        /// </summary>
        Direct3D12,
        /// <summary>
        /// Vulkan.
        /// </summary>
        Vulkan,
        /// <summary>
        /// Metal.
        /// </summary>
        Metal,
    }
}
