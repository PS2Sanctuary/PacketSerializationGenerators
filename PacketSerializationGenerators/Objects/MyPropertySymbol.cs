using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Extensions;
using System.Collections.Immutable;

namespace PacketSerializationGenerators.Objects;

public class MyPropertySymbol
{
    public string Name { get; }
    public SpecialType SpecialType { get; }
    public ITypeSymbol? Type { get; }
    public ImmutableArray<AttributeData>? AttributeData { get; }

    public MyPropertySymbol(IPropertySymbol propertySymbol)
        : this(propertySymbol.Name, propertySymbol.Type.SpecialType)
    {
        Type = propertySymbol.Type;

        ImmutableArray<AttributeData> attributeData = propertySymbol.GetAttributes();
        if (!attributeData.IsEmpty)
            AttributeData = attributeData;
    }

    public MyPropertySymbol(IFieldSymbol fieldSymbol)
        : this(fieldSymbol.Name, fieldSymbol.Type.SpecialType)
    {
        Type = fieldSymbol.Type;

        ImmutableArray<AttributeData> attributeData = fieldSymbol.GetAttributes();
        if (!attributeData.IsEmpty)
            AttributeData = attributeData;
    }

    public MyPropertySymbol(string name, SpecialType specialType)
    {
        Name = name;
        SpecialType = specialType;
    }

    public bool TryFindAttribute(string fullTypeName, out AttributeData? attributeData)
    {
        attributeData = null;
        return AttributeData?.TryFindAttribute(fullTypeName, out attributeData) is true;
    }
}
