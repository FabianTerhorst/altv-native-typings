﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Durty.AltV.NativesTypingsGenerator.Converters;
using Durty.AltV.NativesTypingsGenerator.Extensions;
using Durty.AltV.NativesTypingsGenerator.Models.Typing;

namespace Durty.AltV.NativesTypingsGenerator.TypingDef
{
    public class TypeDefCSharpFileGenerator
    {
        private readonly TypeDef _typeDefFile;
        private readonly bool _generateDocumentation;

        public TypeDefCSharpFileGenerator(
            TypeDef typeDefFile,
            bool generateDocumentation = true)
        {
            _typeDefFile = typeDefFile;
            _generateDocumentation = generateDocumentation;
        }

        public string Generate(bool generateHeader = true, List<string> customHeaderLines = null)
        {
            StringBuilder fileContent = new StringBuilder(string.Empty);
            if (generateHeader)
            {
                if (customHeaderLines != null)
                {
                    foreach (var customHeaderLine in customHeaderLines)
                    {
                        fileContent.Append($"//{customHeaderLine}\n");
                    }
                }
                fileContent.Append("\n");
            }

            foreach (TypeDefModule typeDefModule in _typeDefFile.Modules)
            {
                fileContent.Append(GenerateModule(typeDefModule));
                fileContent.Append("\n");
            }

            return fileContent.ToString();
        }

        private StringBuilder GenerateModule(TypeDefModule typeDefModule)
        {
            StringBuilder result = new StringBuilder(string.Empty);
            result.Append($"using WebAssembly;\n");
            result.Append($"using WebAssembly.Core;\n\n");
            result.Append($"namespace AltV.Net.Client\n{{\n");
            result.Append($"\tpublic class NativeNatives\n\t{{\n");
            result.Append($"\t\tprivate readonly JSObject native;\n\n");
            foreach (var typeDefFunction in typeDefModule.Functions)
            {
                result.Append($"\t\tprivate readonly Function {typeDefFunction.Name};\n");
            }
            result.Append($"\n");
            result.Append($"\t\tpublic NativeNatives(JSObject native)\n\t\t{{\n");
            result.Append($"\t\t\tthis.native = native;\n");
            foreach (var typeDefFunction in typeDefModule.Functions)
            {
                result.Append($"\t\t\tif ({typeDefFunction.Name} == null) {typeDefFunction.Name} = (Function) native.GetObjectProperty(\"{typeDefFunction.Name}\");\n");
            }
            result.Append($"\t\t}}\n\n");
            result = typeDefModule.Functions.Aggregate(result, (current, typeDefFunction) => current.Append($"{GenerateFunction(typeDefFunction)}\n"));
            result.Append("\t}\n");
            result.Append("}");
            return result;
        }

        private StringBuilder GenerateFunction(TypeDefFunction typeDefFunction)
        {
            StringBuilder result = new StringBuilder(string.Empty);
            if (_generateDocumentation)
            {
                result.Append(GenerateFunctionDocumentation(typeDefFunction));
            }
            result.Append($"\t\tpublic {new NativeReturnTypeToCSharpTypingConverter().Convert(null, typeDefFunction.ReturnType.NativeType)} {typeDefFunction.Name.FirstCharToUpper()}(");
            foreach (var parameter in typeDefFunction.Parameters)
            {
                result.Append($"{new NativeTypeToCSharpTypingConverter().Convert(null, parameter.NativeType, false)} {parameter.Name}");
                if (typeDefFunction.Parameters.Last() != parameter)
                {
                    result.Append(", ");
                }
            }
            result.Append($")\n\t\t{{\n");
            if (typeDefFunction.ReturnType.Name != "void")
            {
                result.Append($"\t\t\treturn {typeDefFunction.Name}.Call(native");
            }
            else
            {
                result.Append($"\t\t\t{typeDefFunction.Name}.Call(native");
            }
            foreach (var parameter in typeDefFunction.Parameters)
            {
                if (typeDefFunction.Parameters.First() == parameter)
                {
                    result.Append(", ");
                }
                result.Append($"{parameter.Name}");
                if (typeDefFunction.Parameters.Last() != parameter)
                {
                    result.Append(", ");
                }
            }
            result.Append($");\n");

            result.Append($"\t\t}}\n");

            return result;
        }

        private StringBuilder GenerateFunctionDocumentation(TypeDefFunction typeDefFunction)
        {
            //When no docs exist
            if (typeDefFunction.ReturnType.NativeType.Count <= 1 &&
                string.IsNullOrEmpty(typeDefFunction.Description) && 
                typeDefFunction.Parameters.All(p => string.IsNullOrEmpty(p.Description) && string.IsNullOrEmpty(typeDefFunction.ReturnType.Description)))
                return new StringBuilder(string.Empty);

            StringBuilder result = new StringBuilder($"\t\t/// <summary>\n");
            if (!string.IsNullOrEmpty(typeDefFunction.Description))
            {
                string[] descriptionLines = typeDefFunction.Description.Split("\n");
                foreach (string descriptionLine in descriptionLines)
                {
                    string sanitizedDescriptionLine = descriptionLine.Replace("/*", string.Empty).Replace("*/", string.Empty).Trim();
                    result.Append($"\t\t/// {sanitizedDescriptionLine}\n");
                }
            }
            result.Append("\t\t/// </summary>\n");

            //Add @remarks in the future?
            foreach (var parameter in typeDefFunction.Parameters)
            {
                if (!string.IsNullOrEmpty(parameter.Description))
                {
                    result.Append($"\t\t/// <param name=\"{parameter.Name}\">{parameter.Description}</param>\n");
                }
            }
            //For now build return doc with return type because we dont have strong typed return value for natives returning arrays currently..
            //if (!string.IsNullOrEmpty(typeDefFunction.ReturnType.Description))
            //{
            //    result.Append($"\t\t/// <returns>{typeDefFunction.ReturnType.Description}</returns>\n");
            //}
            string returnTypeForTyping = string.Empty;
            if (typeDefFunction.ReturnType.NativeType.Count > 1)
            {
                returnTypeForTyping = "Array<";
                for (int i = 0; i < typeDefFunction.ReturnType.NativeType.Count; i++)
                {
                    returnTypeForTyping += new NativeTypeToCSharpTypingConverter().Convert(null, typeDefFunction.ReturnType.NativeType[i], false);
                    if (i != typeDefFunction.ReturnType.NativeType.Count - 1)
                    {
                        returnTypeForTyping += ", ";
                    }
                }
                returnTypeForTyping += ">";
            }
            if (!string.IsNullOrEmpty(typeDefFunction.ReturnType.Description) || !string.IsNullOrEmpty(returnTypeForTyping))
            {
                var returnDescription = $"{returnTypeForTyping} {typeDefFunction.ReturnType.Description}".Trim();
                result.Append($"\t\t/// <returns>{returnDescription}</returns>\n");
            }
            return result;
        }
    }
}
