namespace Striked3D.Core.Interfaces
{
    public struct SerializableField<T>
    {
        public T Value { get; set; }
    }
    public interface ISerializable
    {
        public byte[] Serialize();
        public void Deserialize(byte[] array);
    }
}
