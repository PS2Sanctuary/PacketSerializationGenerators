using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Generators.BinaryPackets;

// ReSharper disable once CheckNamespace
namespace PacketSerializationGenerators.Extensions;

public static class ITypeSymbolExtensions
{
    public static bool IsDataPacket(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
            return false;

        foreach (AttributeData attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.BaseType?.ToDisplayString() is BinaryPacketConstants.BaseDataPacketAttributeTypeName)
                return true;
        }

        foreach (INamedTypeSymbol? @interface in typeSymbol.AllInterfaces)
        {
            if (@interface.ToDisplayString().StartsWith(BinaryPacketConstants.IDataPacketTypeName))
                return true;
        }

        return false;
    }
}
