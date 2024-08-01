using System;

namespace PacketSerializationGenerator.Declarations.Xml;

/// <summary>
/// Indicates that the decorated class should be extended with
/// source-generated serialization logic for XML object.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class XmlSerializableAttribute : Attribute
{
}
