using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bcquery
{
    /// <summary>
    /// Class implements output to stdout.</summary>
    class ConsoleOutputProvider : IOutputProvider
    {
        /// <summary>
        ///Method writes an array to stdout.</summary>
        public void WriteLine(string[] message)
        {
            string messageTmp = "";
            for (int i = 0; i < message.Length; i++)
            {
                messageTmp += String.Format("{0} ", message[i]);
            }
            Console.WriteLine(messageTmp.Trim());
        }

        /// <summary>
        ///Method writes a message to stdout.</summary>
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
