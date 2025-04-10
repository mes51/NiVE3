using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Config;
using System.Xml.Serialization;

namespace NiVE3.Util
{
    static class ErrorLog
    {
        public static void ExportErrorLog(Exception ex)
        {
            try
            {
                if (!Directory.Exists(Paths.ErrorLogDirectory))
                {
                    Directory.CreateDirectory(Paths.ErrorLogDirectory);
                }

                var sb = new StringBuilder();
                sb.AppendLine("Log:");
                sb.AppendLine(FormatExceptionMessage(ex));

                sb.AppendLine();
                sb.AppendLine("Application Version: " + Assembly.GetExecutingAssembly().GetName()?.Version?.ToString());

                sb.AppendLine();
                sb.AppendLine("OS: " + EnviromentInfo.GetOSProductName() + " Build " + EnviromentInfo.GetOSReleaseId());

                sb.AppendLine();
                sb.AppendLine("ProcessType: " + (Environment.Is64BitProcess ? "x64" : "x86"));

                sb.AppendLine();
                sb.AppendLine("Loaded Assemblies:");
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var version = assembly.GetName().Version;
                        sb.AppendLine($"{Path.GetFileName(assembly.Location)}:{version}");
                    }
                    catch { }
                }

                File.WriteAllText(Path.Combine(Paths.ErrorLogDirectory, DateTime.Now.ToString("yyyyMMddhhmmss") + "-errorlog.txt"), sb.ToString());
            }
            catch { }
        }

        public static string FormatExceptionMessage(Exception? e, int indent = 0)
        {
            if (e == null)
            {
                return "";
            }

            var indentSpace = string.Join("", Enumerable.Repeat("    ", indent));
            var sb = new StringBuilder();

            sb.AppendLine(indentSpace + e.GetType().FullName);
            sb.AppendLine(indentSpace + e.Message);
            foreach (var line in e.StackTrace?.Split(Environment.NewLine) ?? [])
            {
                sb.AppendLine(indentSpace + line);
            }

            if (e is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    sb.AppendLine();
                    sb.AppendLine(FormatExceptionMessage(innerException, indent + 1));
                    sb.AppendLine();
                }
            }
            else if (e.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine(FormatExceptionMessage(e.InnerException, indent + 1));
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
