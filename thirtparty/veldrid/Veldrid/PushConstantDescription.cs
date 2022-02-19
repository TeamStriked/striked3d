using System;

namespace Veldrid
{
    /// <summary>
    /// A <see cref="Pipeline"/> component describing how values are blended into each individual color target.
    /// </summary>
    public struct PushConstantDescription : IEquatable<PushConstantDescription>
    {
        /// <summary>
        /// A constant blend color used in <see cref="BlendFactor.BlendFactor"/> and <see cref="BlendFactor.InverseBlendFactor"/>,
        /// or otherwise ignored.
        /// </summary>
        public uint SizeInBytes;

        /// <summary>
        /// Constructs a new <see cref="BlendStateDescription"/>,
        /// </summary>
        /// <param name="sizeInBytes">The size of the push constant.</param>
        public PushConstantDescription(uint sizeInBytes)
        {
            SizeInBytes = sizeInBytes;
        }

        /// <summary>
        /// Element-wise equality.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True if all elements and all array elements are equal; false otherswise.</returns>
        public bool Equals(PushConstantDescription other)
        {
            return (SizeInBytes == other.SizeInBytes);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return SizeInBytes.GetHashCode();
        }

        internal PushConstantDescription ShallowClone()
        {
            PushConstantDescription result = this;
            return result;
        }
    }
}
