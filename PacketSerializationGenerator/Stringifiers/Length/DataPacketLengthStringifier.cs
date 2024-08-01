using Microsoft.CodeAnalysis;
using PacketSerializationGenerator.Abstractions.Stringifiers;
using PacketSerializationGenerator.Extensions;
using PacketSerializationGenerator.Generators.DataPackets;
using PacketSerializationGenerator.Objects;

namespace PacketSerializationGenerator.Stringifiers.Length;

/// <summary>
/// Implements an <see cref="IPropertyLengthStringifier"/> that is capable
/// of generating logic to get the binary length of array type properties.
/// </summary>
public class DataPacketLengthStringifier : IPropertyLengthStringifier
{
    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
        => propertySymbol.Type.IsDataPacket();

    /// <inheritdoc />
    public string GetLengthString(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is null)
            return "// Invalid type (null)!";

        string result = string.Empty;

        if (propertySymbol.TryFindAttribute(DataPacketConstants.PayloadAttributeTypeName, out _))
            result += $"""
                       {Constants.DefaultSizeVariableName} += sizeof(uint); // Payload length variable
                               
                       """;

        if (propertySymbol.Type.NullableAnnotation is NullableAnnotation.Annotated)
        {
            result += $"""
                       {Constants.DefaultSizeVariableName} += {propertySymbol.Name}?.GetRequiredBufferSize(false) ?? 0;
                               
                       """;
        }
        else
        {
            result += $"""
                       {Constants.DefaultSizeVariableName} += {propertySymbol.Name}.GetRequiredBufferSize(false);
                               
                       """;
        }

        return result;
    }
}
