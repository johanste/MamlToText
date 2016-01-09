using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Framework.Runtime.Common.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelpGenerator
{

    class Loader : System.Runtime.Loader.AssemblyLoadContext, IAssemblyLoader
    {
        private string path;

        public Loader(string path)
        {
            this.path = path;
        }

        public IntPtr LoadUnmanagedLibrary(string name)
        {
            return base.LoadUnmanagedDll(name);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var probePath = System.IO.Path.Combine(path, assemblyName.Name + ".dll");
            if (File.Exists(probePath))
            {
                var a = LoadFromAssemblyPath(probePath);
                return a;
            }
            else
            {
                return null;
            }
        }

        Assembly IAssemblyLoader.Load(AssemblyName assemblyName)
        {
            return Load(assemblyName);
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            var pkgRoot = app.Option("--root|-r", "Package root directory", CommandOptionType.SingleValue);
            var outputRoot = app.Option("--out|-o", "Output directory", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var di = new DirectoryInfo(pkgRoot.Value());
                var outRoot = String.IsNullOrEmpty(outputRoot.Value()) ? "help" : outputRoot.Value();

                foreach (var indexDir in di.EnumerateDirectories("_indexes", SearchOption.AllDirectories))
                {
                    var pkgName = indexDir.Parent.Parent.Name;
                    var cmdletIndexFilePath = Path.Combine(indexDir.FullName, "_cmdlets.idx");
                    if (File.Exists(cmdletIndexFilePath))
                    {
                        var contentDir = Path.Combine(indexDir.Parent.FullName, "content");
                        var helpDir = Path.Combine(outRoot, pkgName);
                        var libDir = Path.Combine(indexDir.Parent.FullName, "lib");
                        foreach (var cmdletRow in File.ReadAllLines(cmdletIndexFilePath))
                        {
                            var keys = cmdletRow.Split(':')[0];
                            var assemblyAndType = cmdletRow.Split(':')[1];
                            var assembly = assemblyAndType.Split('/')[0];
                            var typeName = assemblyAndType.Split('/')[1];

                            var libDirectoryInfo = new DirectoryInfo(libDir);
                            var assemblyFileInfo = libDirectoryInfo.GetFiles(assembly, SearchOption.AllDirectories).FirstOrDefault();

                            if (assemblyFileInfo != null)
                            {
                                var loader = new Loader(assemblyFileInfo.DirectoryName);
                                PlatformServices.Default.AssemblyLoaderContainer.AddLoader(loader);
                                var assemblyName = assembly.Substring(0, assembly.Length - ".dll".Length);
                                var loadedAssembly = loader.LoadFromAssemblyName(new System.Reflection.AssemblyName(assemblyName));
                                var type = loadedAssembly.GetType(typeName);

                                var help = GenerateHelp(contentDir, assembly, keys, type);
                                var helpFile = Path.Combine(helpDir, keys.Replace(';', '.') + ".hlp");

                                if (!Directory.Exists(helpDir))
                                {
                                    Directory.CreateDirectory(helpDir);
                                }
                                if (File.Exists(helpFile))
                                {
                                    Console.WriteLine($"File {helpFile} already exists - skipping!");
                                }
                                else {
                                    File.WriteAllLines(helpFile, help);
                                }
                            }
                        }
                    }
                }
                return 0;
            });

            return app.Execute(args);
        }

        static IEnumerable<string> GenerateHelp(string contentPath, string assembly, string keys, System.Type type)
        {
            var cmdletAttributes = type.GetTypeInfo().GetCustomAttributes().Where((a) => a.GetType().FullName == "System.Management.Automation.CmdletAttribute" || a.GetType().FullName == "System.Management.Automation.PSCmdletAttribute");
            dynamic cmdletAttribute = cmdletAttributes.FirstOrDefault();
            var commandName = String.Format("{0}-{1}", cmdletAttribute.VerbName, cmdletAttribute.NounName);

            var cmdlet = new Microsoft.CLU.InstalledCmdletInfo() { AssemblyName = assembly, CommandName = commandName, Keys = keys, Type = type };
            var help = Microsoft.CLU.Help.CmdletHelp.Generate(FormatParameterName, contentPath, assembly, cmdlet);

            return help;

        }
        /// <summary>
        /// Present the parameter names passed in using the syntax of the parser.
        /// </summary>
        /// <param name="names">The parameter names</param>
        /// <returns>The formatted parameter names</returns>
        /// <example>
        /// For DOS,  "(first,string),(second,int),(third,date),(a,null)" --> "/first:string,/second:int,/third:date,/a"
        /// For Unix, "first,second,third,a" --> "--first string ,--second int,--third date,-a"
        /// </example>
        public static string FormatParameterName(string name, string type, bool isMandatory, bool isPositional)
        {
            Func<string, string> nameFunc = n => n.Length == 1 ? "-" + n : "--" + n.ToLowerInvariant();

            var builder = new System.Text.StringBuilder();
            if (!isMandatory)
                builder.Append('[');
            if (isPositional)
                builder.Append('[');
            builder.Append(nameFunc(name));
            if (isPositional)
                builder.Append(']');
            if (!string.IsNullOrEmpty(type))
                builder.Append(' ').Append(type);
            if (!isMandatory)
                builder.Append(']');
            return builder.ToString();
        }


    }
}
