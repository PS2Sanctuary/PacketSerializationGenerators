using PacketSerializationGenerators.Declarations.Binary;

namespace Sample;

public enum LoginOpCode
{
    ExamplePacket = 1,
    ExamplePayload = 2
}

[LoginDataPacket]
public partial class LoginPacketExample
{
    // The generator expects a constant variable called 'Type' to specify the OP code of the packet.
    // This is mandatory on every class annotated with [LoginDataPacket]
    public const LoginOpCode Type = LoginOpCode.ExamplePacket;

    // Enums are serialized to their underlying integer type
    public LoginOpCode ExampleEnumValue { get; set; }

    // Strings by default are serialized with a length prefix of size int32
    public string StringValue { get; set; }

    // You don't need to specify the PayloadAttribute to nest packets. But it handles a special case where
    // the payload is required to be prefixed with its length.
    [Payload]
    public LoginPacketExamplePayload? Payload { get; }
}

[LoginDataPacket]
public partial class LoginPacketExamplePayload
{
    public const LoginOpCode Type = LoginOpCode.ExamplePayload;

    public uint IntegerValue { get; set; }

    [StringSerialization(StringSerializationAttribute.LengthPrefixSize.Short)]
    public string StringValueWithOverridenPrefixLength { get; set; }

    [StringSerialization(StringSerializationAttribute.LengthPrefixSize.None, isNullTerminated: true)]
    public string NullTerminated { get; set; }
}
