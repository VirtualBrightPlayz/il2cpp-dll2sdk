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

        public static string ParsedFullNamespace(this ITypeDefOrRef typeDef, out bool didItWork)
        {
            var typeBuilder = new StringBuilder();
            typeBuilder.Append("DLL2SDK::");
            typeBuilder.Append(typeDef.DefinitionAssembly.Name.String.Parseable());

            var ns = typeDef.Namespace.Split('.');
            typeBuilder.Append(ns.Any(n => n.Length > 0)
                ? $"::{string.Join("::", ns.Where(n => n.Length > 0).Select(n => n.Parseable()))}"
                : "");
            didItWork = ns.Any(n => n.Length > 0);
            return typeBuilder.ToString();
        }

        public static string ParsedFullName(this ITypeDefOrRef typeDef)
        {
            var typeBuilder = new StringBuilder();
            bool didwork = false;
            typeBuilder.Append(typeDef.ParsedFullNamespace(out didwork));
            typeBuilder.Append("::");
            //Console.WriteLine(typeDef.FullName);
            string[] arr = typeDef.FullName.EndAt(typeDef.Name.String).Split('.');
            if (!didwork)
                typeBuilder.Append(string.Join("::", arr.Where(n => n.Length > 0).Select(n => n.Parseable())));
            else
                typeBuilder.Append(arr[arr.Length - 1].Parseable()/*typeDef.Name.String.Parseable()*/);
            return typeBuilder.ToString();
        }
    }
}