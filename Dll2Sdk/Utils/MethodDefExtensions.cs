using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace Dll2Sdk.Utils
{
    public static class MethodDefExtensions
    {
        private static Dictionary<TypeDef, HashSet<MethodDef>> _interfacedMethods = new Dictionary<TypeDef, HashSet<MethodDef>>();

        public static string TypeDefinitionStr(this MethodDef method, string context = null)
        {
            var builder = new StringBuilder();
            builder.Append(method.ReturnType.ParsedReferenceTypeDefinition(/*context: context*/));
            builder.Append("(*)(");
            builder.Append(string.Join(", ", method.Parameters.Select((p, i) => $"{(p.IsHiddenThisParameter ? $"{p.Type.ParsedTypeSignatureStr(context: context)/*p.Type.ToTypeDefOrRef().ParsedFullName()*/}*" : p.Type.ParsedReferenceTypeDefinition(context: null))}")));
            builder.Append(")");
            return builder.ToString();
        }

        public static string VirtualTypeDefinitionStr(this MethodDef method, string context = null)
        {
            var builder = new StringBuilder();
            builder.Append(method.ReturnType.ParsedReferenceTypeDefinition(/*context: context*/));
            builder.Append("(*)(");
            builder.Append(string.Join(", ", method.Parameters.Select((p, i) => $"{(p.IsHiddenThisParameter ? $"{p.Type.ParsedTypeSignatureStr(context: context)/*p.Type.ToTypeDefOrRef().ParsedFullName()*/}*" : p.Type.ParsedReferenceTypeDefinition(context: context))}").Append("void*")));
            builder.Append(")");
            return builder.ToString();
        }

        public static object GetRva(this MethodDef method)
        {
            var addr = method.CustomAttributes.FirstOrDefault(a => a.TypeFullName.Contains("AddressAttribute"));
            return addr?.GetNamedArgument("RVA", true).Value;
        }

        public static object GetSlot(this MethodDef method)
        {
            var addr = method.CustomAttributes.FirstOrDefault(a => a.TypeFullName.Contains("AddressAttribute"));
            return addr?.GetNamedArgument("Slot", true).Value;
        }

        public static string InvokeStr(this MethodDef method, object rva)
        {
            var builder = new StringBuilder();
            //string ctx = method.TypeArgumentStr();

            if (method.HasGenericParameters)
            {
                builder.Append("<");
                builder.Append(string.Join(", ", method.GenericParameters.Select(g => $"{g.Name.String.Parseable()}")));
                builder.Append("> ");
            }
            else if (method.DeclaringType.GenericParameters.Count > 0)
            {
                builder.Append("<");
                builder.Append(string.Join(", ", method.DeclaringType.GenericParameters.Select(g => $"{g.Name.String.Parseable()}")));
                builder.Append("> ");
            }
            string ctx = builder.ToString();
            if (true || ctx.Length <= 2)
                ctx = null;
            if (method.IsVirtual && !method.DeclaringType.IsValueType)
            {
                return
                    $"{{ const VirtualInvokeData& Data = this->ClassPtr->VTable[{method.GetSlot()}]; return reinterpret_cast<{method.VirtualTypeDefinitionStr(ctx)}>(Data.methodPtr)({string.Join(", ", method.Parameters.Select((p, i) => $"{(p.IsHiddenThisParameter ? "this" : ((p.Name.Length > 0 ? p.Name.Parseable() : $"a{i}") + "_"))}").Append("Data.method"))}); }}";
            }
            else
            {
                return
                    $"{{ return reinterpret_cast<{method.TypeDefinitionStr(ctx)}>(DLL2SDK::GameAssemblyBase + {rva:X8})({string.Join(", ", method.Parameters.Select((p, i) => $"{(p.IsHiddenThisParameter ? "this" : ((p.Name.Length > 0 ? p.Name.Parseable() : $"a{i}") + "_"))}"))}); }}";
            }
        }

        public static string ArgumentStr(this MethodDef method)
        {
            return string.Join(", ",
                method.Parameters.Where(p => !p.IsHiddenThisParameter).Select((p, i) =>
                    $"{p.Type.ParsedReferenceTypeDefinition()} {(p.Name.Length > 0 ? p.Name.Parseable() : $"a{i}")}_"));
        }

        public static string TypeArgumentStr(this MethodDef method)
        {
            return "<" + string.Join(", ",
                method.Parameters.Where(p => !p.IsHiddenThisParameter).Select((p, i) =>
                    $"{p.Type.ParsedTypeSignatureStr(typesOnly: true)} {(p.Name.Length > 0 ? p.Name.Parseable() : $"a{i}")}_")) + ">";
        }

        public static string ImplementationStr(this MethodDef method, object rva)
        {
            var builder = new StringBuilder();
            if (method.DeclaringType.GenericParameters.Count > 0)
            {
                builder.Append("template <");
                builder.Append(string.Join(", ", method.DeclaringType.GenericParameters.Select(g => $"typename {g.Name.String.Parseable()}")));
                builder.Append("> ");
            }

            if (method.HasGenericParameters)
            {
                builder.Append("template <");
                builder.Append(string.Join(", ", method.GenericParameters.Select(g => $"typename {g.Name.String.Parseable()}")));
                builder.Append("> ");
            }
            
            builder.Append(method.ReturnType.ParsedReferenceTypeDefinition());
            builder.Append(" ");

            builder.Append(method.DeclaringType.ToTypeSig().ParsedTypeSignatureStr(false));
            builder.Append("::");
            builder.Append($"{method.Name.String.Parseable()}_{method.Rid}");
            builder.Append("(");
            builder.Append(method.ArgumentStr());
            builder.Append(")");
            builder.Append(method.InvokeStr(rva));
            return builder.ToString();
        }

        public static string DeclarationStr(this MethodDef method)
        {
            var builder = new StringBuilder();
            if (method.HasGenericParameters)
            {
                builder.Append("template <");
                builder.Append(string.Join(", ", method.GenericParameters.Select(g => $"typename {g.Name.String.Parseable()}")));
                builder.Append("> ");
            }
            
            if (method.IsStatic)
            {
                builder.Append("static ");
            }
            
            builder.Append(method.ReturnType.ParsedReferenceTypeDefinition());
            builder.Append(" ");
            builder.Append($"{method.Name.String.Parseable()}_{method.Rid}");
            builder.Append("(");
            builder.Append(method.ArgumentStr());
            builder.Append(")");
            var rva = method.GetRva();
            if (method.HasGenericParameters && rva != null)
            {
                builder.Append(method.InvokeStr(rva));
            }
            else
            {
                builder.Append(";");
            }
            return builder.ToString();
        }
        
        public static bool IsInterfacedMethod(this MethodDef method)
        {
            if (method.DeclaringType.IsInterface)
                return true;

            if (_interfacedMethods.TryGetValue(method.DeclaringType, out var mds))
                return mds.Contains(method);
            
            mds = new HashSet<MethodDef>();
            _interfacedMethods.Add(method.DeclaringType, mds);
            
            var interfaces = new HashSet<TypeDef>();
            var toVisit = new Queue<InterfaceImpl>();
            
            foreach (var inter in method.DeclaringType.Interfaces)
                toVisit.Enqueue(inter);

            var isIfaceMethod = false;
            while (toVisit.Count > 0)
            {
                var inter = toVisit.Dequeue().Interface.ResolveTypeDefThrow();
                interfaces.Add(inter);

                foreach (var m in inter.Methods)
                {
                    mds.Add(m);
                    if (m.FullName == method.FullName)
                    {
                        isIfaceMethod = true;
                    }
                }

                foreach (var i in inter.Interfaces)
                {
                    if (!interfaces.Contains(inter))
                        toVisit.Enqueue(i);
                }
            }

            return isIfaceMethod;
        }
    }
}