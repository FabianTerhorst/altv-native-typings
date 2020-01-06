﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Durty.AltV.NativesTypingsGenerator.Models.Typing;

namespace Durty.AltV.NativesTypingsGenerator.TypingDef
{
    public class TypeDefFileGenerator
    {
        private readonly TypeDef _typeDefFile;
        private readonly bool _generateDocumentation;

        public TypeDefFileGenerator(
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
                fileContent.Append($"// THIS FILE IS AUTOGENERATED by Durty AltV NativeDB Typings Generator\n// Generated {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}\n");
                if (customHeaderLines != null)
                {
                    foreach (var customHeaderLine in customHeaderLines)
                    {
                        fileContent.Append($"//{customHeaderLine}\n");
                    }
                }
                fileContent.Append("\n");
            }
            fileContent.Append(_typeDefFile.Interfaces.Aggregate(new StringBuilder(), (current, typeDefInterface) => current.Append(GenerateInterface(typeDefInterface)).Append("\n")));
            fileContent.Append("\n");

            fileContent.Append(_typeDefFile.Types.Aggregate(new StringBuilder(), (current, typeDefType) => current.Append(GenerateType(typeDefType)).Append("\n")));
            fileContent.Append("\n");

            foreach (TypeDefModule typeDefModule in _typeDefFile.Modules)
            {
                fileContent.Append(GenerateModule(typeDefModule));
                fileContent.Append("\n");
            }

            return fileContent.ToString();
        }

        private StringBuilder GenerateInterface(TypeDefInterface typeDefInterface)
        {
            StringBuilder result = new StringBuilder($"interface {typeDefInterface.Name} {{\n");
            result = typeDefInterface.Properties.Aggregate(result, (current, property) => current.Append($"  {property.Name}: {property.Type};\n"));
            result.Append("}");
            return result;
        }

        private string GenerateType(TypeDefType typeDefType)
        {
            return $"type {typeDefType.Name} = {typeDefType.TypeDefinition};";
        }

        private StringBuilder GenerateModule(TypeDefModule typeDefModule)
        {
            StringBuilder result = new StringBuilder(string.Empty);
            result.Append($"declare module \"{typeDefModule.Name}\" {{\n");
            result = typeDefModule.Functions.Aggregate(result, (current, typeDefFunction) => current.Append($"{GenerateFunction(typeDefFunction)}\n"));
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
            result.Append($"\texport function {typeDefFunction.Name}(");
            foreach (var parameter in typeDefFunction.Parameters)
            {
                result.Append($"{parameter.Name}: {parameter.Type}");
                if (typeDefFunction.Parameters.Last() != parameter)
                {
                    result.Append(", ");
                }
            }
            result.Append($"): {typeDefFunction.ReturnType.Name};");

            return result;
        }

        private StringBuilder GenerateFunctionDocumentation(TypeDefFunction typeDefFunction)
        {
            //When no docs exist
            if (string.IsNullOrEmpty(typeDefFunction.Description) && typeDefFunction.Parameters.All(p => string.IsNullOrEmpty(p.Description) && string.IsNullOrEmpty(typeDefFunction.ReturnType.Description)))
                return new StringBuilder(string.Empty);

            StringBuilder result = new StringBuilder($"\t/**\n");
            if (!string.IsNullOrEmpty(typeDefFunction.Description))
            {
                string[] descriptionLines = typeDefFunction.Description.Split("\n");
                foreach (string descriptionLine in descriptionLines)
                {
                    string sanitizedDescriptionLine = descriptionLine.Replace("/*", string.Empty).Replace("*/", string.Empty);
                    result.Append($"\t* {sanitizedDescriptionLine}\n");
                }
            }
            //Add @remarks in the future?
            foreach (var parameter in typeDefFunction.Parameters)
            {
                if (!string.IsNullOrEmpty(parameter.Description))
                {
                    result.Append($"\t* @param {parameter.Name} {parameter.Description}\n");
                }
            }
            if (!string.IsNullOrEmpty(typeDefFunction.ReturnType.Description))
            {
                result.Append($"\t* @returns {typeDefFunction.ReturnType.Description}\n");
            }
            result.Append("\t*/\n");
            return result;
        }
    }
}
