using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.CLU.Common
{
    /// <summary>
    /// Represents a Cmdlet details.
    /// </summary>
    internal class CmdletValue
    {
        /// <summary>
        /// Creates an instance of CmdletValue.
        /// </summary>
        /// <param name="commandDiscriminators">The command discriminators</param>
        /// <param name="cmdletIdentifier">The Cmdlet identifier</param>
        /// <param name="localPackage">The LocalPackage holding the Cmdlet</param>
        public CmdletValue(IEnumerable<string> commandDiscriminators, string cmdletIdentifier, LocalPackage localPackage)
        {
            Debug.Assert(commandDiscriminators != null);
            Debug.Assert(!string.IsNullOrEmpty(cmdletIdentifier));
            Debug.Assert(localPackage != null);

            CommandDiscriminators = commandDiscriminators;
            _cmdletIdentifier = cmdletIdentifier;
            Package = localPackage;
        }

        /// <summary>
        /// Lazy load the Cmdlet.
        /// </summary>
        /// <returns></returns>
        public Type LoadCmdlet()
        {
            if (_cmdlet == null)
            {
                var cmdletIdentifier = _cmdletIdentifier.Split(Constants.CmdletIndexItemValueSeparator);
                Debug.Assert(cmdletIdentifier.Length == 2);

                foreach (var assembly in Package.CommandAssemblies)
                {
                    if (String.Equals(assembly.GetName().Name, cmdletIdentifier[0], StringComparison.OrdinalIgnoreCase))
                    {
                        _cmdlet = assembly.GetExportedTypes().FirstOrDefault((t) => t.FullName == cmdletIdentifier[1]);
                        break;
                    }
                }
            }

            return _cmdlet;
        }


        /// <summary>
        /// The LocalPackage instance in which the Cmdlet exists.
        /// </summary>
        public LocalPackage Package
        {
            get; private set;
        }

        /// <summary>
        /// The command discriminators.
        /// </summary>
        public IEnumerable<string> CommandDiscriminators
        {
            get; private set;
        }

        #region private fields

        /// <summary>
        /// The Cmdlet identifier.
        /// </summary>
        private string _cmdletIdentifier;

        /// <summary>
        /// The Cmdlet type.
        /// </summary>
        private Type _cmdlet;

        #endregion
    }
}
