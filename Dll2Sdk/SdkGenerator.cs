using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dll2Sdk.Generators;
using Dll2Sdk.Utils;
using dnlib.DotNet;
using Nito.Collections;

namespace Dll2Sdk
{
    public class SdkGenerator
    {
        public SdkGenerator(ModuleDefMD module, IAssembly corlib)
        {
            Console.WriteLine("generating types for: " + module); 

            var visitedTypes = new HashSet<TypeDef>();
            var toVisit = new Deque<TypeDef>();

            foreach (var type in module.Types)
            {
                toVisit.AddToBack(type);
            }

            if (module.Name.Contains("mscorlib"))
                toVisit.AddToBack(module.Types.First(t => t.FullName == "System.Object"));

            var inOrderGenerators = new List<TypeSdkGenerator>();
            while (toVisit.Count > 0)
            {
                var currentVisited = toVisit.RemoveFromBack();
                if (visitedTypes.Contains(currentVisited))
                    continue;
                
                var canVisit = true;
                void CheckDep(TypeSig ts)
                {
                    if (ts != null && ts.IsValueType && !ts.IsPrimitive)
                    {
                        foreach (var t2 in ts.UsedTypes())
                        {
                            var tn = t2.GetNonNestedTypeRefScope().ResolveTypeDef();
                            if (tn != null && tn != currentVisited && tn.DefinitionAssembly == module.Assembly && !visitedTypes.Contains(tn))
                            {
                                canVisit = false;
                                toVisit.AddToBack(tn);
                            }
                        }
                    }
                }

                var baseType = currentVisited.BaseType?.ResolveTypeDef();
                if (baseType != null && baseType.DefinitionAssembly == module.Assembly && !visitedTypes.Contains(baseType))
                {
                    canVisit = false;
                    toVisit.AddToBack(baseType);
                }
                
                foreach (var valueField in currentVisited.Fields)
                {
                    CheckDep(valueField.FieldType);
                }

                foreach (var method in currentVisited.Methods)
                {
                    foreach (var arg in method.Parameters)
                    {
                        CheckDep(arg.Type);
                    }
                }

                if (!canVisit)
                {
                    toVisit.AddToFront(currentVisited);
                    continue;
                }

                visitedTypes.Add(currentVisited);
                inOrderGenerators.Add(new TypeSdkGenerator(currentVisited, currentVisited.ParsedFullNamespace()));
            }

            var dependencies = new HashSet<IAssembly>();

            void AddDependency(IAssembly assemblyRef)
            {
                if (assemblyRef == module.Assembly)
                {
                    return;
                }
                
                if (assemblyRef.Name.Contains("System.Private.CoreLib"))
                {
                    return;
                }
                
                dependencies.Add(assemblyRef);
            }
            
            void AddTypeDependency(ITypeDefOrRef typeDefOrRef)
            {
                if (typeDefOrRef == null)
                {
                    return;
                }
                
                if (typeDefOrRef.IsValueType && !typeDefOrRef.ResolveTypeDefThrow().IsEnum)
                {
                    AddDependency(typeDefOrRef.DefinitionAssembly);
                }
            }

            AddDependency(corlib);
            foreach (var dep in module.GetTypes().Where(t => !t.IsInterface))
            {
                if (dep.BaseType != null && !dep.IsEnum)
                {
                    AddDependency(dep.BaseType.DefinitionAssembly);
                }
                
                foreach (var field in dep.Fields)
                {
                    AddTypeDependency(field.FieldType.GetNonNestedTypeRefScope());
                }

                foreach (var method in dep.Methods)
                {
                    AddTypeDependency(method.ReturnType.GetNonNestedTypeRefScope());
                    foreach (var param in method.Parameters)
                    {
                        AddTypeDependency(param.Type.GetNonNestedTypeRefScope());
                    }
                }
            }

            var deps = new HashSet<string>();
            var depBuilder = new StringBuilder();
            foreach (var d in dependencies)
            {
                var dn = d.Name.String.Parseable();
                if (deps.Add(dn))
                {
                    depBuilder.AppendLine($"#include \"..\\{dn}\\{dn}.hpp\"");
                }
            }
            
            var hdr = new IndentedBuilder();
            var forward = new IndentedBuilder();
            var file = new IndentedBuilder();
            foreach (var generator in inOrderGenerators)
            {
                forward.AppendIndented("namespace ");
                forward.Append(generator.Namespace);
                forward.AppendNewLine();
            
                forward.AppendIndentedLine("{");
                forward.Indent();
                
                generator.GenerateForwardTypeDefinition(forward);
            
                forward.Outdent();
                forward.AppendIndentedLine("}");

                if (!generator.TypeDef.IsInterface)
                {
                    hdr.AppendIndented("namespace ");
                    hdr.Append(generator.Namespace);
                    hdr.AppendNewLine();
            
                    hdr.AppendIndentedLine("{");
                    hdr.Indent();
                
                    generator.GenerateHeaderTypeDefinition(hdr);
            
                    hdr.Outdent();
                    hdr.AppendIndentedLine("}");
                }

                generator.GenerateImplementation(file);
            }

            var name = module.Assembly.Name.String.Parseable();
            var path = $"out/DLL2SDK/{name}";
            Directory.CreateDirectory(path);
            File.WriteAllText($"{path}/{name}.hpp", $@"//generated with dll2sdk
#pragma once
#include ""..\dll2sdk_forward.g.hpp""
{depBuilder}
{hdr}");
            File.WriteAllText($"{path}/{name}_forward.hpp", $@"//generated with dll2sdk
#pragma once
#include ""..\dll2sdk_forward.g.hpp""
{forward}
");
            File.WriteAllText($"{path}/{name}.cpp", $@"//generated with dll2sdk
#include ""{name}.hpp""
{file}");
            File.AppendAllText("out/DLL2SDK/dll2sdk_forward.g.hpp", $@"#include ""{name}\{name}_forward.hpp""
");
        }
    }
}