using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bcquery
{
    /// <summary>
    /// Interface for generic output providers.</summary>
    interface IOutputProvider
    {
        /// <summary>
        /// Writes an array.
        /// </summary>
        void WriteLine(string[] message);
        
        /// <summary>
        /// Writes a message.
        /// </summary>
        void WriteLine(string message);
    }
}
