using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Extensions;
using PacketSerializationGenerators.Abstractions.Stringifiers;
using PacketSerializationGenerators.Generators.BinaryPackets;
using PacketSerializationGenerators.Objects;
using System.Linq;

namespace PacketSerializationGenerators.Stringifiers.Length;

/// <summary>
/// Implements an <see cref="IPropertyLengthStringifier"/> that is capable
/// of generating logic to get the binary length of array type properties.
/// </summary>
public class ArrayLengthStringifier : IPropertyLengthStringifier
{
    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
        => propertySymbol.Type is IArrayTypeSymbol arrayType
            && (arrayType.ElementType.IsValueType || arrayType.ElementType.IsDataPacket());

    /// <inheritdoc />
    public string GetLengthString(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is not IArrayTypeSymbol arrayType)
            return "// Not an array type!";

        SpecialType lengthPrefixType = SpecialType.System_UInt32;

        propertySymbol.TryFindAttribute
        (
            BinaryPacketConstants.ArraySerializationMethodAttributeTypeName,
            out AttributeData? attribute
        );

        // Default to PrefixedLength
        int indexMethod = 0;
        if (attribute is not null && attribute.ConstructorArguments.Any(a => a.Type?.Name == "ArraySizeMethod"))
            indexMethod = (int)(attribute.ConstructorArguments.First(a => a.Type?.Name == "ArraySizeMethod").Value ?? 0);

        if (attribute is not null && attribute.ConstructorArguments.Any(a => a.Type?.Name == "LengthPrefixSize"))
        {
            lengthPrefixType = attribute.ConstructorArguments.First(a => a.Type?.Name == "LengthPrefixSize").Value switch
            {
                1 => SpecialType.System_Byte,
                2 => SpecialType.System_UInt16,
                4 => SpecialType.System_UInt64,
                _ => SpecialType.System_UInt32,
            };
        }

        string result = string.Empty;

        // TODO: We only support length-prefixed and unbounded arrays right now
        if (indexMethod == 0)
        {
            result += $"""
                       {Constants.DefaultSizeVariableName} += sizeof({lengthPrefixType.ToString().Replace("System_", "")}); // Length value for the following array
                               
                       """;
        }

        // else if (indexMethod == 3)...
        // We don't prefix with a length for Unbounded

        if (arrayType.ElementType.IsDataPacket())
        {
            return result + $"""
                             foreach (IDataPacket element in {propertySymbol.Name})
                                         {Constants.DefaultSizeVariableName} += element.GetRequiredBufferSize(false);
                                     
                             """;
        }

        return result + $"""
                         {Constants.DefaultSizeVariableName} += {propertySymbol.Name}.Length * sizeof({arrayType.ElementType.Name});
                                 
                         """;
    }
}
