using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bcquery
{
    /// <summary>
    /// Class implements output to csv file.</summary>
    class CSVOutputProvider : IOutputProvider
    {
        private string filepath;

        /// <summary>
        ///Constructor for output provider.</summary>
        public CSVOutputProvider(string filepath)
        {
            this.filepath = filepath;
        }

        /// <summary>
        ///Method writes an array to csv file.</summary>
        public void WriteLine(string[] message)
        {
            string messageTmp = "";
            for (int i = 0; i < message.Length; i++)
            {
                messageTmp += String.Format("{0};", message[i]);
            }
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, true))
            {
                file.WriteLine(messageTmp);
            }
        }

        /// <summary>
        ///Method writes a message to csv file.</summary>
        public void WriteLine(string message)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, true))
            {
                file.WriteLine(String.Format("{0};", message));
            }
        }
    }
}
