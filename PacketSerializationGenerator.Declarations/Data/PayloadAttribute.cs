using System;

namespace PacketSerializationGenerator.Declarations.Data;

/// <summary>
/// When used on a property of type IDataPacket, indicates that the property should be prefixed with a <c>uint</c> value
/// containing its serialized length and the OP code of the subclass should not be included in the serialized result.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PayloadAttribute : Attribute
{
}
