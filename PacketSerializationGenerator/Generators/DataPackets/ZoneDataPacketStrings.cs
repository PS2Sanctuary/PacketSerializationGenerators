using Microsoft.CodeAnalysis;
using PacketSerializationGenerator.Extensions;
using PacketSerializationGenerator.Objects;
using System;
using static PacketSerializationGenerator.Constants;

namespace PacketSerializationGenerator.Generators.DataPackets;

public class ZoneDataPacketStrings : BaseDataPacketStrings
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
using {DataPacketConstants.DeclarationsNamespace};
using Sanctuary.Zone.Util;
using System;

namespace {c.Namespace};

/// <summary>
/// Represents a <see cref=""{c.Name}""/> zone data packet.
/// </summary>
public partial class {c.Name} : IDataPacket<{c.Name}>
{{
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

    protected override void BeginFunctionStrings
    (
        ClassToAugment c,
        Action<Diagnostic> reportDiagnostic,
        IFieldSymbol opCodeSymbol,
        INamedTypeSymbol opCodeEnumSymbol,
        object opCodeValue,
        out string deserializeString,
        out string requiredBufferSizeString,
        out string serializeString
    )
    {
        uint code = (uint)opCodeValue;
        int opCodeSize = 0;
        while (code != 0)
        {
            code >>= 8;
            opCodeSize++;
        }

        deserializeString = $@"int {DefaultOffsetVariableName} = readOPCode ? {opCodeSize} : 0;
        ";
        requiredBufferSizeString = $@"includeOPCode ? {opCodeSize} : 0;
        ";
        serializeString = $@"int {DefaultOffsetVariableName} = writeOPCode
            ? ZonePacketUtils.WriteZoneOpCode({DefaultBufferVariableName}, {opCodeSymbol.Name})
            : 0;
        ";

        if (!c.Attributes.TryFindAttribute(DataPacketConstants.LZ4CompressedAttributeTypeName, out _))
            return;

        deserializeString += $@"
        {DefaultBufferVariableName} = {DefaultBufferVariableName}[{DefaultOffsetVariableName}..];
        int nameEndIndex = {DefaultBufferVariableName}.IndexOf((byte)0);
        {DefaultBufferVariableName} = {DefaultBufferVariableName}[(nameEndIndex + 1)..];

        uint decompressedLength = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian({DefaultBufferVariableName});
        uint payloadLength = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian({DefaultBufferVariableName}[4..]);
        {DefaultBufferVariableName} = {DefaultBufferVariableName}.Slice(8, (int)payloadLength);

        byte[] target = new byte[(int)decompressedLength];
        int written = K4os.Compression.LZ4.LZ4Codec.Decode({DefaultBufferVariableName}, target);
        {DefaultBufferVariableName} = target.AsSpan(0, written);
        {DefaultOffsetVariableName} = 0;
        ";
        // TODO: Serialization logic must come after. A post-function string may be required
    }
}
