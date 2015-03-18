using System.Collections.Generic;
using System.Text;

namespace BitDB
{
    static class StringExtensions
    {
        public static string[] SplitToArray(this string input)
        {
            var split = new List<string>();
            var sb = new StringBuilder();
            var splitOnQuote = false;
            const char quote = '"';
            const char space = ' ';
            foreach (var c in input.ToCharArray())
            {
                if (splitOnQuote)
                {
                    if (c == quote)
                    {
                        if (sb.Length > 0)
                        {
                            split.Add(sb.ToString());
                            sb.Clear();
                        }
                        splitOnQuote = false;
                    }
                    else { sb.Append(c); }
                }
                else
                {
                    if (c == space)
                    {
                        if (sb.Length > 0)
                        {
                            split.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else if (c == quote)
                    {
                        if (sb.Length > 0)
                        {
                            split.Add(sb.ToString());
                            sb.Clear();
                        }
                        splitOnQuote = true;
                    }

                    else { sb.Append(c); }
                }
            }
            if (sb.Length > 0) split.Add(sb.ToString());
            return split.ToArray();
        }
    }
}
