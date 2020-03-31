﻿using System;
 using System.Collections.Generic;
 using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace Dll2Sdk.Utils
{
    public static class ITypeDefOrRefExtensions
    {
        public static string ParsedFullNamespace(this ITypeDefOrRef typeDef)
        {
            var typeBuilder = new StringBuilder();
            typeBuilder.Append("DLL2SDK::");
            typeBuilder.Append(typeDef.DefinitionAssembly.Name.String.Parseable());

            var ns = typeDef.Namespace.Split('.');
            typeBuilder.Append(ns.Any(n => n.Length > 0)
                ? $"::{string.Join("::", ns.Where(n => n.Length > 0).Select(n => n.Parseable()))}"
                : "");

            return typeBuilder.ToString();
        }

        public static string ParsedFullName(this ITypeDefOrRef typeDef)
        {
            var typeBuilder = new StringBuilder();
            typeBuilder.Append(typeDef.ParsedFullNamespace());
            typeBuilder.Append("::");
            typeBuilder.Append(typeDef.Name.String.Parseable());
            return typeBuilder.ToString();
        }
    }
}