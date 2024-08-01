using System;

namespace PacketSerializationGenerators.Declarations.Binary;

/// <summary>
/// Controls how the decorated array property is de/serialized to binary.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ArraySerializationMethodAttribute : Attribute
{
    /// <summary>
    /// Defines the method by which an array will be sized.
    /// </summary>
    public enum ArraySizeMethod
    {
        /// <summary>
        /// The array will be prefixed with an integer value indicating its length.
        /// </summary>
        PrefixedLength = 0,

        /// <summary>
        /// The length of the array is constant.
        /// </summary>
        FixedLength = 1,

        /// <summary>
        /// The array will always extend to a certain position in the data.
        /// </summary>
        FixedUpperBound = 2,

        /// <summary>
        /// The array extends to the end of the data.
        /// </summary>
        Unbounded = 3
    }

    /// <summary>
    /// Indicates the size of a string length prefix field.
    /// </summary>
    public enum LengthPrefixSize
    {
        None = 0,
        Byte = 1,
        Short = 2,
        Int = 3,
        Long = 4
    }

    /// <summary>
    /// Gets the array sizing method.
    /// </summary>
    public ArraySizeMethod Method { get; }

    /// <summary>
    /// Gets the constant length of the array when using <see cref="ArraySizeMethod.FixedLength"/>,
    /// or the upper bound of the array when using <see cref="ArraySizeMethod.FixedUpperBound"/>.
    /// </summary>
    public int LengthValue { get; }

    /// <summary>
    /// Gets the size of the length prefix field when using <see cref="ArraySizeMethod.PrefixedLength"/>.
    /// </summary>
    public LengthPrefixSize PrefixSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArraySerializationMethodAttribute"/>.
    /// </summary>
    /// <param name="method">The array sizing method.</param>
    /// <param name="prefixSize">
    /// The size of the length prefix field when using <see cref="ArraySizeMethod.PrefixedLength"/>.
    /// </param>
    /// <param name="lengthValue">
    /// The constant length of the array when using <see cref="ArraySizeMethod.FixedLength"/>,
    /// or the upper bound of the array when using <see cref="ArraySizeMethod.FixedUpperBound"/>.
    /// </param>
    public ArraySerializationMethodAttribute
    (
        ArraySizeMethod method = ArraySizeMethod.PrefixedLength,
        int lengthValue = 0,
        LengthPrefixSize prefixSize = LengthPrefixSize.Int
    )
    {
        Method = method;
        LengthValue = lengthValue;
        PrefixSize = prefixSize;
    }
}
