using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.CLU.Common
{
    /// <summary>
    /// Represents details of a package exists in local file system under packages root directory.
    /// </summary>
    internal class LocalPackage
    {

        /// <summary>
        /// Protected constructor ensure class instances created only via
        /// LocalPackage::Load* static methods.
        /// </summary>
        public LocalPackage(DirectoryInfo packageDirInfo, Assembly[] assemblies)
        {
            FullPath = packageDirInfo.FullName;
            _commandAssemblies = assemblies;
        }


        /// <summary>
        /// Absolute path to the package in the local file system under packages root directory.
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// Full path to lib directory for the current FX
        /// </summary>
        public string LibDirPath
        {
            get
            {
                return Path.Combine(FullPath, "lib", "dnxcore50");
            }
        }
        /// <summary>
        /// Full path to content directory of the package.
        /// </summary>
        public string ContentDirPath
        {
            get
            {
                return Path.Combine(FullPath, "content");
            }
        }

        /// <summary>
        /// Load and return reference to the default package assembly. A package may contains multiple
        /// assemblies, for e.g. assemblies with command implementations, assembly with name same as
        /// the package and more. This method loads the one with name same as package (if exists).
        /// </summary>
        /// <returns></returns>
        public Assembly DefaultAssembly
        {
            get { return _commandAssemblies.FirstOrDefault(); }
        }

        public IEnumerable<Assembly> CommandAssemblies
        {
            get
            {
                return _commandAssemblies;
            }
        }

        /// <summary>
        /// Backing field used in LoadDefaultAssembly method.
        /// </summary>
        private Assembly _defaultPackageAssembly;

        #region Private fields

        private Assembly[] _commandAssemblies;
        #endregion
    }
}