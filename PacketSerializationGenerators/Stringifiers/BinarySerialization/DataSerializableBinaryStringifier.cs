using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Extensions;
using PacketSerializationGenerators.Abstractions.Stringifiers;
using PacketSerializationGenerators.Generators.DataPackets;
using PacketSerializationGenerators.Objects;
using static PacketSerializationGenerators.Constants;

namespace PacketSerializationGenerators.Stringifiers.BinarySerialization;

public class DataSerializableBinaryStringifier : IPropertyBinarySerializationStringifier
{
    /// <summary>
    /// Gets or sets the name of the buffer variable to read/write the data to/from.
    /// </summary>
    public string BufferVariableName { get; set; }

    /// <summary>
    /// Gets or sets the name of the offset variable to increment.
    /// </summary>
    public string OffsetVariableName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataPacketBinarySerializationStringifier"/> class.
    /// </summary>
    /// <param name="bufferVariableName">The name of the buffer variable to read/write data to/from.</param>
    /// <param name="offsetVariableName">The name of the offset variable use to access the buffer.</param>
    public DataSerializableBinaryStringifier
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
    {
        if (propertySymbol.Type is null)
            return false;

        foreach (INamedTypeSymbol? @interface in propertySymbol.Type.AllInterfaces)
        {
            if (@interface.ToDisplayString().StartsWith(DataPacketConstants.IDataSerializableTypeName))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public string GetDeserializationString(MyPropertySymbol propertySymbol, string? tempVariableNameOverride = null)
    {
        if (propertySymbol.Type is null)
            return "// Invalid type (null)!";

        string tempPropName = tempVariableNameOverride ?? propertySymbol.Name.ToSafeLowerCamel();
        string typeName = propertySymbol.Type.ToString();

        string result = $"""
                         
                                 {typeName} {tempPropName} = {typeName}.Deserialize({BufferVariableName}, ref {OffsetVariableName});

                         """;

        return result;
    }

    /// <inheritdoc />
    public string GetSerializationString(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is null)
            return "// Invalid type (null)!";

        return $"""
                
                        {propertySymbol.Name}.Serialize({BufferVariableName}, ref {OffsetVariableName});

                """;
    }
}
