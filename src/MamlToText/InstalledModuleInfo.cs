using Microsoft.CLU.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CLU
{
    internal class InstalledModuleInfo
    {
        public LocalPackage Package { get; set; }
        public IList<InstalledCmdletInfo> Cmdlets { get; set; }

        public static IEnumerable<InstalledModuleInfo> Enumerate(CmdletLocalPackage package, IEnumerable<string> commandDiscriminators)
        {
            var installedModuleInfos = new List<InstalledModuleInfo>();

            var matchedCmdlets = package.FindMatchingCommandlets(commandDiscriminators, false);
            if (matchedCmdlets.Count() > 0)
            {
                var module = new InstalledModuleInfo { Package = package, Cmdlets = new List<InstalledCmdletInfo>() };
                foreach (var entry in matchedCmdlets)
                {
       
                    module.Cmdlets.Add(new InstalledCmdletInfo
                    {
                        Keys = String.Join(";", entry.CommandDiscriminators),
                        AssemblyName = entry.Package.DefaultAssembly.GetName().Name,
                        Type = entry.LoadCmdlet()
                    });
                }

                installedModuleInfos.Add(module);
            }

            return installedModuleInfos;
        }
    }

    internal class InstalledCmdletInfo
    {
        public string Keys { get; set; }
        public string CommandName { get; set; }
        public string AssemblyName { get; set; }
        public Type Type { get; set; }
        public Help.MAMLReader.CommandHelpInfo Info { get; set; }
    }
}
