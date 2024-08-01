using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Abstractions.Stringifiers;
using PacketSerializationGenerators.Generators.DataPackets;
using PacketSerializationGenerators.Objects;

namespace PacketSerializationGenerators.Stringifiers.Length;

/// <summary>
/// Implements an <see cref="IPropertyLengthStringifier"/> that is capable
/// of generating logic to get the binary length of string properties.
/// </summary>
public class StringLengthStringifier : IPropertyLengthStringifier
{
    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
        => propertySymbol.SpecialType is SpecialType.System_String;

    /// <inheritdoc />
    public string GetLengthString(MyPropertySymbol propertySymbol)
    {
        bool isNullTerminated = false;
        SpecialType? lengthPrefixType = SpecialType.System_UInt32;

        propertySymbol.TryFindAttribute
        (
            DataPacketConstants.StringSerializationAttributeTypeName,
            out AttributeData? attribute
        );

        if (attribute?.ConstructorArguments.Length > 0)
        {
            lengthPrefixType = attribute.ConstructorArguments[0].Value switch
            {
                1 => SpecialType.System_Byte,
                2 => SpecialType.System_UInt16,
                3 => SpecialType.System_UInt32,
                _ => null
            };
        }

        if (attribute?.ConstructorArguments.Length > 1)
            isNullTerminated = attribute.ConstructorArguments[1].Value is true;

        string result = $"{Constants.DefaultSizeVariableName} += {propertySymbol.Name}.Length";

        if (lengthPrefixType is not null)
            result += $" + sizeof({lengthPrefixType.ToString().Replace("System_", "")})";
        if (isNullTerminated)
            result += " + 1";

        return result + @";
        ";
    }
}
