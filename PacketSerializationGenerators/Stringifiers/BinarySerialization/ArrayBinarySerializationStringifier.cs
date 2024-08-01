using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Extensions;
using PacketSerializationGenerators.Abstractions.Stringifiers;
using PacketSerializationGenerators.Generators.DataPackets;
using PacketSerializationGenerators.Objects;
using System;
using System.Linq;
using static PacketSerializationGenerators.Constants;

namespace PacketSerializationGenerators.Stringifiers.BinarySerialization;

/// <summary>
/// Implements an <see cref="IPropertyBinarySerializationStringifier"/>
/// that is capable of generating logic to serialize array values.
/// </summary>
public class ArrayBinarySerializationStringifier : IPropertyBinarySerializationStringifier
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
    /// Initializes a new instance of the <see cref="ArrayBinarySerializationStringifier"/> class.
    /// </summary>
    /// <param name="integerBinarySerializer">A binary serializer that is capable of serializing integer values.</param>
    /// <param name="bufferVariableName">The name of the buffer variable to read/write data to/from.</param>
    /// <param name="offsetVariableName">The name of the offset variable use to access the buffer.</param>
    public ArrayBinarySerializationStringifier
    (
        IPropertyBinarySerializationStringifier integerBinarySerializer,
        string bufferVariableName = DefaultBufferVariableName,
        string offsetVariableName = DefaultOffsetVariableName
    )
    {
        MyPropertySymbol uint64Symbol = new("", SpecialType.System_UInt64);
        MyPropertySymbol uint32Symbol = new("", SpecialType.System_UInt32);
        MyPropertySymbol uint16Symbol = new("", SpecialType.System_UInt16);
        MyPropertySymbol byteSymbol = new("", SpecialType.System_Byte);

        bool canStringifyPrefixValues = integerBinarySerializer.CanStringify(uint64Symbol)
            && integerBinarySerializer.CanStringify(uint32Symbol)
            && integerBinarySerializer.CanStringify(uint16Symbol)
            && integerBinarySerializer.CanStringify(byteSymbol);
        if (!canStringifyPrefixValues)
        {
            throw new ArgumentException
            (
                "The provided integerBinarySerializer must be able to serialize ulong, uint, ushort and byte values",
                nameof(integerBinarySerializer)
            );
        }

        _integerBinarySerializer = integerBinarySerializer;
        BufferVariableName = bufferVariableName;
        OffsetVariableName = offsetVariableName;
    }

    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is not IArrayTypeSymbol arrayType)
            return false;

        return arrayType.ElementType.IsDataPacket()
            || arrayType.ElementType.SpecialType is SpecialType.System_Byte
            || _integerBinarySerializer.CanStringify(new MyPropertySymbol(string.Empty, arrayType.ElementType.SpecialType));
    }

    /// <inheritdoc />
    public string GetDeserializationString(MyPropertySymbol propertySymbol, string? tempVariableNameOverride = null)
    {
        if (propertySymbol.Type is not IArrayTypeSymbol arrayType)
            return "// Not an array symbol!";

        string tempPropName = tempVariableNameOverride ?? propertySymbol.Name.ToSafeLowerCamel();
        string lengthName = tempPropName + "Length";

        // Default to PrefixedLength
        SpecialType lengthPrefixType = SpecialType.System_UInt32;
        int indexMethod = 0;
        int lengthValue = 0;

        propertySymbol.TryFindAttribute
        (
            DataPacketConstants.ArraySerializationMethodAttributeTypeName,
            out AttributeData? attribute
        );

        if (attribute?.ConstructorArguments.Length > 0)
        {
            indexMethod = (int?)attribute.ConstructorArguments[0].Value ?? 0;

            if (attribute.ConstructorArguments.Length > 1)
                lengthValue = (int)(attribute.ConstructorArguments[1].Value ?? 0);
        }

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

        if (arrayType.ElementType.IsDataPacket())
            return GetDataPacketDeserializationString(tempPropName, lengthName, lengthPrefixType, arrayType.ElementType);

        if (arrayType.ElementType.SpecialType is SpecialType.System_Byte)
            return GetByteDeserializationString(indexMethod, tempPropName, lengthName, lengthValue, lengthPrefixType);

        MyPropertySymbol elementTypeSymbol = new(string.Empty, arrayType.ElementType.SpecialType);
        if (_integerBinarySerializer.CanStringify(elementTypeSymbol))
        {
            return GetIntegerDeserializationString
            (
                elementTypeSymbol.SpecialType,
                indexMethod,
                tempPropName,
                lengthName,
                lengthPrefixType,
                lengthValue
            );
        }

        return "// Invalid array type!";
    }

    private string GetByteDeserializationString
    (
        int indexMethod,
        string tempPropName,
        string lengthName,
        int lengthValue,
        SpecialType lengthPrefixType
    )
    {
        string result = string.Empty;
        string indexer = BufferVariableName;

        switch (indexMethod)
        {
            case 0: // PrefixedLength
                MyPropertySymbol lengthVariableSymbol = new(lengthName.Trim('@'), lengthPrefixType);
                result += _integerBinarySerializer.GetDeserializationString(lengthVariableSymbol);
                indexer += $".Slice({OffsetVariableName}, (int){lengthName})";
                break;
            case 1: // FixedLength
                indexer += $".Slice({OffsetVariableName}, (int){lengthValue})";
                break;
            case 2: // FixedUpperBound
                indexer += $"[{OffsetVariableName}..(int){lengthValue}]";
                break;
            case 3: // Unbounded
                indexer += $"[{OffsetVariableName}..]";
                break;
        }

        return result + $"""
                         
                                 byte[] {tempPropName} = {indexer}.ToArray();
                                 {OffsetVariableName} += {tempPropName}.Length;

                         """;
    }

    private string GetDataPacketDeserializationString
    (
        string tempPropName,
        string lengthName,
        SpecialType lengthPrefixType,
        ITypeSymbol packetType
    )
    {
        string result = string.Empty;

        MyPropertySymbol lengthVariableSymbol = new(lengthName.Trim('@'), lengthPrefixType);
        result += _integerBinarySerializer.GetDeserializationString(lengthVariableSymbol);

        result += $"""
                   
                           {packetType}[] {tempPropName} = new {packetType}[{lengthName}];

                   """;

        result += $$"""
                    
                            for ({{lengthPrefixType.ToString().Replace("System_", string.Empty)}} i = 0; i < {{lengthName}}; i++)
                            {
                                {{tempPropName}}[i] = {{packetType}}.Deserialize({{BufferVariableName}}[{{OffsetVariableName}}..], out int {{tempPropName}}AmountRead, false);
                                {{OffsetVariableName}} += {{tempPropName}}AmountRead;
                            }

                    """;

        return result;
    }

    // This method only supports length-prefixed arrays!
    private string GetIntegerDeserializationString
    (
        SpecialType underlyingType,
        int indexMethod,
        string tempPropName,
        string lengthName,
        SpecialType lengthPrefixType,
        int lengthValue
    )
    {
        string result = string.Empty;
        string arrayTypeName = underlyingType.ToString().Split('_')[1];
        string tempStorageVarName = tempPropName + "Value";

        MyPropertySymbol lengthVariableSymbol = new(lengthName.Trim('@'), lengthPrefixType);
        result += _integerBinarySerializer.GetDeserializationString(lengthVariableSymbol);

        MyPropertySymbol tempStorageVarSymbol = new(tempStorageVarName.Trim('@'), underlyingType);

        result += $"""
                   
                           {arrayTypeName}[] {tempPropName} = new {arrayTypeName}[{lengthName}];

                   """;

        result += $$"""
                    
                            for ({{lengthPrefixType.ToString().Replace("System_", string.Empty)}} i = 0; i < {{lengthName}}; i++)
                            {
                                {{_integerBinarySerializer.GetDeserializationString(tempStorageVarSymbol)}}
                                {{tempPropName}}[i] = {{tempStorageVarName}};
                            }

                    """;

        return result;
    }

    /// <inheritdoc />
    public string GetSerializationString(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is not IArrayTypeSymbol arrayType)
            return "// Not an array symbol!";

        // Default to PrefixedLength
        int indexMethod = 0;
        SpecialType lengthPrefixType = SpecialType.System_UInt32;
        string result = string.Empty;

        propertySymbol.TryFindAttribute
        (
            DataPacketConstants.ArraySerializationMethodAttributeTypeName,
            out AttributeData? attribute
        );

        if (attribute?.ConstructorArguments.Length > 0)
            indexMethod = (int)(attribute.ConstructorArguments[0].Value ?? 0);

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

        switch (indexMethod)
        {
            case 0: // PrefixedLength
                MyPropertySymbol lengthSymbol = new($"(uint){propertySymbol.Name}.Length", lengthPrefixType);
                result += _integerBinarySerializer.GetSerializationString(lengthSymbol);
                break;
        }

        if (arrayType.ElementType.IsDataPacket())
        {
            return result + $$"""
                              
                                      foreach ({{arrayType.ElementType}} element in {{propertySymbol.Name}})
                                      {
                                          int elementSize = element.Serialize({{BufferVariableName}}[{{OffsetVariableName}}..], false);
                                          {{OffsetVariableName}} += elementSize;
                                      }

                              """;
        }

        if (arrayType.ElementType.SpecialType is SpecialType.System_Byte)
        {
            return result + $"""
                             
                                     {propertySymbol.Name}.CopyTo({BufferVariableName}[{OffsetVariableName}..]);
                                     {OffsetVariableName} += {propertySymbol.Name}.Length;

                             """;
        }

        // We must be an integer type array
        MyPropertySymbol tempStorageVarSymbol = new("element", arrayType.ElementType.SpecialType);

        return result + $$"""
                          
                                  foreach ({{arrayType.ElementType}} element in {{propertySymbol.Name}})
                                  {
                                      {{_integerBinarySerializer.GetSerializationString(tempStorageVarSymbol)}}
                                  }

                          """;
    }
}
