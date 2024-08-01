using Microsoft.CodeAnalysis;
using PacketSerializationGenerator.Abstractions.Stringifiers;
using PacketSerializationGenerator.Extensions;
using PacketSerializationGenerator.Objects;
using static PacketSerializationGenerator.Constants;

namespace PacketSerializationGenerator.Stringifiers.BinarySerialization;

/// <summary>
/// Implements an <see cref="IPropertyBinarySerializationStringifier"/>
/// that is capable of generating logic to serialize boolean values.
/// </summary>
public class BooleanBinarySerializationStringifier : IPropertyBinarySerializationStringifier
{
    /// <summary>
    /// Gets or sets the name of the buffer variable to read/write the data to/from.
    /// </summary>
    public string BufferVariableName { get; set; }

    /// <summary>
    /// Gets or sets the name of the offset variable used to access the buffer.
    /// </summary>
    public string OffsetVariableName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanBinarySerializationStringifier"/> class.
    /// </summary>
    /// <param name="bufferVariableName">The name of the buffer variable to read/write data to/from.</param>
    /// <param name="offsetVariableName">The name of the offset variable use to access the buffer.</param>
    public BooleanBinarySerializationStringifier
    (
        string bufferVariableName = DefaultBufferVariableName,
        string offsetVariableName = DefaultOffsetVariableName
    )
    {
        BufferVariableName = bufferVariableName;
        OffsetVariableName = offsetVariableName;
    }

    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
        => propertySymbol.SpecialType is SpecialType.System_Boolean;

    /// <inheritdoc />
    public string GetDeserializationString(MyPropertySymbol propertySymbol, string? tempVariableNameOverride = null)
    {
        string tempPropName = tempVariableNameOverride ?? propertySymbol.Name.ToSafeLowerCamel();

        return $"""
                
                        bool {tempPropName} = {BufferVariableName}[{OffsetVariableName}++] > 0;

                """;
    }

    /// <inheritdoc />
    public string GetSerializationString(MyPropertySymbol propertySymbol)
        => $"""
            
                    {BufferVariableName}[{OffsetVariableName}++] = (byte)({propertySymbol.Name} ? 1 : 0);

            """;
}
