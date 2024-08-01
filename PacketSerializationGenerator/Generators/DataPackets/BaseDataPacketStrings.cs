using Microsoft.CodeAnalysis;
using PacketSerializationGenerator.Abstractions;
using PacketSerializationGenerator.Abstractions.Stringifiers;
using PacketSerializationGenerator.Extensions;
using PacketSerializationGenerator.Objects;
using PacketSerializationGenerator.Stringifiers.BinarySerialization;
using PacketSerializationGenerator.Stringifiers.Length;
using System;
using System.Collections.Generic;
using System.Linq;
using static PacketSerializationGenerator.Constants;

namespace PacketSerializationGenerator.Generators.DataPackets;

public abstract class BaseDataPacketStrings
{
    protected static readonly IntegerBinarySerializationStringifier _ibss;
    protected static readonly IReadOnlyList<IPropertyBinarySerializationStringifier> _binarySerializationStringifiers;
    protected static readonly IReadOnlyList<IPropertyLengthStringifier> _lengthStringifiers;

    static BaseDataPacketStrings()
    {
        _ibss = new IntegerBinarySerializationStringifier(PacketEndianness.LittleEndian);
        BooleanBinarySerializationStringifier bbss = new();
        StringBinarySerializationStringifier sbss = new(_ibss);
        ArrayBinarySerializationStringifier abss = new(_ibss);
        DataPacketBinarySerializationStringifier lpbss = new(_ibss);
        DataSerializableBinaryStringifier dsbss = new();

        _binarySerializationStringifiers = new List<IPropertyBinarySerializationStringifier>
        {
            bbss, _ibss, sbss, abss, lpbss, dsbss
        };

        ValueTypesLengthStringifier vtls = new();
        StringLengthStringifier sls = new();
        ArrayLengthStringifier als = new();
        DataPacketLengthStringifier dpls = new();
        IDataSerializableLengthStringifier dsls = new();

        _lengthStringifiers = new List<IPropertyLengthStringifier>
        {
            dpls, dsls, sls, als, vtls
        };
    }

    public abstract string GeneratePacketString(ClassToAugment c, Action<Diagnostic> reportDiagnostic);

    /// <summary>
    /// This method should generate the logic to set the initial
    /// offset and manage the OP code.
    /// </summary>
    /// <param name="c">The class for which logic is being generated.</param>
    /// <param name="reportDiagnostic">A delegate used to report a diagnostic message.</param>
    /// <param name="opCodeSymbol">The OP code field symbol.</param>
    /// <param name="opCodeEnumSymbol">The symbol of the underlying type of the OP code enum.</param>
    /// <param name="opCodeValue">The value of the OP code constant field.</param>
    /// <param name="deserializeString">The beginning of the deserialize string.</param>
    /// <param name="requiredBufferSizeString">The beginning of the required buffer size string.</param>
    /// <param name="serializeString">The beginning of the serialize string.</param>
    protected virtual void BeginFunctionStrings
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
        deserializeString = $@"int {DefaultOffsetVariableName} = readOPCode ? sizeof({opCodeEnumSymbol.EnumUnderlyingType}) : 0;
        ";
        requiredBufferSizeString = $@"includeOPCode ? sizeof({opCodeEnumSymbol.EnumUnderlyingType}) : 0;
        ";

        MyPropertySymbol enumPropSymbol = new(opCodeSymbol);
        serializeString = $@"int {DefaultOffsetVariableName} = 0;
        if (writeOPCode)
        {{
            {_ibss.GetSerializationString(enumPropSymbol)}
        }}
        ";
    }

    protected virtual void GenerateFunctionStrings
    (
        ClassToAugment c,
        Action<Diagnostic> reportDiagnostic,
        out string deserializeString,
        out string requiredBufferSizeString,
        out string serializeString
    )
    {
        deserializeString = string.Empty;
        requiredBufferSizeString = string.Empty;
        serializeString = string.Empty;

        IFieldSymbol? opCodeSymbol = c.Constants.FirstOrDefault(f => f.Name == "Type");
        if (opCodeSymbol?.ConstantValue is null)
        {
            Diagnostic d = Diagnostic.Create
            (
                DiagnosticDescriptors.DataPacketNoTypeConstant("Expected a 'Type' enum constant, storing the OP code of this data packet"),
                c.Locations[0]
            );
            reportDiagnostic(d);
            return;
        }

        if (opCodeSymbol.Type is not INamedTypeSymbol { EnumUnderlyingType: { } } opCodeEnumSymbol)
        {
            Diagnostic d = Diagnostic.Create
            (
                DiagnosticDescriptors.DataPacketNoTypeConstant("Expected a 'Type' enum constant, storing the OP code of this data packet"),
                c.Locations[0]
            );
            reportDiagnostic(d);
            return;
        }

        BeginFunctionStrings
        (
            c,
            reportDiagnostic,
            opCodeSymbol,
            opCodeEnumSymbol,
            opCodeSymbol.ConstantValue,
            out deserializeString,
            out requiredBufferSizeString,
            out serializeString
        );

        foreach (IPropertySymbol prop in c.Properties)
        {
            if (prop.DeclaredAccessibility is not Accessibility.Public)
                continue;

            MyPropertySymbol propSymbol = new(prop);
            bool propWritten = false;
            bool lengthWritten = false;

            foreach (IPropertyBinarySerializationStringifier pbss in _binarySerializationStringifiers)
            {
                if (!pbss.CanStringify(propSymbol))
                    continue;

                deserializeString += pbss.GetDeserializationString(propSymbol);
                serializeString += pbss.GetSerializationString(propSymbol);

                propWritten = true;
                break;
            }

            foreach (IPropertyLengthStringifier pls in _lengthStringifiers)
            {
                if (!pls.CanStringify(propSymbol))
                    continue;

                requiredBufferSizeString += pls.GetLengthString(propSymbol);
                lengthWritten = true;
                break;
            }

            if (!propWritten)
            {
                Diagnostic d = Diagnostic.Create
                (
                    DiagnosticDescriptors.GetStringGenerationFailure("No compatible stringifiers for the property " + prop.Name),
                    prop.Locations[0]
                );
                reportDiagnostic(d);

                deserializeString += $"\n// Could not generate string for {prop.Name}\n";
                serializeString += $"\n// Could not generate string for {prop.Name}\n";
            }

            if (!lengthWritten)
            {
                Diagnostic d = Diagnostic.Create
                (
                    DiagnosticDescriptors.GetStringGenerationFailure("No compatible LENGTH stringifiers for the property " + prop.Name),
                    prop.Locations[0]
                );
                reportDiagnostic(d);

                deserializeString += $"\n// Could not generate string for {prop.Name}\n";
                serializeString += $"\n// Could not generate string for {prop.Name}\n";
            }
        }

        deserializeString = FinaliseDeserializeString(deserializeString, c);
    }

    protected virtual string FinaliseDeserializeString(string input, ClassToAugment c)
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
        );";

        return input;
    }

    protected virtual string GenerateConstructorString(ClassToAugment c)
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
