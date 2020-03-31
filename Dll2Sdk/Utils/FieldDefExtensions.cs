using System.Text;
using dnlib.DotNet;

namespace Dll2Sdk.Utils
{
    public static class FieldDefExtensions
    {
        public static string ParsedTypeDefinitionStr(this FieldDef fieldDef)
        {
            var builder = new StringBuilder();
            builder.Append(fieldDef.FieldType.ParsedReferenceTypeDefinition());
            builder.Append(" ");
            builder.Append($"{fieldDef.Name.String.Parseable()}_");
            builder.Append(";");
            return builder.ToString();
        }
    }
}