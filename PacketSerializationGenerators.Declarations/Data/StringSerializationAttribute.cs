using System;

namespace PacketSerializationGenerators.Declarations.Data;

/// <summary>
/// Indicates how the decorated string property should be serialized.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class StringSerializationAttribute : Attribute
{
    /// <summary>
    /// Indicates the size of a string length prefix field.
    /// </summary>
    public enum LengthPrefixSize
    {
        None = 0,
        Byte = 1,
        Short = 2,
        Int = 3
    }

    /// <summary>
    /// Gets the size of the length prefix field.
    /// </summary>
    public LengthPrefixSize PrefixSize { get; }

    /// <summary>
    /// Indicates that the provided string is null-terminated.
    /// </summary>
    /// <remarks>
    /// Setting this property to true does not ignore the value of the
    /// <see cref="LengthPrefixSize"/> field. Those bytes will still be read,
    /// so you must explicitly set it to <see cref="LengthPrefixSize.None"/>
    /// if desired.
    /// </remarks>
    public bool IsNullTerminated { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringSerializationAttribute"/>
    /// </summary>
    /// <param name="prefixSize">The size of the string-length prefix field.</param>
    /// <param name="isNullTerminated">Whether the string is null-terminated or length-prefixed.</param>
    public StringSerializationAttribute
    (
        LengthPrefixSize prefixSize = LengthPrefixSize.Int,
        bool isNullTerminated = false
    )
    {
        PrefixSize = prefixSize;
        IsNullTerminated = isNullTerminated;
    }
}
