using System;
using System.Collections.Generic;
using System.Text;

namespace Veldrid.Vk
{
    internal static partial class VkFormats
    {
        internal static Silk.NET.Vulkan.Format VdToVkPixelFormat(PixelFormat format, bool toDepthFormat = false)
        {
            switch (format)
            {
                case PixelFormat.R8_UNorm:
                    return Silk.NET.Vulkan.Format.R8Unorm;
                case PixelFormat.R8_SNorm:
                    return Silk.NET.Vulkan.Format.R8SNorm;
                case PixelFormat.R8_UInt:
                    return Silk.NET.Vulkan.Format.R8Uint;
                case PixelFormat.R8_SInt:
                    return Silk.NET.Vulkan.Format.R8Sint;

                case PixelFormat.R16_UNorm:
                    return toDepthFormat ? Silk.NET.Vulkan.Format.D16Unorm : Silk.NET.Vulkan.Format.R16Unorm;
                case PixelFormat.R16_SNorm:
                    return Silk.NET.Vulkan.Format.R16SNorm;
                case PixelFormat.R16_UInt:
                    return Silk.NET.Vulkan.Format.R16Uint;
                case PixelFormat.R16_SInt:
                    return Silk.NET.Vulkan.Format.R16Sint;
                case PixelFormat.R16_Float:
                    return Silk.NET.Vulkan.Format.R16Sfloat;

                case PixelFormat.R32_UInt:
                    return Silk.NET.Vulkan.Format.R32Uint;
                case PixelFormat.R32_SInt:
                    return Silk.NET.Vulkan.Format.R32Sint;
                case PixelFormat.R32_Float:
                    return toDepthFormat ? Silk.NET.Vulkan.Format.D32Sfloat : Silk.NET.Vulkan.Format.R32Sfloat;

                case PixelFormat.R8_G8_UNorm:
                    return Silk.NET.Vulkan.Format.R8G8Unorm;
                case PixelFormat.R8_G8_SNorm:
                    return Silk.NET.Vulkan.Format.R8G8SNorm;
                case PixelFormat.R8_G8_UInt:
                    return Silk.NET.Vulkan.Format.R8G8Uint;
                case PixelFormat.R8_G8_SInt:
                    return Silk.NET.Vulkan.Format.R8G8Sint;

                case PixelFormat.R16_G16_UNorm:
                    return Silk.NET.Vulkan.Format.R16G16Unorm;
                case PixelFormat.R16_G16_SNorm:
                    return Silk.NET.Vulkan.Format.R16G16SNorm;
                case PixelFormat.R16_G16_UInt:
                    return Silk.NET.Vulkan.Format.R16G16Uint;
                case PixelFormat.R16_G16_SInt:
                    return Silk.NET.Vulkan.Format.R16G16Sint;
                case PixelFormat.R16_G16_Float:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Sfloat;

                case PixelFormat.R32_G32_UInt:
                    return Silk.NET.Vulkan.Format.R32G32Uint;
                case PixelFormat.R32_G32_SInt:
                    return Silk.NET.Vulkan.Format.R32G32Sint;
                case PixelFormat.R32_G32_Float:
                    return Silk.NET.Vulkan.Format.R32G32B32A32Sfloat;

                case PixelFormat.R8_G8_B8_A8_UNorm:
                    return Silk.NET.Vulkan.Format.R8G8B8A8Unorm;
                case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                    return Silk.NET.Vulkan.Format.R8G8B8A8Srgb;
                case PixelFormat.B8_G8_R8_A8_UNorm:
                    return Silk.NET.Vulkan.Format.B8G8R8A8Unorm;
                case PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                    return Silk.NET.Vulkan.Format.B8G8R8A8Srgb;
                case PixelFormat.R8_G8_B8_A8_SNorm:
                    return Silk.NET.Vulkan.Format.R8G8B8A8SNorm;
                case PixelFormat.R8_G8_B8_A8_UInt:
                    return Silk.NET.Vulkan.Format.R8G8B8A8Uint;
                case PixelFormat.R8_G8_B8_A8_SInt:
                    return Silk.NET.Vulkan.Format.R8G8B8A8Sint;

                case PixelFormat.R16_G16_B16_A16_UNorm:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Unorm;
                case PixelFormat.R16_G16_B16_A16_SNorm:
                    return Silk.NET.Vulkan.Format.R16G16B16A16SNorm;
                case PixelFormat.R16_G16_B16_A16_UInt:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Uint;
                case PixelFormat.R16_G16_B16_A16_SInt:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Sint;
                case PixelFormat.R16_G16_B16_A16_Float:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Sfloat;

                case PixelFormat.R32_G32_B32_A32_UInt:
                    return Silk.NET.Vulkan.Format.R32G32B32A32Uint;
                case PixelFormat.R32_G32_B32_A32_SInt:
                    return Silk.NET.Vulkan.Format.R32G32B32A32Sint;
                case PixelFormat.R32_G32_B32_A32_Float:
                    return Silk.NET.Vulkan.Format.R32G32B32A32Sfloat;

                case PixelFormat.BC1_Rgb_UNorm:
                    return Silk.NET.Vulkan.Format.BC1RgbUnormBlock;
                case PixelFormat.BC1_Rgb_UNorm_SRgb:
                    return Silk.NET.Vulkan.Format.BC1RgbSrgbBlock;
                case PixelFormat.BC1_Rgba_UNorm:
                    return Silk.NET.Vulkan.Format.BC1RgbaUnormBlock;
                case PixelFormat.BC1_Rgba_UNorm_SRgb:
                    return Silk.NET.Vulkan.Format.BC1RgbaSrgbBlock;
                case PixelFormat.BC2_UNorm:
                    return Silk.NET.Vulkan.Format.BC2UnormBlock;
                case PixelFormat.BC2_UNorm_SRgb:
                    return Silk.NET.Vulkan.Format.BC2SrgbBlock;
                case PixelFormat.BC3_UNorm:
                    return Silk.NET.Vulkan.Format.BC3UnormBlock;
                case PixelFormat.BC3_UNorm_SRgb:
                    return Silk.NET.Vulkan.Format.BC3SrgbBlock;
                case PixelFormat.BC4_UNorm:
                    return Silk.NET.Vulkan.Format.BC4UnormBlock;
                case PixelFormat.BC4_SNorm:
                    return Silk.NET.Vulkan.Format.BC4SNormBlock;
                case PixelFormat.BC5_UNorm:
                    return Silk.NET.Vulkan.Format.BC5UnormBlock;
                case PixelFormat.BC5_SNorm:
                    return Silk.NET.Vulkan.Format.BC5SNormBlock;
                case PixelFormat.BC7_UNorm:
                    return Silk.NET.Vulkan.Format.BC7UnormBlock;
                case PixelFormat.BC7_UNorm_SRgb:
                    return Silk.NET.Vulkan.Format.BC7SrgbBlock;

                case PixelFormat.ETC2_R8_G8_B8_UNorm:
                    return Silk.NET.Vulkan.Format.Etc2R8G8B8UnormBlock;
                case PixelFormat.ETC2_R8_G8_B8_A1_UNorm:
                    return Silk.NET.Vulkan.Format.Etc2R8G8B8A1UnormBlock;
                case PixelFormat.ETC2_R8_G8_B8_A8_UNorm:
                    return Silk.NET.Vulkan.Format.Etc2R8G8B8A8UnormBlock;

                case PixelFormat.D32_Float_S8_UInt:
                    return Silk.NET.Vulkan.Format.D32SfloatS8Uint;
                case PixelFormat.D24_UNorm_S8_UInt:
                    return Silk.NET.Vulkan.Format.D24UnormS8Uint;

                case PixelFormat.R10_G10_B10_A2_UNorm:
                    return Silk.NET.Vulkan.Format.A2B10G10R10UnormPack32;
                case PixelFormat.R10_G10_B10_A2_UInt:
                    return Silk.NET.Vulkan.Format.A2B10G10R10UintPack32;
                case PixelFormat.R11_G11_B10_Float:
                    return Silk.NET.Vulkan.Format.B10G11R11UfloatPack32;

                default:
                    throw new VeldridException($"Invalid {nameof(PixelFormat)}: {format}");
            }
        }
    }
}
