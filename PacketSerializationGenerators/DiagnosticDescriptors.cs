using Microsoft.CodeAnalysis;

namespace PacketSerializationGenerators;

public class DiagnosticDescriptors
{
    public static DiagnosticDescriptor GetStringGenerationFailure(string description)
        => new
        (
            "SSG001",
            "String Generation Failure",
            description,
            "Generation",
            DiagnosticSeverity.Error,
            true
        );

    public static DiagnosticDescriptor DataPacketNoTypeConstant(string description)
        => new
        (
            "SSG002",
            "Expected Type Constant",
            description,
            "Generation",
            DiagnosticSeverity.Error,
            true
        );
}
