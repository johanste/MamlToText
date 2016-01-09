using Microsoft.CLU.Common;
using Microsoft.CLU.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Microsoft.CLU.Help
{
    /// <summary>
    /// Generates Cmdlet command help.
    /// </summary>
    internal static class CmdletHelp
    {
        /// <summary>
        /// Generates a list of text lines containing help information for a specific Cmdlet command.
        /// </summary>
        /// <param name="format">The command help formattter</param>
        /// <param name="args">The command-line arguments to be considered in the help logic.</param>
        /// <param name="prefix">True if the help argument comes first, false if last.</param>
        /// <returns>A list of lines containing help information.</returns>
        /// <remarks>
        /// This should be called for generating help for a commands, for example,
        /// 
        ///     azure help vm start     (prefix = true)
        /// 
        /// In the prefix form, if the arguments provide sufficient detail to identify a single command, 
        /// the details about that command are listed. If more than one command is found, they are listed.
        /// 
        ///     azure vm start --help   (prefix = false)
        ///
        /// In the postfix form, there will never be a command list. Rather, if the arguments match an
        /// existing command, help for only that command is displayed. If none match the arguments exactly,
        /// an error is generated.
        /// </remarks>
        public static IEnumerable<string> Generate(Func<string, string, bool, bool, string> format, string contentRootPath, string assembly, InstalledCmdletInfo cmdlet)
        {
            var result = new List<string>();
            MAMLReader.ReadMAMLFile(contentRootPath, assembly, new InstalledCmdletInfo[] { cmdlet });
            AddReflectionCmdletInfo(cmdlet);
            GenerateSingleCommandHelp(format, result, cmdlet);
            return result;
        }

        private static void GenerateSingleCommandHelp(Func<string, string, bool, bool, string> formatter, List<string> result, InstalledCmdletInfo cmdlet)
        {
            var info = cmdlet.Info;

            if (string.IsNullOrEmpty(info.Brief))
            {
                result.Add("");
                result.Add($"{info.Name}");
            }
            else
            {
                result.Add("");
                result.Add($"{info.Name}: {info.Brief}");
            }

            if (info.Description.Count > 0)
            {
                result.Add("");

                foreach (var descLine in info.Description)
                {
                    result.Add("");
                    result.Add($"{descLine}");
                }
            }

            result.Add("");
            result.Add("");
            result.Add("Command syntax");

            var parameters = new Dictionary<string, MAMLReader.ParameterHelpInfo>();

            foreach (var pset in info.ParameterSets)
            {
                result.Add("");

                var builder = new System.Text.StringBuilder();
                builder.Append("az").Append(' ').Append(info.Keys.Replace(';', ' '));

                foreach (var p in pset.Parameters)
                {
                    if (!parameters.ContainsKey(p.Name))
                        parameters.Add(p.Name, p);
                    var formattedName = formatter(p.Name, MapTypeName(p.Type), p.IsMandatory, p.Position != int.MaxValue);
                    builder.Append(' ').Append(formattedName);
                }
                result.Add(builder.ToString());
            }

            AddReflectionParameterInfo(cmdlet, parameters);

            if (parameters.Values.Count > 0)
            {
                result.Add("");
                result.Add("");
                result.Add("Parameters");
                result.Add("");

                bool hasDescriptions = parameters.Values.SelectMany(p => p.Description).Any();

                foreach (var p in parameters.Values)
                {
                    var bldr = new StringBuilder(formatter(p.Name, null, true, false));
                    var aliases = p.Aliases != null ? string.Join(", ", p.Aliases.Select(a => formatter(a, null, true, false))) : null;
                    if (!string.IsNullOrEmpty(aliases))
                    {
                        bldr.Append(", ").Append(aliases);
                    }
                    result.Add(bldr.ToString());

                    foreach (var desc in p.Description)
                    {
                        result.Add(desc);
                    }

                    if (hasDescriptions)
                        result.Add("");
                }
            }

            result.Add("");
        }

        private static void AddReflectionCmdletInfo(InstalledCmdletInfo cmdlet)
        {
            if (cmdlet.Info == null)
            {
                var typeMetadata = new TypeMetadata(cmdlet.Type);
                typeMetadata.Load();

                cmdlet.Info = new MAMLReader.CommandHelpInfo { Name = cmdlet.CommandName, Keys = cmdlet.Keys };

                if (typeMetadata.ParameterSets != null && typeMetadata.ParameterSets.Count > 0)
                {
                    foreach (var pSet in typeMetadata.ParameterSets)
                    {
                        var psetInfo = new MAMLReader.ParameterSetHelpInfo();
                        cmdlet.Info.ParameterSets.Add(psetInfo);
                        psetInfo.Parameters.AddRange(pSet.Parameters
                            .Select(p => new MAMLReader.ParameterHelpInfo
                            {
                                Name = p.Name,
                                Aliases = p.Aliases != null ? p.Aliases.ToArray() : null,
                                IsMandatory = p.IsMandatory,
                                Position = p.Position,
                                Type = p.ParameterType.Name
                            }));
                    }
                }
                else
                {
                    var psetInfo = new MAMLReader.ParameterSetHelpInfo();
                    cmdlet.Info.ParameterSets.Add(psetInfo);
                    psetInfo.Parameters.AddRange(typeMetadata.Parameters.Values.Where(p => !p.IsBuiltin)
                        .Select(p => new MAMLReader.ParameterHelpInfo
                        {
                            Name = p.Name,
                            Aliases = p.Aliases != null ? p.Aliases.ToArray() : null,
                            IsMandatory = p.IsMandatory(null),
                            Position = p.Position(null),
                            Type = p.ParameterType.Name
                        }));
                }
            }
        }

        private static void AddReflectionParameterInfo(InstalledCmdletInfo cmdlet, Dictionary<string, MAMLReader.ParameterHelpInfo> parameters)
        {
            var typeMetadata = new TypeMetadata(cmdlet.Type);
            typeMetadata.Load();

            foreach (var p in parameters)
            {
                ParameterMetadata metadata;
                if (typeMetadata.Parameters.TryGetValue(p.Key.ToLowerInvariant(), out metadata))
                {
                    p.Value.Aliases = metadata.Aliases.ToArray();
                }
            }
        }

        private static string MapTypeName(string name)
        {
            switch (name)
            {
                case "SwitchParameter":
                    return null;
                case "Boolean":
                    return "<bool>";
                case "Byte":
                    return "<byte>";
                case "Int32":
                    return "<int>";
                case "Int64":
                    return "<long>";
                case "Char":
                case "String":
                    return $"<{name.ToLower()}>";
                default:
                    return $"<{name}>";
            }
        }
    }
}
