using PacketSerializationGenerators.Objects;

namespace PacketSerializationGenerators.Abstractions.Stringifiers;

/// <summary>
/// Represents an interface for generating logic to de/serialize the binary length of a property.
/// </summary>
public interface IPropertyLengthStringifier : IPropertyStringifier
{
    /// <summary>
    /// Generates the logic required to get the binary length of the given <see cref="MyPropertySymbol"/>.
    /// </summary>
    /// <param name="propertySymbol">The property symbol.</param>
    /// <returns>The generated logic.</returns>
    string GetLengthString(MyPropertySymbol propertySymbol);
}
