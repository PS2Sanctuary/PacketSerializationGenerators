using Microsoft.CodeAnalysis;
using PacketSerializationGenerators.Extensions;
using PacketSerializationGenerators.Objects;
using System.Text;

namespace PacketSerializationGenerators.Generators.XmlObjects;

#pragma warning disable RCS1197 // Optimize StringBuilder.Append/AppendLine call.
public static class XmlSerializationStrings
{
    public static string GenerateXmlSerializationLogic(ClassToAugment c)
        => $@"#nullable enable
using {XmlObjectConstants.DeclarationsNamespace};
using System;
using System.Buffers;
using System.Reflection;
using System.Text;

namespace {c.Namespace};

public partial class {c.Name}
{{
    /// <summary>
    /// Initializes a new instance of the <see cref=""{c.Name}""/> class.
    /// </summary>
    {GenerateConstructorString(c)}

    /// <summary>
    /// Deserializes an <see cref=""{c.Name}""/> object from an XML string.
    /// </summary>
    /// <param name=""value"">The XML string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    public static {c.Name}? Deserialize(string value)
    {{
        if (string.IsNullOrEmpty(value))
            return null;

        ReadOnlySequence<char> dataSequence = new(value.AsMemory());
        SequenceReader<char> reader = new(dataSequence);

        return Deserialize(ref reader);
    }}

    /// <summary>
    /// Deserializes an <see cref=""{c.Name}""/> object from an XML string.
    /// </summary>
    /// <param name=""reader"">A <see cref=""SequenceReader{{T}}""/> configured to read from an XML data source.</param>
    /// <returns>The deserialized object.</returns>
    public static {c.Name}? Deserialize(ref SequenceReader<char> reader)
    {{
        if (reader.End)
            return null;

        {GenerateDeserializeString(c).CleanGeneratorString()}
    }}

    /// <summary>
    /// Serializes this <see cref=""{c.Name}""/> instance to an XML string.
    /// </summary>
    /// <returns>The XML string.</returns>
    public string Serialize()
    {{
        StringBuilder sb = new();
        Serialize(sb);

        return sb.ToString();
    }}

    /// <summary>
    /// Serializes this <see cref=""{c.Name}""/> instance to an XML string.
    /// </summary>
    /// <param name=""sb"">The <see cref=""StringBuilder""/> to write the XML string to.</param>
    /// <returns>The XML string.</returns>
    public void Serialize(StringBuilder sb)
    {{
        {GenerateSerializeString(c).CleanGeneratorString()}
    }}

    /// <inheritdoc />
    public override string ToString()
        => Serialize();
}}
";

    private static string GenerateDeserializeString(ClassToAugment c)
    {
        string className = GetPreferredName(c);

        // Advance to the start of the first property
        // The +2 comes from the surrounding '<' and ' ' chars.
        StringBuilder sb = new($@"reader.Advance({className.Length + 1});
        ");
        string ctorParamAssignments = string.Empty;
        bool hasChildren = false;

        foreach (IPropertySymbol prop in c.Properties)
        {
            string propName = GetPreferredName(prop);
            string propTypeName = prop.Type.Name;
            string tempVarName = prop.Name.ToSafeLowerCamel();
            string tempStringVarName = prop.Name.ToSafeLowerCamel() + "String";

            ctorParamAssignments += $@"{tempVarName},
            ";

            if (prop.Type.IsReferenceType && prop.Type.SpecialType is not SpecialType.System_String)
            {
                // Advance past the end of the starting element
                if (!hasChildren)
                {
                    hasChildren = true;
                    sb.Append(@"reader.Advance(1);
        ");
                }

                // Assume we've found a list
                if (prop.Type is INamedTypeSymbol { IsGenericType: true } nts)
                {
                    sb.Append($@"reader.Advance({propName.Length + "IsList='1'".Length + 3});
        System.Collections.Generic.List<{nts.TypeArguments[0]}> {tempVarName} = new();

        if (!reader.IsNext('>')) // Catch empty lists
        {{
            while (!reader.IsNext(""</"".AsSpan()))
                {tempVarName}.Add({nts.TypeArguments[0]}.Deserialize(ref reader));

            reader.Advance({propName.Length + 3});
        }}
        else
        {{
            reader.Advance(1);
        }}

        ");

                    continue;
                }

                if (prop.Type is INamedTypeSymbol childType)
                {
                    sb.Append($@"{childType} {tempVarName} = {childType}.Deserialize(ref reader);

        ");
                    continue;
                }
            }

            SpecialType propSpecialType = prop.Type.SpecialType;

            if (prop.NullableAnnotation is NullableAnnotation.Annotated)
            {
                if (prop.Type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedType)
                {
                    propTypeName = namedType.TypeArguments[0].ToString();
                    propSpecialType = namedType.TypeArguments[0].SpecialType;
                }

                sb.Append($@"
        {propTypeName}? {tempVarName} = default;
        if (reader.IsNext(' ' + nameof({propName}), true))
        {{
            reader.Advance(2);
            reader.TryReadTo(out ReadOnlySpan<char> {tempStringVarName}, '""', true);
            ");
            }
            else
            {
                // Advance past the property name.
                // The +2 comes from the =" segment.
                sb.Append($@"
        reader.Advance(1);
        reader.Advance({propName.Length + 2});
        reader.TryReadTo(out ReadOnlySpan<char> {tempStringVarName}, '""', true);
        {propTypeName} ");
            }

            switch (propSpecialType)
            {
                case SpecialType.System_String:
                    {
                        sb.Append($@"{tempVarName} = {tempStringVarName}.ToString();
        ");
                        break;
                    }
                case SpecialType.System_Boolean:
                    {
                        sb.Append($@"{tempVarName} = {tempStringVarName}[0] == '1';
        ");
                        break;
                    }
                default:
                    {
                        sb.Append($@"{tempVarName} = {propTypeName}.Parse({tempStringVarName});
        ");
                        break;
                    }
            }

            if (prop.NullableAnnotation is NullableAnnotation.Annotated)
            {
                sb.Append(@"}
        ");
            }
        }

        if (hasChildren)
            sb.Append($"reader.Advance({className.Length + 3});");
        else
            sb.Append("reader.TryAdvanceTo('>');"); // Self-closing elements might have a space at the end, so we have to search instead.

        sb.Append($@"
        return new {c.Name}
        (
            {ctorParamAssignments.CleanGeneratorString()}
        );");

        return sb.ToString();
    }

    private static string GenerateSerializeString(ClassToAugment c)
    {
        string className = GetPreferredName(c);

        StringBuilder sb = new($@"sb.Append(""<{className}"");

        ");
        bool hasChildren = false;

        foreach (IPropertySymbol prop in c.Properties)
        {
            if (prop.DeclaredAccessibility is not Accessibility.Public)
                continue;

            string propName = GetPreferredName(prop);

            if (prop.Type.IsReferenceType && prop.Type.SpecialType is not SpecialType.System_String)
            {
                if (!hasChildren)
                {
                    hasChildren = true;
                    sb.Append(@"sb.Append("">"");
        ");
                }

                // Assume we've found a list
                if (prop.Type is INamedTypeSymbol { IsGenericType: true } nts)
                {
                    sb.Append($@"sb.Append(""<{propName} IsList=\""1\"">"");

        foreach ({nts.TypeArguments[0]} element in {prop.Name})
            element.Serialize(sb);

        sb.Append(""</{propName}>"");
        ");

                    continue;
                }

                sb.Append($@"{propName}.Serialize(sb);
        ");
                continue;
            }

            ITypeSymbol propType = prop.Type;
            if (prop.NullableAnnotation is NullableAnnotation.Annotated)
            {
                if (prop.Type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedType)
                    propType = namedType.TypeArguments[0];

                sb.Append($@"if ({propName} is not null)
            ");
            }

            sb.Append(@"sb.Append($"" ").Append(propName).Append(@"=\"""").Append(").Append(GetPropValueString(propName, propType)).Append(@").Append(""\"""");
        ");
        }

        string closeString = hasChildren
            ? $"</{className}>"
            : " />";

        sb.Append($@"sb.Append(""{closeString}"");
        ");

        return sb.ToString();
    }

    private static string GetPropValueString(string propName, ITypeSymbol typeSymbol)
        => typeSymbol.SpecialType switch
        {
            SpecialType.System_String => propName,
            SpecialType.System_Boolean => $"((bool){propName} ? 1 : 0)", // Might be nullable
            _ => $"{propName}.ToString()"
        };

    private static string GetPreferredName(ClassToAugment c)
        => c.Attributes.TryFindAttribute(XmlObjectConstants.XmlNameAttributeTypeName, out AttributeData classNameAttr)
            ? (string)classNameAttr.ConstructorArguments[0].Value!
            : c.Name;

    private static string GetPreferredName(IPropertySymbol p)
        => p.GetAttributes().TryFindAttribute(XmlObjectConstants.XmlNameAttributeTypeName, out AttributeData classNameAttr)
            ? (string)classNameAttr.ConstructorArguments[0].Value!
            : p.Name;

    private static string GenerateConstructorString(ClassToAugment c)
    {
        string parameterList = string.Empty;
        string propertyAssignmentList = string.Empty;

        foreach (IPropertySymbol prop in c.Properties)
        {
            string paramName = prop.Name.ToSafeLowerCamel();

            if (prop.Type.TypeKind is TypeKind.Array)
            {
                IArrayTypeSymbol arrayType = (IArrayTypeSymbol)prop.Type;
                parameterList += $@"{arrayType.ElementType.ToDisplayString()}[] {paramName},
        ";
            }
            else
            {
                parameterList += $@"{prop.Type.ToDisplayString()} {paramName},
        ";
            }

            propertyAssignmentList += $@"{prop.Name} = {paramName};
        ";
        }

        return $@"public {c.Name}
    (
        {parameterList.CleanGeneratorString()}
    )
    {{
        {propertyAssignmentList.CleanGeneratorString()}
    }}";
    }
}
#pragma warning restore RCS1197 // Optimize StringBuilder.Append/AppendLine call.
