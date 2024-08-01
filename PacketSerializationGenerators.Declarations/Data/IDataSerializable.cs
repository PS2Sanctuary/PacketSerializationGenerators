using System;

namespace PacketSerializationGenerators.Declarations.Data;

/// <summary>
/// Represents a binary-serializable object.
/// </summary>
public interface IDataSerializable
{
    /// <summary>
    /// Gets the number of bytes consumed by this object when binary-serialized.
    /// </summary>
    /// <returns>The number of bytes required to serialize this object.</returns>
    int GetRequiredBufferSize();

    /// <summary>
    /// Serializes this object to a binary representation.
    /// </summary>
    /// <param name="buffer">The buffer to serialize into.</param>
    /// <param name="offset">
    /// The offset into the buffer at which to start serializing.This is incremented by the amount of data written to
    /// the buffer.
    /// </param>
    void Serialize(Span<byte> buffer, ref int offset);
}

/// <summary>
/// Represents a binary de/serializable object.
/// </summary>
/// <typeparam name="T">The type of the object to deserialize to.</typeparam>
public interface IDataSerializable<out T> : IDataSerializable
{
    /// <summary>
    /// Deserializes this object from a binary representation.
    /// </summary>
    /// <param name="buffer">The buffer to deserialize from.</param>
    /// <param name="offset">
    /// The offset into the buffer at which to start deserializing. This is incremented by the amount of data consumed
    /// during deserialization.
    /// </param>
    /// <returns></returns>
    abstract static T Deserialize(ReadOnlySpan<byte> buffer, ref int offset);
}
