using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Abstractions.Stringifiers;
using PacketSerializationGenerators.Generators.BinaryPackets;
using PacketSerializationGenerators.Objects;

namespace PacketSerializationGenerators.Stringifiers.Length;

public class IDataSerializableLengthStringifier : IPropertyLengthStringifier
{
    /// <inheritdoc />
    public bool CanStringify(MyPropertySymbol propertySymbol)
    {
        if (propertySymbol.Type is null)
            return false;

        foreach (INamedTypeSymbol? @interface in propertySymbol.Type.AllInterfaces)
        {
            if (@interface.ToDisplayString().StartsWith(BinaryPacketConstants.IDataSerializableTypeName))
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
