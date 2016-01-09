using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.CLU.Common
{
    /// <summary>
    /// Represents details of a package (containing cmdlets) exists in local file system under packages root directory.
    /// </summary>
    internal class CmdletLocalPackage : LocalPackage
    {
        /// <summary>
        /// Create an instance of CmdletLocalPackage.
        /// </summary>
        /// <param name="packageBaseName">The base name of the package, i.e. the name without version number</param>
        /// <param name="packageDirInfo">DirectoryInfo of this package</param>
        internal CmdletLocalPackage(DirectoryInfo packageDirInfo, Assembly[] assemblies) : base(packageDirInfo, assemblies)
        { }

        /// <summary>
        /// Find the Cmdlet corrosponding to the given command discriminators.
        /// e.g. Suppose the command discriminators are [ 'vm', 'image', 'list'], if this package
        /// contains Cmdlet (AzureVMImageList) corrosponding to this command discriminators then
        /// the method returns a CmdletValue instance representing AzureVMImageList cmdlet.
        /// </summary>
        /// <param name="commandDiscriminators">The command discriminators</param>
        /// <returns>CmdletValue instance representing Cmdlet for the given discriminators</returns>
        public CmdletValue FindCmdlet(IEnumerable<string> commandDiscriminators)
        {
            Debug.Assert(commandDiscriminators != null);
            Debug.Assert(commandDiscriminators.Count() >= 1);
            var index = LoadFromCache("");
            if (index != null)
            {
                string key = string.Join(Constants.CmdletIndexWordSeparator, commandDiscriminators);
                string cmdletIdentifier;
                if (index.Entries.TryGetValue(key, out cmdletIdentifier))
                {
                    return new CmdletValue(commandDiscriminators, cmdletIdentifier, this);
                }
            }

            return null;
        }

        public IEnumerable<CmdletValue> FindMatchingCommandlets(IEnumerable<string> commandDiscriminators, bool matchPartialWord)
        {
            var cache = LoadFromCache("");
            var concatenatedCommandDiscriminators = string.Join(";", commandDiscriminators);
            if (!matchPartialWord)
            {
                concatenatedCommandDiscriminators += ";";
            }
            var matchingEntries = cache.Entries.Where((entry) => entry.Key.StartsWith(concatenatedCommandDiscriminators));

            return matchingEntries.Select((entry) =>
            {
                return new CmdletValue(entry.Key.Split(';'), entry.Value, this);
            });
        }

        /// <summary>
        /// Load an index of this package identified by the given index name.
        /// </summary>
        /// <param name="indexName">The name of the index to load</param>
        /// <returns>The index</returns>
        private CmdletIndex LoadFromCache(string indexName)
        {
            var indexFilePath = Path.Combine(FullPath, "_indexes", indexName);
            CmdletIndex index;
            if (!_cache.TryGetValue(indexFilePath, out index))
            {
                index = CmdletIndex.Load(new string[] { indexName }, indexFilePath, false);
                _cache.Add(indexFilePath, index);
            }

            return index;
        }

        #region Private fields

        /// <summary>
        /// The lock for cache while running in mutli-threaded mode.
        /// </summary>
        private static object _cachelock = new object();

        /// <summary>
        /// Cache of loaded command indicies.
        /// The cache is shared across all instances of CmdletLocalPackage.
        /// </summary>
        private static IDictionary<string, CmdletIndex> _cache = new Dictionary<string, CmdletIndex>();

        #endregion
    }
}
