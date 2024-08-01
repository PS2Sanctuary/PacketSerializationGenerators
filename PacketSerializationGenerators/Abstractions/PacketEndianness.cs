namespace PacketSerializationGenerators.Abstractions;

/// <summary>
/// Defines the endianness that a value can be serialized to.
/// </summary>
public enum PacketEndianness
{
    /// <summary>
    /// Indicates that a value should be serialized in little-endian format.
    /// </summary>
    LittleEndian,

    /// <summary>
    /// Indicates that a value should be serialized in big-endian format.
    /// </summary>
    BigEndian
}
