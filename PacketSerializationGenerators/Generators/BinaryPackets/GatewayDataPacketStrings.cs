using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Extensions;
using PacketSerializationGenerators.Objects;
using System;
using static PacketSerializationGenerators.Constants;

namespace PacketSerializationGenerators.Generators.BinaryPackets;

public class GatewayDataPacketStrings : BaseDataPacketStrings
{
    public override string GeneratePacketString(ClassToAugment c, Action<Diagnostic> reportDiagnostic)
    {
        GenerateFunctionStrings
        (
            c,
            reportDiagnostic,
            out string deserializeString,
            out string requiredBufferSizeString,
            out string serializeString
        );

        return $@"#nullable enable

using {BinaryPacketConstants.DeclarationsNamespace};
using System;

namespace {c.Namespace};

/// <summary>
/// Represents a <see cref=""{c.Name}""/> gateway data packet.
/// </summary>
public partial class {c.Name} : IDataPacket<{c.Name}>
{{
    public byte Channel {{ get; init; }}

    /// <summary>
    /// Initializes a new instance of the <see cref=""{c.Name}""/> class.
    /// </summary>
    {GenerateConstructorString(c)}

    /// <inheritdoc />
    public static {c.Name} Deserialize(ReadOnlySpan<byte> {DefaultBufferVariableName}, out int amountRead, bool readOPCode = true)
    {{
        {deserializeString.CleanGeneratorString()}
    }}

    /// <inheritdoc />
    public unsafe int GetRequiredBufferSize(bool includeOPCode = true)
    {{
        int {DefaultSizeVariableName} = {requiredBufferSizeString.CleanGeneratorString()}
        return {DefaultSizeVariableName};
    }}

    /// <inheritdoc />
    public int Serialize(Span<byte> {DefaultBufferVariableName}, bool writeOPCode = true)
    {{
        {serializeString.CleanGeneratorString()}

        {DefaultBufferVariableName}[0] |= (byte)(Channel << 5);
        return {DefaultOffsetVariableName};
    }}
}}
";
    }

    protected override string FinaliseDeserializeString(string input, ClassToAugment c)
    {
        string ctorParamAssignments = string.Empty;

        foreach (IPropertySymbol prop in c.Properties)
        {
            ctorParamAssignments += $@"{prop.Name.ToSafeLowerCamel()},
            ";
        }

        input += $@"
        amountRead = {DefaultOffsetVariableName};
        return new {c.Name}
        (
            {ctorParamAssignments.CleanGeneratorString()}
        )
        {{
            Channel = (byte)({DefaultBufferVariableName}[0] >> 5)
        }};";

        return input;
    }
}
