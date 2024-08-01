using Microsoft.CodeAnalysis;
using PacketSerializationGenerator.Abstractions.Stringifiers;
using PacketSerializationGenerator.Extensions;
using PacketSerializationGenerator.Generators.DataPackets;
using PacketSerializationGenerator.Objects;
using System;
using static PacketSerializationGenerator.Constants;

namespace PacketSerializationGenerator.Stringifiers.BinarySerialization;

/// <summary>
/// Implements an <see cref="IPropertyBinarySerializationStringifier"/>
/// that is capable of generating logic to serialize string values.
/// </summary>
public class StringBinarySerializationStringifier : IPropertyBinarySerializationStringifier
{
    private readonly IPropertyBinarySerializationStringifier _integerBinarySerializer;

    /// <summary>
    /// Gets or sets the name of the buffer variable to read/write the data to/from.
    /// </summary>
    public string BufferVariableName { get; set; }

    /// <summary>
    /// Gets or sets the name of the offset variable to increment.
    /// </summary>
    public string OffsetVariableName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringBinarySerializationStringifier"/> class.
    /// </summary>
    /// <param name="integerBinarySerializer">A binary serializer that is capable of serializing integer values.</param>
    /// <param name="bufferVariableName">The name of the buffer variable to read/write data to/from.</param>
    /// <param name="offsetVariableName">The name of the offset variable use to access the buffer.</param>
    public StringBinarySerializationStringifier
    (
        IPropertyBinarySerializationStringifier integerBinarySerializer,
        string bufferVariableName = DefaultBufferVariableName,
        string offsetVariableName = DefaultOffsetVariableName
    )
    {
        MyPropertySymbol uint32Symbol = new("", SpecialType.System_UInt32);
        MyPropertySymbol uint16Symbol = new("", SpecialType.System_UInt16);
        MyPropertySymbol byteSymbol = new("", SpecialType.System_Byte);
        bool canStringifyPrefixValues = integerBinarySerializer.CanStringify(uint32Symbol)
                                        && integerBinarySerializer.CanStringify(uint16Symbol)
                                        && integerBinarySerializer.CanStringify(byteSymbol);
        if (!canStringifyPrefixValues)
        {
            throw new ArgumentException
            (
                "The provided integerBinarySerializer must be able to " +
                "serialize uint, ushort and byte values",
                nameof(integerBinarySerializer)
            );
        }

        _integerBinarySerializer = integerBinarySerializer;
        BufferVariableName = bufferVariableName;
        OffsetVariableName = offsetVariableName;
    }

    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
        => propertySymbol.SpecialType is SpecialType.System_String;

    /// <inheritdoc />
    public string GetDeserializationString(MyPropertySymbol propertySymbol, string? tempVariableNameOverride = null)
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

        string tempPropName = tempVariableNameOverride ?? propertySymbol.Name.ToSafeLowerCamel();
        string lengthName = tempPropName + "Length";

        string result = string.Empty;
        if (lengthPrefixType is not null)
        {
            MyPropertySymbol lengthVariableSymbol = new(lengthName, lengthPrefixType.Value);
            result += _integerBinarySerializer.GetDeserializationString(lengthVariableSymbol, lengthName);
        }
        else if (isNullTerminated)
        {
            lengthName += "NT";
            result += $"""
                       
                               int {lengthName} = {DefaultBufferVariableName}[{DefaultOffsetVariableName}..].IndexOf((byte)0);
                       """;
        }
        else
        {
            result += "// Must use either null-termination or non-zero prefix length";
        }

        result += $"""
                   
                           string {tempPropName} = System.Text.Encoding.ASCII.GetString({BufferVariableName}.Slice({OffsetVariableName}, (int){lengthName}));
                           {OffsetVariableName} += {tempPropName}.Length{(isNullTerminated ? " + 1;" : ";")}

                   """;

        return result;
    }

    /// <inheritdoc />
    public string GetSerializationString(MyPropertySymbol propertySymbol)
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

        string result = string.Empty;
        if (lengthPrefixType is not null)
        {
            MyPropertySymbol lengthSymbol = new($"(uint){propertySymbol.Name}.Length", lengthPrefixType.Value);
            result += _integerBinarySerializer.GetSerializationString(lengthSymbol);
        }
        else if (!isNullTerminated)
        {
            result += "// Must use either null-termination or non-zero prefix length";
        }

        result += $"""
                   
                           System.Text.Encoding.ASCII.GetBytes({propertySymbol.Name}).CopyTo({BufferVariableName}[{OffsetVariableName}..]);
                           {OffsetVariableName} += {propertySymbol.Name}.Length;

                   """;

        if (isNullTerminated)
        {
            result += $"""
                       {BufferVariableName}[{OffsetVariableName}++] = 0;

                       """;
        }

        return result;
    }
}
