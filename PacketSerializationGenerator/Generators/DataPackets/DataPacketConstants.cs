namespace PacketSerializationGenerator.Generators.DataPackets;

public static class DataPacketConstants
{
    public const string IDataPacketTypeName = "Sanctuary.Core.Abstractions.IDataPacket";
    public const string DeclarationsNamespace = "Sanctuary.SourceGeneration.Declarations.Data";
    public const string IDataSerializableTypeName = $"{DeclarationsNamespace}.IDataSerializable";
    public const string PayloadAttributeTypeName = $"{DeclarationsNamespace}.PayloadAttribute";
    public const string BaseDataPacketAttributeTypeName = $"{DeclarationsNamespace}.BaseDataPacketAttribute";
    public const string LoginDataPacketAttributeTypeName = $"{DeclarationsNamespace}.LoginDataPacketAttribute";
    public const string GatewayDataPacketAttributeTypeName = $"{DeclarationsNamespace}.GatewayDataPacketAttribute";
    public const string ZoneDataPacketAttributeTypeName = $"{DeclarationsNamespace}.ZoneDataPacketAttribute";
    public const string WeaponDataPacketAttributeTypeName = $"{DeclarationsNamespace}.WeaponDataPacketAttribute";
    public const string LZ4CompressedAttributeTypeName = $"{DeclarationsNamespace}.LZ4CompressedAttribute";
    public const string ArraySerializationMethodAttributeTypeName = $"{DeclarationsNamespace}.ArraySerializationMethodAttribute";
    public const string StringSerializationAttributeTypeName = $"{DeclarationsNamespace}.StringSerializationAttribute";
}
