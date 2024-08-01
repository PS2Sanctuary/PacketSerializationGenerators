using Microsoft.CodeAnalysis;
using PacketSerializationGenerator.Extensions;
using PacketSerializationGenerator.Objects;
using System;
using static PacketSerializationGenerator.Constants;

namespace PacketSerializationGenerator.Generators.DataPackets;

public class WeaponDataPacketStrings : BaseDataPacketStrings
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

using Sanctuary.Core.Abstractions;
using Sanctuary.Zone.Abstractions;
using {DataPacketConstants.DeclarationsNamespace};
using System;

namespace {c.Namespace};

/// <summary>
/// Represents a <see cref=""{c.Name}""/> weapon data packet.
/// </summary>
public partial class {c.Name} : IWeaponPacket, IDataPacket<{c.Name}>
{{
    /// <inheritdoc />
    public uint GameTime {{ get; set; }}

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

        return {DefaultOffsetVariableName};
    }}
}}
";
    }
}
