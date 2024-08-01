using PacketSerializationGenerator.Abstractions.Stringifiers;
using PacketSerializationGenerator.Objects;

namespace PacketSerializationGenerator.Stringifiers.Length;

/// <summary>
/// Implements an <see cref="IPropertyLengthStringifier"/> that is capable
/// of generating logic to get the binary length of value type properties.
/// </summary>
public class ValueTypesLengthStringifier : IPropertyLengthStringifier
{
    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
        => propertySymbol.Type?.IsValueType is true;

    /// <inheritdoc />
    public string GetLengthString(MyPropertySymbol propertySymbol)
        => $"""
            {Constants.DefaultSizeVariableName} += sizeof({propertySymbol.Type!.ToDisplayString()});
                    
            """;
}
