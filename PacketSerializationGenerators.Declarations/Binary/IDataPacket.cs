using System;

namespace PacketSerializationGenerators.Declarations.Binary;

/// <summary>
/// Represents a data packet.
/// </summary>
public interface IDataPacket
{
    /// <summary>
    /// Gets the required buffer length to be able to serialize this packet.
    /// Actual serialized data may take less space than the value provided
    /// by this method.
    /// </summary>
    /// <param name="includeOPCode">A value indicating whether to include the size of the OP code.</param>
    /// <returns>The buffer length.</returns>
    int GetRequiredBufferSize(bool includeOPCode = true);

    /// <summary>
    /// Serializes the packet to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="writeOPCode">A value indicating whether to write the OP code of the packet.</param>
    /// <returns>The amount of data that was serialized to the buffer.</returns>
    int Serialize(Span<byte> buffer, bool writeOPCode = true);
}

/// <summary>
/// Represents a data packet.
/// </summary>
/// <typeparam name="T">The concrete type of the data packet.</typeparam>
public interface IDataPacket<out T> : IDataPacket where T : class
{
    /// <summary>
    /// Deserializes the data packet from the given buffer.
    /// </summary>
    /// <param name="buffer">The packet data.</param>
    /// <param name="amountRead">
    /// The amount of data that was read from the buffer. This value should be equal
    /// to the length of the buffer, unless the packet structure is incorrect.
    /// </param>
    /// <param name="readOPCode">A value indicating whether the buffer contains the OP code of the packet.</param>
    /// <returns>A login packet instance.</returns>
    abstract static T Deserialize(ReadOnlySpan<byte> buffer, out int amountRead, bool readOPCode = true);
}
