namespace PacketSerializationGenerators.Generators.BinaryPackets;

public static class BinaryPacketConstants
{
    public const string IDataPacketTypeName = "PacketSerializationGenerators.Declarations.Binary.IDataPacket";
    public const string DeclarationsNamespace = "PacketSerializationGenerators.Declarations.Binary";
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
