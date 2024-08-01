using System;

namespace PacketSerializationGenerators.Declarations.Xml;

/// <summary>
/// Indicates that the decorated class/property should
/// use the given custom name when serialized.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class XmlNameAttribute : Attribute
{
    public string Name { get; }

    public XmlNameAttribute(string name)
    {
        Name = name;
    }
}
