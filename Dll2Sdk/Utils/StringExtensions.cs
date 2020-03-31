using System.Text;

namespace Dll2Sdk.Utils
{
    public static class StringExtensions
    {
        public static string Parseable(this string str)
        {
            var builder = new StringBuilder();
            if (char.IsDigit(str[0]))
                builder.Append("_");
            foreach (var c in str)
            {
                if (!char.IsLetterOrDigit(c))
                    builder.Append("_");
                else
                    builder.Append(c);
            }
            return builder.ToString();
        }
    }
}