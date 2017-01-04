using NBitcoin.BitcoinCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace bcquery
{
    /// <summary>
    /// Class contains misc utility methods.
    /// </summary>
    class Helper
    {
        /// <summary>
        /// Method used to enumerate files in specified folder, filtered by provided regular expression.
        /// </summary>
        /// <param name="folder">Foler path.</param>
        /// <param name="FileRegex">Regular expression used as filter.</param>
        /// <returns>
        /// Enumeration of found files.</returns>
        public IEnumerable<FileInfo> GetFiles(string folder, Regex FileRegex)
        {
            foreach (var file in new DirectoryInfo(folder).GetFiles().OrderBy(f => f.Name))
            {
                var fileIndex = GetFileIndex(file.Name, FileRegex);
                if (fileIndex < 0)
                    continue;
                yield return file;
                
            }
        }

        /// <summary>
        /// Method used to get index number of provided file name.
        /// </summary>
        /// <param name="fileName">File name used to get information.</param>
        /// <param name="FileRegex">Regular expression used as filter.</param>
        /// <returns>
        /// Integer number, index of file.</returns>
        private int GetFileIndex(string fileName, Regex FileRegex)
        {
            var match = FileRegex.Match(fileName);
            if (!match.Success)
                return -1;
            return int.Parse(match.Groups[1].Value);
        }

        /// <summary>
        /// Method checks if path is available for file write operations.
        /// </summary>
        /// <param name="fullpath">Path to check</param>
        /// <returns>
        /// True if available, false if not available.</returns>
        public bool HasWritePermissionOnDir(string fullpath)
        {
            string path = Path.GetFileName(Path.GetDirectoryName(fullpath));

            var writeAllow = false;
            var writeDeny = false;
            var accessControlList = Directory.GetAccessControl(path);
            if (accessControlList == null)
                return false;
            var accessRules = accessControlList.GetAccessRules(true, true,
                                        typeof(System.Security.Principal.SecurityIdentifier));
            if (accessRules == null)
                return false;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;

                if (rule.AccessControlType == AccessControlType.Allow)
                    writeAllow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    writeDeny = true;
            }

            return writeAllow && !writeDeny;
        }



    }
}
