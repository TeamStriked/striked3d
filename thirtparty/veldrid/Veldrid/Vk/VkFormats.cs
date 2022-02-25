using System;
using System.Collections.Generic;
using Veldrid;

namespace Veldrid.Vk
{
    internal static partial class VkFormats
    {
        internal static Silk.NET.Vulkan.SamplerAddressMode VdToVkSamplerAddressMode(SamplerAddressMode mode)
        {
            switch (mode)
            {
                case SamplerAddressMode.Wrap:
                    return Silk.NET.Vulkan.SamplerAddressMode.Repeat;
                case SamplerAddressMode.Mirror:
                    return Silk.NET.Vulkan.SamplerAddressMode.MirroredRepeat;
                case SamplerAddressMode.Clamp:
                    return Silk.NET.Vulkan.SamplerAddressMode.ClampToEdge;
                case SamplerAddressMode.Border:
                    return Silk.NET.Vulkan.SamplerAddressMode.ClampToBorder;
                default:
                    throw Illegal.Value<SamplerAddressMode>();
            }
        }

        internal static void GetFilterParams(
            SamplerFilter filter,
            out Silk.NET.Vulkan.Filter minFilter,
            out Silk.NET.Vulkan.Filter magFilter,
            out Silk.NET.Vulkan.SamplerMipmapMode mipmapMode)
        {
            switch (filter)
            {
                case SamplerFilter.Anisotropic:
                    minFilter = Silk.NET.Vulkan.Filter.Linear;
                    magFilter = Silk.NET.Vulkan.Filter.Linear;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.MinPoint_MagPoint_MipPoint:
                    minFilter = Silk.NET.Vulkan.Filter.Nearest;
                    magFilter = Silk.NET.Vulkan.Filter.Nearest;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinPoint_MagPoint_MipLinear:
                    minFilter = Silk.NET.Vulkan.Filter.Nearest;
                    magFilter = Silk.NET.Vulkan.Filter.Nearest;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.MinPoint_MagLinear_MipPoint:
                    minFilter = Silk.NET.Vulkan.Filter.Nearest;
                    magFilter = Silk.NET.Vulkan.Filter.Linear;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinPoint_MagLinear_MipLinear:
                    minFilter = Silk.NET.Vulkan.Filter.Nearest;
                    magFilter = Silk.NET.Vulkan.Filter.Linear;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.MinLinear_MagPoint_MipPoint:
                    minFilter = Silk.NET.Vulkan.Filter.Linear;
                    magFilter = Silk.NET.Vulkan.Filter.Nearest;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinLinear_MagPoint_MipLinear:
                    minFilter = Silk.NET.Vulkan.Filter.Linear;
                    magFilter = Silk.NET.Vulkan.Filter.Nearest;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.MinLinear_MagLinear_MipPoint:
                    minFilter = Silk.NET.Vulkan.Filter.Linear;
                    magFilter = Silk.NET.Vulkan.Filter.Linear;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinLinear_MagLinear_MipLinear:
                    minFilter = Silk.NET.Vulkan.Filter.Linear;
                    magFilter = Silk.NET.Vulkan.Filter.Linear;
                    mipmapMode = Silk.NET.Vulkan.SamplerMipmapMode.Linear;
                    break;
                default:
                    throw Illegal.Value<SamplerFilter>();
            }
        }

        internal static Silk.NET.Vulkan.ImageUsageFlags VdToVkTextureUsage(TextureUsage vdUsage)
        {
            Silk.NET.Vulkan.ImageUsageFlags vkUsage = 0;

            vkUsage = Silk.NET.Vulkan.ImageUsageFlags.ImageUsageTransferDstBit | Silk.NET.Vulkan.ImageUsageFlags.ImageUsageTransferSrcBit;
            bool isDepthStencil = (vdUsage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;
            if ((vdUsage & TextureUsage.Sampled) == TextureUsage.Sampled)
            {
                vkUsage |= Silk.NET.Vulkan.ImageUsageFlags.ImageUsageSampledBit;
            }
            if (isDepthStencil)
            {
                vkUsage |= Silk.NET.Vulkan.ImageUsageFlags.ImageUsageDepthStencilAttachmentBit;
            }
            if ((vdUsage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
            {
                vkUsage |= Silk.NET.Vulkan.ImageUsageFlags.ImageUsageColorAttachmentBit;
            }
            if ((vdUsage & TextureUsage.Storage) == TextureUsage.Storage)
            {
                vkUsage |= Silk.NET.Vulkan.ImageUsageFlags.ImageUsageStorageBit;
            }

            return vkUsage;
        }

        internal static Silk.NET.Vulkan.ImageType VdToVkTextureType(TextureType type)
        {
            switch (type)
            {
                case TextureType.Texture1D:
                    return Silk.NET.Vulkan.ImageType.ImageType1D;
                case TextureType.Texture2D:
                    return Silk.NET.Vulkan.ImageType.ImageType2D;
                case TextureType.Texture3D:
                    return Silk.NET.Vulkan.ImageType.ImageType3D;
                default:
                    throw Illegal.Value<TextureType>();
            }
        }

        internal static Silk.NET.Vulkan.DescriptorType VdToVkDescriptorType(ResourceKind kind, ResourceLayoutElementOptions options)
        {
            bool dynamicBinding = (options & ResourceLayoutElementOptions.DynamicBinding) != 0;
            switch (kind)
            {
                case ResourceKind.UniformBuffer:
                    return dynamicBinding ? Silk.NET.Vulkan.DescriptorType.UniformBufferDynamic : Silk.NET.Vulkan.DescriptorType.UniformBuffer;
                case ResourceKind.StructuredBufferReadWrite:
                case ResourceKind.StructuredBufferReadOnly:
                    return dynamicBinding ? Silk.NET.Vulkan.DescriptorType.StorageBufferDynamic : Silk.NET.Vulkan.DescriptorType.StorageBuffer;
                case ResourceKind.TextureReadOnly:
                    return Silk.NET.Vulkan.DescriptorType.SampledImage;
                case ResourceKind.TextureReadWrite:
                    return Silk.NET.Vulkan.DescriptorType.StorageImage;
                case ResourceKind.Sampler:
                    return Silk.NET.Vulkan.DescriptorType.Sampler;
                default:
                    throw Illegal.Value<ResourceKind>();
            }
        }

        internal static Silk.NET.Vulkan.SampleCountFlags VdToVkSampleCount(TextureSampleCount sampleCount)
        {
            switch (sampleCount)
            {
                case TextureSampleCount.Count1:
                    return Silk.NET.Vulkan.SampleCountFlags.SampleCount1Bit;
                case TextureSampleCount.Count2:
                    return Silk.NET.Vulkan.SampleCountFlags.SampleCount2Bit;
                case TextureSampleCount.Count4:
                    return Silk.NET.Vulkan.SampleCountFlags.SampleCount4Bit;
                case TextureSampleCount.Count8:
                    return Silk.NET.Vulkan.SampleCountFlags.SampleCount8Bit;
                case TextureSampleCount.Count16:
                    return Silk.NET.Vulkan.SampleCountFlags.SampleCount16Bit;
                case TextureSampleCount.Count32:
                    return Silk.NET.Vulkan.SampleCountFlags.SampleCount32Bit;
                default:
                    throw Illegal.Value<TextureSampleCount>();
            }
        }

        internal static Silk.NET.Vulkan.StencilOp VdToVkStencilOp(StencilOperation op)
        {
            switch (op)
            {
                case StencilOperation.Keep:
                    return Silk.NET.Vulkan.StencilOp.Keep;
                case StencilOperation.Zero:
                    return Silk.NET.Vulkan.StencilOp.Zero;
                case StencilOperation.Replace:
                    return Silk.NET.Vulkan.StencilOp.Replace;
                case StencilOperation.IncrementAndClamp:
                    return Silk.NET.Vulkan.StencilOp.IncrementAndClamp;
                case StencilOperation.DecrementAndClamp:
                    return Silk.NET.Vulkan.StencilOp.DecrementAndClamp;
                case StencilOperation.Invert:
                    return Silk.NET.Vulkan.StencilOp.Invert;
                case StencilOperation.IncrementAndWrap:
                    return Silk.NET.Vulkan.StencilOp.IncrementAndWrap;
                case StencilOperation.DecrementAndWrap:
                    return Silk.NET.Vulkan.StencilOp.DecrementAndWrap;
                default:
                    throw Illegal.Value<StencilOperation>();
            }
        }

        internal static Silk.NET.Vulkan.PolygonMode VdToVkPolygonMode(PolygonFillMode fillMode)
        {
            switch (fillMode)
            {
                case PolygonFillMode.Solid:
                    return Silk.NET.Vulkan.PolygonMode.Fill;
                case PolygonFillMode.Wireframe:
                    return Silk.NET.Vulkan.PolygonMode.Line;
                default:
                    throw Illegal.Value<PolygonFillMode>();
            }
        }

        internal static Silk.NET.Vulkan.CullModeFlags VdToVkCullMode(FaceCullMode cullMode)
        {
            switch (cullMode)
            {
                case FaceCullMode.Back:
                    return Silk.NET.Vulkan.CullModeFlags.CullModeBackBit;
                case FaceCullMode.Front:
                    return Silk.NET.Vulkan.CullModeFlags.CullModeFrontBit;
                case FaceCullMode.None:
                    return Silk.NET.Vulkan.CullModeFlags.CullModeNone;
                default:
                    throw Illegal.Value<FaceCullMode>();
            }
        }

        internal static Silk.NET.Vulkan.BlendOp VdToVkBlendOp(BlendFunction func)
        {
            switch (func)
            {
                case BlendFunction.Add:
                    return Silk.NET.Vulkan.BlendOp.Add;
                case BlendFunction.Subtract:
                    return Silk.NET.Vulkan.BlendOp.Subtract;
                case BlendFunction.ReverseSubtract:
                    return Silk.NET.Vulkan.BlendOp.ReverseSubtract;
                case BlendFunction.Minimum:
                    return Silk.NET.Vulkan.BlendOp.Min;
                case BlendFunction.Maximum:
                    return Silk.NET.Vulkan.BlendOp.Max;
                default:
                    throw Illegal.Value<BlendFunction>();
            }
        }

        internal static Silk.NET.Vulkan.PrimitiveTopology VdToVkPrimitiveTopology(PrimitiveTopology topology)
        {
            switch (topology)
            {
                case PrimitiveTopology.TriangleList:
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleList;
                case PrimitiveTopology.TriangleStrip:
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleStrip;
                case PrimitiveTopology.LineList:
                    return Silk.NET.Vulkan.PrimitiveTopology.LineList;
                case PrimitiveTopology.LineStrip:
                    return Silk.NET.Vulkan.PrimitiveTopology.LineStrip;
                case PrimitiveTopology.PointList:
                    return Silk.NET.Vulkan.PrimitiveTopology.PointList;
                default:
                    throw Illegal.Value<PrimitiveTopology>();
            }
        }

        internal static uint GetSpecializationConstantSize(ShaderConstantType type)
        {
            switch (type)
            {
                case ShaderConstantType.Bool:
                    return 4;
                case ShaderConstantType.UInt16:
                    return 2;
                case ShaderConstantType.Int16:
                    return 2;
                case ShaderConstantType.UInt32:
                    return 4;
                case ShaderConstantType.Int32:
                    return 4;
                case ShaderConstantType.UInt64:
                    return 8;
                case ShaderConstantType.Int64:
                    return 8;
                case ShaderConstantType.Float:
                    return 4;
                case ShaderConstantType.Double:
                    return 8;
                default:
                    throw Illegal.Value<ShaderConstantType>();
            }
        }

        internal static Silk.NET.Vulkan.BlendFactor VdToVkBlendFactor(BlendFactor factor)
        {
            switch (factor)
            {
                case BlendFactor.Zero:
                    return Silk.NET.Vulkan.BlendFactor.Zero;
                case BlendFactor.One:
                    return Silk.NET.Vulkan.BlendFactor.One;
                case BlendFactor.SourceAlpha:
                    return Silk.NET.Vulkan.BlendFactor.SrcAlpha;
                case BlendFactor.InverseSourceAlpha:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusSrcAlpha;
                case BlendFactor.DestinationAlpha:
                    return Silk.NET.Vulkan.BlendFactor.DstAlpha;
                case BlendFactor.InverseDestinationAlpha:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusDstAlpha;
                case BlendFactor.SourceColor:
                    return Silk.NET.Vulkan.BlendFactor.SrcColor;
                case BlendFactor.InverseSourceColor:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusSrcColor;
                case BlendFactor.DestinationColor:
                    return Silk.NET.Vulkan.BlendFactor.DstColor;
                case BlendFactor.InverseDestinationColor:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusDstColor;
                case BlendFactor.BlendFactor:
                    return Silk.NET.Vulkan.BlendFactor.ConstantColor;
                case BlendFactor.InverseBlendFactor:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusConstantColor;
                default:
                    throw Illegal.Value<BlendFactor>();
            }
        }

        internal static Silk.NET.Vulkan.Format VdToVkVertexElementFormat(VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Float1:
                    return Silk.NET.Vulkan.Format.R32Sfloat;
                case VertexElementFormat.Float2:
                    return Silk.NET.Vulkan.Format.R32G32Sfloat;
                case VertexElementFormat.Float3:
                    return Silk.NET.Vulkan.Format.R32G32B32Sfloat;
                case VertexElementFormat.Float4:
                    return Silk.NET.Vulkan.Format.R32G32B32A32Sfloat;
                case VertexElementFormat.Byte2_Norm:
                    return Silk.NET.Vulkan.Format.R8G8Unorm;
                case VertexElementFormat.Byte2:
                    return Silk.NET.Vulkan.Format.R8G8Uint;
                case VertexElementFormat.Byte4_Norm:
                    return Silk.NET.Vulkan.Format.R8G8B8A8Unorm;
                case VertexElementFormat.Byte4:
                    return Silk.NET.Vulkan.Format.R8G8B8A8Uint;
                case VertexElementFormat.SByte2_Norm:
                    return Silk.NET.Vulkan.Format.R8G8SNorm;
                case VertexElementFormat.SByte2:
                    return Silk.NET.Vulkan.Format.R8G8Sint;
                case VertexElementFormat.SByte4_Norm:
                    return Silk.NET.Vulkan.Format.R8G8B8A8SNorm;
                case VertexElementFormat.SByte4:
                    return Silk.NET.Vulkan.Format.R8G8B8A8Sint;
                case VertexElementFormat.UShort2_Norm:
                    return Silk.NET.Vulkan.Format.R16G16Unorm;
                case VertexElementFormat.UShort2:
                    return Silk.NET.Vulkan.Format.R16G16Uint;
                case VertexElementFormat.UShort4_Norm:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Unorm;
                case VertexElementFormat.UShort4:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Uint;
                case VertexElementFormat.Short2_Norm:
                    return Silk.NET.Vulkan.Format.R16G16SNorm;
                case VertexElementFormat.Short2:
                    return Silk.NET.Vulkan.Format.R16G16Sint;
                case VertexElementFormat.Short4_Norm:
                    return Silk.NET.Vulkan.Format.R16G16B16A16SNorm;
                case VertexElementFormat.Short4:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Sint;
                case VertexElementFormat.UInt1:
                    return Silk.NET.Vulkan.Format.R32Uint;
                case VertexElementFormat.UInt2:
                    return Silk.NET.Vulkan.Format.R32G32Uint;
                case VertexElementFormat.UInt3:
                    return Silk.NET.Vulkan.Format.R32G32B32Uint;
                case VertexElementFormat.UInt4:
                    return Silk.NET.Vulkan.Format.R32G32B32A32Uint;
                case VertexElementFormat.Int1:
                    return Silk.NET.Vulkan.Format.R32Sint;
                case VertexElementFormat.Int2:
                    return Silk.NET.Vulkan.Format.R32G32Sint;
                case VertexElementFormat.Int3:
                    return Silk.NET.Vulkan.Format.R32G32B32Sint;
                case VertexElementFormat.Int4:
                    return Silk.NET.Vulkan.Format.R32G32B32A32Sint;
                case VertexElementFormat.Half1:
                    return Silk.NET.Vulkan.Format.R16Sfloat;
                case VertexElementFormat.Half2:
                    return Silk.NET.Vulkan.Format.R16G16Sfloat;
                case VertexElementFormat.Half4:
                    return Silk.NET.Vulkan.Format.R16G16B16A16Sfloat;
                default:
                    throw Illegal.Value<VertexElementFormat>();
            }
        }

        internal static Silk.NET.Vulkan.ShaderStageFlags VdToVkShaderStages(ShaderStages stage)
        {
            Silk.NET.Vulkan.ShaderStageFlags ret = 0;

            if ((stage & ShaderStages.Vertex) == ShaderStages.Vertex)
                ret |= Silk.NET.Vulkan.ShaderStageFlags.ShaderStageVertexBit;

            if ((stage & ShaderStages.Geometry) == ShaderStages.Geometry)
                ret |= Silk.NET.Vulkan.ShaderStageFlags.ShaderStageGeometryBit;

            if ((stage & ShaderStages.TessellationControl) == ShaderStages.TessellationControl)
                ret |= Silk.NET.Vulkan.ShaderStageFlags.ShaderStageTessellationControlBit;

            if ((stage & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation)
                ret |= Silk.NET.Vulkan.ShaderStageFlags.ShaderStageTessellationEvaluationBit;

            if ((stage & ShaderStages.Fragment) == ShaderStages.Fragment)
                ret |= Silk.NET.Vulkan.ShaderStageFlags.ShaderStageFragmentBit;

            if ((stage & ShaderStages.Compute) == ShaderStages.Compute)
                ret |= Silk.NET.Vulkan.ShaderStageFlags.ShaderStageComputeBit;

            return ret;
        }

        internal static Silk.NET.Vulkan.BorderColor VdToVkSamplerBorderColor(SamplerBorderColor borderColor)
        {
            switch (borderColor)
            {
                case SamplerBorderColor.TransparentBlack:
                    return Silk.NET.Vulkan.BorderColor.FloatTransparentBlack;
                case SamplerBorderColor.OpaqueBlack:
                    return Silk.NET.Vulkan.BorderColor.FloatOpaqueBlack;
                case SamplerBorderColor.OpaqueWhite:
                    return Silk.NET.Vulkan.BorderColor.FloatOpaqueWhite;
                default:
                    throw Illegal.Value<SamplerBorderColor>();
            }
        }

        internal static Silk.NET.Vulkan.IndexType VdToVkIndexFormat(IndexFormat format)
        {
            switch (format)
            {
                case IndexFormat.UInt16:
                    return Silk.NET.Vulkan.IndexType.Uint16;
                case IndexFormat.UInt32:
                    return Silk.NET.Vulkan.IndexType.Uint32;
                default:
                    throw Illegal.Value<IndexFormat>();
            }
        }

        internal static Silk.NET.Vulkan.CompareOp VdToVkCompareOp(ComparisonKind comparisonKind)
        {
            switch (comparisonKind)
            {
                case ComparisonKind.Never:
                    return Silk.NET.Vulkan.CompareOp.Never;
                case ComparisonKind.Less:
                    return Silk.NET.Vulkan.CompareOp.Less;
                case ComparisonKind.Equal:
                    return Silk.NET.Vulkan.CompareOp.Equal;
                case ComparisonKind.LessEqual:
                    return Silk.NET.Vulkan.CompareOp.LessOrEqual;
                case ComparisonKind.Greater:
                    return Silk.NET.Vulkan.CompareOp.Greater;
                case ComparisonKind.NotEqual:
                    return Silk.NET.Vulkan.CompareOp.NotEqual;
                case ComparisonKind.GreaterEqual:
                    return Silk.NET.Vulkan.CompareOp.GreaterOrEqual;
                case ComparisonKind.Always:
                    return Silk.NET.Vulkan.CompareOp.Always;
                default:
                    throw Illegal.Value<ComparisonKind>();
            }
        }

        internal static PixelFormat VkToVdPixelFormat(Silk.NET.Vulkan.Format vkFormat)
        {
            switch (vkFormat)
            {
                case Silk.NET.Vulkan.Format.R8Unorm:
                    return PixelFormat.R8_UNorm;
                case Silk.NET.Vulkan.Format.R8SNorm:
                    return PixelFormat.R8_SNorm;
                case Silk.NET.Vulkan.Format.R8Uint:
                    return PixelFormat.R8_UInt;
                case Silk.NET.Vulkan.Format.R8Sint:
                    return PixelFormat.R8_SInt;

                case Silk.NET.Vulkan.Format.R16Unorm:
                    return PixelFormat.R16_UNorm;
                case Silk.NET.Vulkan.Format.R16SNorm:
                    return PixelFormat.R16_SNorm;
                case Silk.NET.Vulkan.Format.R16Uint:
                    return PixelFormat.R16_UInt;
                case Silk.NET.Vulkan.Format.R16Sint:
                    return PixelFormat.R16_SInt;
                case Silk.NET.Vulkan.Format.R16Sfloat:
                    return PixelFormat.R16_Float;

                case Silk.NET.Vulkan.Format.R32Uint:
                    return PixelFormat.R32_UInt;
                case Silk.NET.Vulkan.Format.R32Sint:
                    return PixelFormat.R32_SInt;
                case Silk.NET.Vulkan.Format.R32Sfloat:
                case Silk.NET.Vulkan.Format.D32Sfloat:
                    return PixelFormat.R32_Float;

                case Silk.NET.Vulkan.Format.R8G8Unorm:
                    return PixelFormat.R8_G8_UNorm;
                case Silk.NET.Vulkan.Format.R8G8SNorm:
                    return PixelFormat.R8_G8_SNorm;
                case Silk.NET.Vulkan.Format.R8G8Uint:
                    return PixelFormat.R8_G8_UInt;
                case Silk.NET.Vulkan.Format.R8G8Sint:
                    return PixelFormat.R8_G8_SInt;

                case Silk.NET.Vulkan.Format.R16G16Unorm:
                    return PixelFormat.R16_G16_UNorm;
                case Silk.NET.Vulkan.Format.R16G16SNorm:
                    return PixelFormat.R16_G16_SNorm;
                case Silk.NET.Vulkan.Format.R16G16Uint:
                    return PixelFormat.R16_G16_UInt;
                case Silk.NET.Vulkan.Format.R16G16Sint:
                    return PixelFormat.R16_G16_SInt;
                case Silk.NET.Vulkan.Format.R16G16Sfloat:
                    return PixelFormat.R16_G16_Float;

                case Silk.NET.Vulkan.Format.R32G32Uint:
                    return PixelFormat.R32_G32_UInt;
                case Silk.NET.Vulkan.Format.R32G32Sint:
                    return PixelFormat.R32_G32_SInt;
                case Silk.NET.Vulkan.Format.R32G32Sfloat:
                    return PixelFormat.R32_G32_Float;

                case Silk.NET.Vulkan.Format.R8G8B8A8Unorm:
                    return PixelFormat.R8_G8_B8_A8_UNorm;
                case Silk.NET.Vulkan.Format.R8G8B8A8Srgb:
                    return PixelFormat.R8_G8_B8_A8_UNorm_SRgb;
                case Silk.NET.Vulkan.Format.B8G8R8A8Unorm:
                    return PixelFormat.B8_G8_R8_A8_UNorm;
                case Silk.NET.Vulkan.Format.B8G8R8A8Srgb:
                    return PixelFormat.B8_G8_R8_A8_UNorm_SRgb;
                case Silk.NET.Vulkan.Format.R8G8B8A8SNorm:
                    return PixelFormat.R8_G8_B8_A8_SNorm;
                case Silk.NET.Vulkan.Format.R8G8B8A8Uint:
                    return PixelFormat.R8_G8_B8_A8_UInt;
                case Silk.NET.Vulkan.Format.R8G8B8A8Sint:
                    return PixelFormat.R8_G8_B8_A8_SInt;

                case Silk.NET.Vulkan.Format.R16G16B16A16Unorm:
                    return PixelFormat.R16_G16_B16_A16_UNorm;
                case Silk.NET.Vulkan.Format.R16G16B16A16SNorm:
                    return PixelFormat.R16_G16_B16_A16_SNorm;
                case Silk.NET.Vulkan.Format.R16G16B16A16Uint:
                    return PixelFormat.R16_G16_B16_A16_UInt;
                case Silk.NET.Vulkan.Format.R16G16B16A16Sint:
                    return PixelFormat.R16_G16_B16_A16_SInt;
                case Silk.NET.Vulkan.Format.R16G16B16A16Sfloat:
                    return PixelFormat.R16_G16_B16_A16_Float;

                case Silk.NET.Vulkan.Format.R32G32B32A32Uint:
                    return PixelFormat.R32_G32_B32_A32_UInt;
                case Silk.NET.Vulkan.Format.R32G32B32A32Sint:
                    return PixelFormat.R32_G32_B32_A32_SInt;
                case Silk.NET.Vulkan.Format.R32G32B32A32Sfloat:
                    return PixelFormat.R32_G32_B32_A32_Float;

                case Silk.NET.Vulkan.Format.BC1RgbUnormBlock:
                    return PixelFormat.BC1_Rgb_UNorm;
                case Silk.NET.Vulkan.Format.BC1RgbSrgbBlock:
                    return PixelFormat.BC1_Rgb_UNorm_SRgb;
                case Silk.NET.Vulkan.Format.BC1RgbaUnormBlock:
                    return PixelFormat.BC1_Rgba_UNorm;
                case Silk.NET.Vulkan.Format.BC1RgbaSrgbBlock:
                    return PixelFormat.BC1_Rgba_UNorm_SRgb;
                case Silk.NET.Vulkan.Format.BC2UnormBlock:
                    return PixelFormat.BC2_UNorm;
                case Silk.NET.Vulkan.Format.BC2SrgbBlock:
                    return PixelFormat.BC2_UNorm_SRgb;
                case Silk.NET.Vulkan.Format.BC3UnormBlock:
                    return PixelFormat.BC3_UNorm;
                case Silk.NET.Vulkan.Format.BC3SrgbBlock:
                    return PixelFormat.BC3_UNorm_SRgb;
                case Silk.NET.Vulkan.Format.BC4UnormBlock:
                    return PixelFormat.BC4_UNorm;
                case Silk.NET.Vulkan.Format.BC4SNormBlock:
                    return PixelFormat.BC4_SNorm;
                case Silk.NET.Vulkan.Format.BC5UnormBlock:
                    return PixelFormat.BC5_UNorm;
                case Silk.NET.Vulkan.Format.BC5SNormBlock:
                    return PixelFormat.BC5_SNorm;
                case Silk.NET.Vulkan.Format.BC7UnormBlock:
                    return PixelFormat.BC7_UNorm;
                case Silk.NET.Vulkan.Format.BC7SrgbBlock:
                    return PixelFormat.BC7_UNorm_SRgb;

                case Silk.NET.Vulkan.Format.A2B10G10R10UnormPack32:
                    return PixelFormat.R10_G10_B10_A2_UNorm;
                case Silk.NET.Vulkan.Format.A2B10G10R10UintPack32:
                    return PixelFormat.R10_G10_B10_A2_UInt;
                case Silk.NET.Vulkan.Format.B10G11R11UfloatPack32:
                    return PixelFormat.R11_G11_B10_Float;

                default:
                    throw Illegal.Value<Silk.NET.Vulkan.Format>();
            }
        }
    }
}
