using System;

namespace PacketSerializationGenerators.Declarations.Binary;

/// <summary>
/// Indicates that the annotated IDataPacket should be generated such that its contents are de/compressed
/// using the LZ4 algorithm.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LZ4CompressedAttribute : Attribute
{
}
