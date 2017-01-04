# bcquery
Implementation of C# console application that reads blockchain data from a Bitcoin Unlimited client / Bitcoin Core client that is running on the same machine.

### Compiling executable

To compile executable use msbuild as follows
```
msbuild bcquery.sln
```

### Usage 

Usage: bcquery [OPTIONS]+
Bitcoin Client data directory query tool

GetBlocks - operation returns a list of blocks since a specified date. Each item in the list contains the block height, block hash, datetime created, transaction count.

GetBlockTransactions - operation returns a list of transactions of a specified block. Each item in the list contains the transaction hash, a list of inputs, a list of outputs. For each item in the input/output list, the item contains the address, the transaction hash of the input/output and the BTC amount.

GetAddressTransactions - operation returns a list of transactions (including unconfirmed transactions) of a specified address. Each item in the list contains the data file name, the transaction hash, a list of inputs, a list of outputs. For each item in the input/output list, the item contains the address, the transaction hash of the input/output and the BTC amount. It is expected that the total of all transactions results in the address balance that is shown on other services such as blockchain.info or blockr.io.

Options:


  -d, --datapath=VALUE       Bitcoin Client data directory full path.
  
  -o, --operation=VALUE      Operation to execute: GetBlocks,
                               GetBlockTransactions, GetAddressTransactions
                               
  -p, --parameter=VALUE      Operation parameter: GetBlocks - use date
                               (mm//dd//yyyy); GetBlockTransactions - use block
                               hash; GetAddressTransactions - use address.
                               
  -f, --file=VALUE           Path to output file (CSV).
  
  -h, --help                 Show this message and exit.
  
  
  Sample:
  ```
  bcquery.exe -d F:\bitcoin\blocks -o GetAddressTransactions -p 113zkRm1JGKjtHXFurgz7sRcvy9BMAeko1
  ```
