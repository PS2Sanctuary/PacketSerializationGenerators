using Microsoft.CodeAnalysis;
using PacketSerializationGenerator.Abstractions.Stringifiers;
using PacketSerializationGenerator.Generators.DataPackets;
using PacketSerializationGenerator.Objects;

namespace PacketSerializationGenerator.Stringifiers.Length;

public class IDataSerializableLengthStringifier : IPropertyLengthStringifier
{
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
    public string GetLengthString(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is null)
            return "// Invalid type (null)!";

        return $"""
                {Constants.DefaultSizeVariableName} += {propertySymbol.Name}.GetRequiredBufferSize();
                        
                """;
    }
}
