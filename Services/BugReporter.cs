using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Pete.Services.Interfaces;
using Prism.Services.Dialogs;

namespace Pete.Services
{
    public class BugReporter : IBugReporter
    {
        #region Consts
        private const string PATH_BUGS = "bug reports";
        #endregion

        #region Methods
        private string SaveReport(Exception ex, Dictionary<string, string> otherData)
        {
            DateTime date = DateTime.UtcNow;
            string fileNameBase = $"Pete {date:dd-MM-yyyy HH-mm-ss}";
            if (!Directory.Exists(PATH_BUGS)) Directory.CreateDirectory(PATH_BUGS);

            string fileName = Path.Combine(PATH_BUGS, fileNameBase + ".txt");
            int counter = 1;
            while (File.Exists(fileName))
                fileName = Path.Combine(PATH_BUGS, $"{fileNameBase} ({++counter}).txt");

            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                #region Header
                sw.Write($"[Bug Report on ");
                sw.Write(date.ToString("dd/MM/yyyy HH:mm:ss"));
                sw.WriteLine(']');

                sw.Write("Source: ");
                sw.WriteLine(ex.Source);

                sw.Write("HResult: ");
                sw.WriteLine(ex.HResult);

                sw.Write("Help link: ");
                sw.WriteLine(ex.HelpLink);
                #endregion

                #region other Data
                sw.WriteLine("\n -- Extra data --");
                foreach (KeyValuePair<string,string> data in otherData)
                {
                    sw.Write('[');
                    sw.Write(data.Key);
                    sw.Write("]: `");
                    sw.Write(data.Value);
                    sw.WriteLine('`');
                }
                sw.WriteLine(" ----------------\n");
                #endregion

                #region Exception data
                sw.WriteLine(" -- Exception data --");
                foreach (DictionaryEntry entry in ex.Data)
                {
                    sw.Write('[');
                    sw.Write(entry.Key);
                    sw.Write("]: `");
                    sw.Write(entry.Value);
                    sw.WriteLine('`');
                }
                sw.WriteLine(" --------------------\n");
                #endregion

                #region Exception info
                sw.Write("Message: \"");
                sw.Write(ex.Message);
                sw.WriteLine('"');
                sw.Write("[Exception type]: `");
                sw.Write(ex.GetType().FullName);
                sw.WriteLine('`');

                sw.Write("Target site: ");
                sw.WriteLine(GenerateMethodName(ex.TargetSite));

                sw.WriteLine($"\n -- Stack Trace --");
                sw.WriteLine(ex.StackTrace);
                #endregion
            }

            return fileName;

        }
        private static string GenerateMethodName(MethodBase method)
        {
            if (method == null) return string.Empty;

            List<string> declaringTypes = new List<string>();
            Type type = method.DeclaringType;
            while (type != null)
            {
                declaringTypes.Insert(0, type.FullName);
                type = type.DeclaringType;
            }

            List<string> modifiers = new List<string>();
            if (method.IsPrivate) modifiers.Add("private");
            else if (method.IsPublic) modifiers.Add("public");
            else if (method.IsFamily) modifiers.Add("protected");
            else if (method.IsAssembly) modifiers.Add("internal");
            else if (method.IsFamilyOrAssembly) modifiers.Add("protected internal");
            else if (method.IsFamilyAndAssembly) modifiers.Add("private protected");


            if (method.IsAbstract) modifiers.Add("abstract");
            else if (method.IsVirtual) modifiers.Add("virtual");
            else if (method.IsStatic) modifiers.Add("static");

            List<string> parameters = new List<string>();
            foreach(ParameterInfo param in method.GetParameters())
            {
                List<string> parts= new List<string>();
                if (param.IsOut && param.IsIn) parts.Add("[Out, In]");
                else if (param.IsOut) parts.Add("[Out]");
                else if (param.IsIn) parts.Add("[In]");

                parts.Add(param.ParameterType.FullName ?? param.ParameterType.Name);
                parts.Add(param.Name);

                if (param.HasDefaultValue)
                {
                    parts.Add("=");
                    parts.Add(param.DefaultValue?.ToString());
                }
                parameters.Add(string.Join(' ', parts));
            }

            string fullStr = string.Empty;
            if (declaringTypes.Count > 0)
                fullStr += $"in [{string.Join("./",declaringTypes)}] ";

            if (method is MethodInfo methodInfo)
                modifiers.Add(methodInfo.ReturnType.FullName);
            else if (method is ConstructorInfo)
                modifiers.Add("ctor()");

            fullStr += string.Join(' ', modifiers);


            fullStr += " " + method.Name;

            if (method.IsGenericMethod)
            {
                fullStr += "<";
                Type[] generic = method.GetGenericArguments();
                string[] genStr = new string[generic.Length];
                for (int i = 0; i < generic.Length; i++) genStr[i] = generic[i].FullName ?? generic[i].Name;
                fullStr += string.Join(", ", genStr) + ">";
            }
            fullStr += "(" + string.Join(", ", parameters) + ");";

            return fullStr;

        }
        public string Report(Exception ex, Dictionary<string, string> otherData)
        {
            string path = SaveReport(ex, otherData);
            path = Path.Combine(Environment.CurrentDirectory, path);
            Debug.WriteLine($"[BugReporter] report saved to {path}");
            return path;
        }
        #endregion
    }
}
