using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Extensions;
using PacketSerializationGenerators.Abstractions;
using PacketSerializationGenerators.Abstractions.Stringifiers;
using PacketSerializationGenerators.Objects;
using static PacketSerializationGenerators.Constants;

namespace PacketSerializationGenerators.Stringifiers.BinarySerialization;

/// <summary>
/// Implements an <see cref="IPropertyBinarySerializationStringifier"/>
/// that is capable of generating logic to serialize integer values.
/// </summary>
public class IntegerBinarySerializationStringifier : IPropertyBinarySerializationStringifier
{
    /// <summary>
    /// Gets or sets the endianness to read/write the serialized binary data as.
    /// </summary>
    public PacketEndianness Endianness { get; set; }

    /// <summary>
    /// Gets or sets the name of the buffer variable to read/write the data to/from.
    /// </summary>
    public string BufferVariableName { get; set; }

    /// <summary>
    /// Gets or sets the name of the offset variable to increment.
    /// </summary>
    public string OffsetVariableName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerBinarySerializationStringifier"/> class.
    /// </summary>
    /// <param name="endianness">The endianness of the serialized data.</param>
    /// <param name="bufferVariableName">The name of the buffer variable to read/write data to/from.</param>
    /// <param name="offsetVariableName">The name of the offset variable use to access the buffer.</param>
    public IntegerBinarySerializationStringifier
    (
        PacketEndianness endianness,
        string bufferVariableName = DefaultBufferVariableName,
        string offsetVariableName = DefaultOffsetVariableName
    )
    {
        Endianness = endianness;
        BufferVariableName = bufferVariableName;
        OffsetVariableName = offsetVariableName;
    }

    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is INamedTypeSymbol { EnumUnderlyingType: not null })
            return true;

        return propertySymbol.SpecialType switch
        {
            SpecialType.System_Byte
                or SpecialType.System_Int16
                or SpecialType.System_Int32
                or SpecialType.System_Int64
                or SpecialType.System_UInt16
                or SpecialType.System_UInt32
                or SpecialType.System_UInt64
                or SpecialType.System_Single
                or SpecialType.System_Double
                => true,
            _ => false
        };
    }

    /// <inheritdoc />
    public string GetDeserializationString(MyPropertySymbol propertySymbol, string? tempVariableNameOverride = null)
    {
        string underlyingType = propertySymbol.Type is INamedTypeSymbol { EnumUnderlyingType: not null } enumType
            ? enumType.EnumUnderlyingType.SpecialType.ToString().Split('_')[1]
            : propertySymbol.SpecialType.ToString().Split('_')[1];

        string typeName = propertySymbol.Type?.ToDisplayString() ?? propertySymbol.SpecialType.ToString().Split('_')[1];
        string tempPropName = tempVariableNameOverride ?? propertySymbol.Name.ToSafeLowerCamel();

        return underlyingType == "Byte"
            ? $"""
                                           
                       {typeName} {tempPropName} = ({typeName}){BufferVariableName}[{OffsetVariableName}++];
               """
            : $"""
                                                  
                       {typeName} {tempPropName} = ({typeName})System.Buffers.Binary.BinaryPrimitives.Read{underlyingType}{Endianness}({BufferVariableName}[{OffsetVariableName}..]);
                       {OffsetVariableName} += sizeof({underlyingType});

               """;
    }

    /// <inheritdoc />
    public string GetSerializationString(MyPropertySymbol propertySymbol)
    {
        string underlyingType = propertySymbol.Type is INamedTypeSymbol { EnumUnderlyingType: { } } enumType
            ? enumType.EnumUnderlyingType.SpecialType.ToString().Split('_')[1]
            : propertySymbol.SpecialType.ToString().Split('_')[1];

        if (underlyingType == "Byte")
        {
            return $"""
                    
                            {BufferVariableName}[{OffsetVariableName}++] = ({underlyingType}){propertySymbol.Name};

                    """;
        }

        return $"""
                
                        System.Buffers.Binary.BinaryPrimitives.Write{underlyingType}{Endianness}({BufferVariableName}[{OffsetVariableName}..], ({underlyingType}){propertySymbol.Name});
                        {OffsetVariableName} += sizeof({underlyingType});

                """;
    }
}
