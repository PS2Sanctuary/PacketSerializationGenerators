using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Generators.DataPackets;

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
            if (attr.AttributeClass?.BaseType?.ToDisplayString() is DataPacketConstants.BaseDataPacketAttributeTypeName)
                return true;
        }

        foreach (INamedTypeSymbol? @interface in typeSymbol.AllInterfaces)
        {
            if (@interface.ToDisplayString().StartsWith(DataPacketConstants.IDataPacketTypeName))
                return true;
        }

        return false;
    }
}
