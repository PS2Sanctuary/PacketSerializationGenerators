using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace PacketSerializationGenerators.Extensions;

public static class ImmutableArrayOfAttributeDataExtensions
{
    public static bool TryFindAttribute
    (
        this ImmutableArray<AttributeData> attributes,
        string fullTypeName,
        out AttributeData attributeData
    )
    {
        attributeData = null!;

        foreach (AttributeData data in attributes)
        {
            if (data.AttributeClass?.ToDisplayString() == fullTypeName)
            {
                attributeData = data;
                return true;
            }
        }

        return false;
    }

    public static bool TryFindAttribute
    (
        this ImmutableArray<AttributeData>? attributes,
        string fullTypeName,
        out AttributeData attributeData
    )
    {
        attributeData = null!;

        if (attributes is null)
            return false;

        foreach (AttributeData data in attributes)
        {
            if (data.AttributeClass?.ToDisplayString() == fullTypeName)
            {
                attributeData = data;
                return true;
            }
        }

        return false;
    }
}
