﻿using System.Collections.Generic;
 using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace Dll2Sdk.Utils
{
    public static class TypeDefExtensions
    {
        public static string TypeDefinitionStr(this TypeDef typeDef)
        {
            var typeBuilder = new StringBuilder();
            if (typeDef.HasGenericParameters)
            {
                var gps = typeDef.GenericParameters.ToArray();
                if (typeDef.DeclaringType != null)
                {
                    gps = gps.Where(gp => gp.Number >= typeDef.DeclaringType.GenericParameters.Count).ToArray();
                }
                if (gps.Length > 0)
                {
                    typeBuilder.Append("template <");
                    typeBuilder.Append(string.Join(", ", gps.Select(p => $"typename {p.Name.String.Parseable()}")));
                    typeBuilder.Append("> ");
                }
            }
            if (typeDef.IsEnum)
                typeBuilder.Append("enum ");
            if (typeDef.IsInterface)
            {
                typeBuilder.Append("using ");
                typeBuilder.Append(typeDef.Name.String.Parseable());
                typeBuilder.Append(" = void*");
            }
            else
            {
                typeBuilder.Append("struct ");
                //System.Console.WriteLine(typeDef.Name.Parseable());
                typeBuilder.Append(typeDef.Name.String.Parseable());
            }
            return typeBuilder.ToString();
        }
    }
}