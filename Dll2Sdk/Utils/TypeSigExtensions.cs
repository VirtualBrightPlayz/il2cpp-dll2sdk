﻿using System;
 using System.Collections.Generic;
 using System.Linq;
 using System.Text;
 using dnlib.DotNet;

namespace Dll2Sdk.Utils
{
    public static class TypeSigExtensions
    {
        public static HashSet<TypeDef> UsedTypes(this TypeSig typeSig)
        {
            var types = new HashSet<TypeDef>();
            var td = typeSig.GetNonNestedTypeRefScope().ResolveTypeDef();
            if (td != null)
            {
                types.Add(td);
                if (typeSig.IsGenericInstanceType)
                {
                    var gi = typeSig.ToGenericInstSig();
                    foreach (var gp in gi.GenericArguments)
                    {
                        foreach (var t in gp.UsedTypes())
                        {
                            types.Add(t);
                        }
                    }
                }
            }
            return types;
        }
        
        public static string ParsedReferenceTypeDefinition(this TypeSig typeSig)
        {
            var s = ParsedTypeSignatureStr(typeSig);
            if (!typeSig.IsValueType && !typeSig.IsGenericParameter)
                s += "*";
            return s;
        }
        
        public static string ParsedTypeSignatureStr(this TypeSig typeSig, bool useValueTypes = true)
        {
            switch (typeSig.ElementType)
            {
                case ElementType.Object:
                    return "DLL2SDK::mscorlib::System::Object";
                case ElementType.String:
                    return "DLL2SDK::mscorlib::System::String";
                case ElementType.Void when useValueTypes:
                    return "void";
                case ElementType.Boolean when useValueTypes:
                    return "bool";
                case ElementType.Char when useValueTypes:
                    return "wchar_t";
                case ElementType.I1 when useValueTypes:
                    return "int8_t";
                case ElementType.U1 when useValueTypes:
                    return "uint8_t";
                case ElementType.I2 when useValueTypes:
                    return "int16_t";
                case ElementType.U2 when useValueTypes:
                    return "uint16_t";
                case ElementType.I4 when useValueTypes:
                    return "int32_t";
                case ElementType.U4 when useValueTypes:
                    return "uint32_t";
                case ElementType.I8 when useValueTypes:
                    return "int64_t";
                case ElementType.U8 when useValueTypes:
                    return "uint64_t";
                case ElementType.R4 when useValueTypes:
                    return "float";
                case ElementType.R8 when useValueTypes:
                    return "double";
                case ElementType.I when useValueTypes:
                    return "intptr_t";
                case ElementType.U when useValueTypes:
                    return "uintptr_t";
            }

            var underlying = typeSig.GetNonNestedTypeRefScope().ResolveTypeDef();
            if (underlying?.IsEnum ?? false)
                return underlying.ParsedFullName();

            if (typeSig.IsArray || typeSig.IsSZArray)
            {
                return $"DLL2SDK::Array<{typeSig.Next.ParsedReferenceTypeDefinition()}>";
            }

            if (typeSig.IsGenericParameter)
            {
                return typeSig.ToGenericSig().GenericParam.Name.String.Parseable();
            }

            var genericCtx = new List<string>();
            if (typeSig.IsGenericInstanceType)
            {
                var gi = typeSig.ToGenericInstSig();
                genericCtx = new List<string>(gi.GenericArguments.Count);
                foreach (var t in gi.GenericArguments)
                {
                    genericCtx.Add(t.ParsedReferenceTypeDefinition());
                }
            }
            
            var typeDef = typeSig.TryGetTypeDef();
            if (typeDef?.HasGenericParameters ?? false)
            {
                var gi = typeDef.GenericParameters;
                genericCtx = new List<string>(gi.Count);
                foreach (var t in gi)
                {
                    genericCtx.Add(t.Name.String.Parseable());
                }
            }

            if (genericCtx.Count > 0)
            {
                var builder = new StringBuilder();
                builder.Append(typeSig.ToTypeDefOrRef().ParsedFullName());
                builder.Append("<");
                builder.Append(string.Join(", ", genericCtx));
                builder.Append(">");
                return builder.ToString();
            }
            
            if (typeSig.IsByRef || typeSig.IsPointer)
            {
                return $"{typeSig.Next.ParsedTypeSignatureStr()}";
            }
            
            return typeSig.ToTypeDefOrRef().ParsedFullName();
        }
    }
}