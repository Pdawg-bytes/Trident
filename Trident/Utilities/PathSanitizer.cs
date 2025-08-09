using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trident.Utilities
{
    internal static class PathSanitizer
    {
        internal static string SanitizePath(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                return string.Empty;

            string sanitized = inputPath.Trim().Trim('"', '\'');
            sanitized = sanitized.Replace('/', Path.DirectorySeparatorChar);

            char[] invalidChars = Path.GetInvalidPathChars();
            return new string([.. sanitized.Where(c => !invalidChars.Contains(c))]);
        }
    }
}