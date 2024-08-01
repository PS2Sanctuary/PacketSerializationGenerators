using System;

namespace PacketSerializationGenerators.Declarations.Data;

/// <summary>
/// Indicates that the decorated class should be extended with source-generated serialization logic for data packets.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public abstract class BaseDataPacketAttribute : Attribute
{
}
