using System;
using NDesk.Options;
using System.Text;
using System.Collections.Generic;

namespace bcquery
{
    class Program
    {
        static void Main(string[] args)
        {
            bool showhelp = false;
            string datapath = "";
            string file = "";
            string operation = "";
            string parameter = "";

            var options = new OptionSet()
            {
                { "d|datapath=","Bitcoin Client data directory full path.",item=>datapath=item},
                { "o|operation=", "Operation to execute: GetBlocks, GetBlockTransactions, GetAddressTransactions", item=>operation=item},
                { "p|parameter=","Operation parameter: GetBlocks - use date (mm//dd//yyyy); GetBlockTransactions - use block hash; GetAddressTransactions - use address.", item=>parameter=item},
                { "f|file=","Path to output file (CSV).",item=>file=item},
                { "h|help", "Show this message and exit.", item=> { showhelp=(item!=null); } }
            };

            List<string> arguments = options.Parse(args);

            if (showhelp || (String.IsNullOrEmpty(operation) && String.IsNullOrEmpty(parameter)))
            {
                ShowHelp(options);
                return;
            }

            new BCQuery().Execute(datapath, file, operation, parameter);
        }

        static void ShowHelp(OptionSet options)
        {
            StringBuilder builder = new StringBuilder();
            var header = builder.AppendLine("Usage: bcquery [OPTIONS]+")
                   .AppendLine("Bitcoin Client data directory query tool")
                   .AppendLine("")
                   .AppendLine("GetBlocks - operation returns a list of blocks since a specified date. Each item in the list contains the block height, block hash, datetime created, transaction count.")
                   .AppendLine("")
                   .AppendLine("GetBlockTransactions - operation returns a list of transactions of a specified block. Each item in the list contains the transaction hash, a list of inputs, a list of outputs. For each item in the input/output list, the item contains the address, the transaction hash of the input/output and the BTC amount.")
                   .AppendLine("")
                   .AppendLine("GetAddressTransactions - operation returns a list of transactions (including unconfirmed transactions) of a specified address. Each item in the list contains the data file name, the transaction hash, a list of inputs, a list of outputs. For each item in the input/output list, the item contains the address, the transaction hash of the input/output and the BTC amount. It is expected that the total of all transactions results in the address balance that is shown on other services such as blockchain.info or blockr.io.")
                   .AppendLine("")
                   .AppendLine("Options:")
                   .ToString();
            Console.WriteLine(header);
            options.WriteOptionDescriptions(Console.Out);
        }
    }
}
