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
/// that is capable of generating logic to serialize data packets packets.
/// </summary>
public class DataPacketBinarySerializationStringifier : IPropertyBinarySerializationStringifier
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
    /// Initializes a new instance of the <see cref="DataPacketBinarySerializationStringifier"/> class.
    /// </summary>
    /// <param name="integerBinarySerializer">A binary serializer that is capable of serializing integer values.</param>
    /// <param name="bufferVariableName">The name of the buffer variable to read/write data to/from.</param>
    /// <param name="offsetVariableName">The name of the offset variable use to access the buffer.</param>
    public DataPacketBinarySerializationStringifier
    (
        IPropertyBinarySerializationStringifier integerBinarySerializer,
        string bufferVariableName = DefaultBufferVariableName,
        string offsetVariableName = DefaultOffsetVariableName
    )
    {
        MyPropertySymbol uint32Symbol = new("", SpecialType.System_UInt32);
        if (!integerBinarySerializer.CanStringify(uint32Symbol))
        {
            throw new ArgumentException
            (
                "The provided IPropertyBinarySerializationStringifier cannot serialize UInt32 variables",
                nameof(integerBinarySerializer)
            );
        }

        _integerBinarySerializer = integerBinarySerializer;
        BufferVariableName = bufferVariableName;
        OffsetVariableName = offsetVariableName;
    }

    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
        => propertySymbol.Type.IsDataPacket();

    /// <inheritdoc />
    public string GetDeserializationString(MyPropertySymbol propertySymbol, string? tempVariableNameOverride = null)
    {
        if (propertySymbol.Type is null)
            return "// Invalid type (null)!";

        string tempPropName = tempVariableNameOverride ?? propertySymbol.Name.ToSafeLowerCamel();
        string typeName = propertySymbol.Type.ToString().TrimEnd('?'); // Remove nullable annotations

        string result = @"
        ";

        if (propertySymbol.TryFindAttribute(DataPacketConstants.PayloadAttributeTypeName, out _))
        {
            string payloadLengthName = tempPropName.Trim('@') + "PayloadLength";
            MyPropertySymbol lengthSymbol = new(payloadLengthName, SpecialType.System_UInt32);
            result += _integerBinarySerializer.GetDeserializationString(lengthSymbol);
            result += $$"""

                                {{typeName}}? {{tempPropName}};
                                if ({{payloadLengthName}} is 0)
                                {
                                    {{tempPropName}} = null;
                                }
                                else
                                {
                                    {{tempPropName}} = {{typeName}}.Deserialize({{BufferVariableName}}[{{OffsetVariableName}}..], out int {{tempPropName}}AmountRead, false);
                                    {{OffsetVariableName}} += {{tempPropName}}AmountRead;
                                }

                        """;
        }
        else
        {
            result += $"""
                       {typeName}? {tempPropName} = {typeName}.Deserialize({BufferVariableName}[{OffsetVariableName}..], out int {tempPropName}AmountRead, false);
                               {OffsetVariableName} += {tempPropName}AmountRead;

                       """;
        }

        return result;
    }

    /// <inheritdoc />
    public string GetSerializationString(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is null)
            return "// Invalid type (null)!";

        string result = string.Empty;

        bool hasPayloadAttribute = propertySymbol.TryFindAttribute(DataPacketConstants.PayloadAttributeTypeName, out _);

        if (!hasPayloadAttribute)
        {
            result += $$"""
                        
                                if ({{propertySymbol.Name}} is null)
                                {
                                    throw new ArgumentNullException("{{propertySymbol.Name}}", "Non-payload nested packets may not be null");
                                }
                        """;
        }
        else
        {
            result += $$"""
                        
                                if ({{propertySymbol.Name}} is null)
                                {
                        """;
            MyPropertySymbol lengthSymbol = new("0", SpecialType.System_UInt32);
            result += _integerBinarySerializer.GetSerializationString(lengthSymbol);
            result += "}";
        }

        result += """
                  
                          else {
                  """;
        // Reserve space for the payload length
        if (hasPayloadAttribute)
        {
            result += $"""
                       
                                   // Reserve payload length space
                                   {OffsetVariableName} += sizeof(uint);
                       """;
        }

        result += $"""
                   
                               int {propertySymbol.Name}Written = {propertySymbol.Name}.Serialize({BufferVariableName}[{OffsetVariableName}..], false);

                   """;

        // Write the payload length
        if (propertySymbol.TryFindAttribute(DataPacketConstants.PayloadAttributeTypeName, out _))
        {
            result += $"""
                       
                                   {OffsetVariableName} -= sizeof(uint);
                       """;
            MyPropertySymbol lengthSymbol = new($"{propertySymbol.Name}Written", SpecialType.System_UInt32);
            result += _integerBinarySerializer.GetSerializationString(lengthSymbol);
        }

        result += $$"""
                    
                                {{OffsetVariableName}} += {{propertySymbol.Name}}Written;
                            }

                    """;

        return result;
    }
}
