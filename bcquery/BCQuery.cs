using NBitcoin;
using NBitcoin.BitcoinCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LevelDB;
using System.IO;
using System.Threading;

namespace bcquery
{
    /// <summary>
    /// Class implements operations to query blockchain data.
    /// </summary>
    class BCQuery
    {
        /// <summary>
        /// Queue property used for background indexing.
        /// </summary>
        private ConcurrentQueue<KeyValuePair<byte[], byte[]>> queue = new ConcurrentQueue<KeyValuePair<byte[], byte[]>>();

        /// <summary>
        /// Index property, for faster access to block data.
        /// </summary>
        private IndexedBlockStore index;

        /// <summary>
        /// Main entry point, calls operations by provided parameters.
        /// </summary>
        /// <param name="datapath">Path to blockchain files.</param>
        /// <param name="file">Text output file for results in csv, if empty stdout is used.</param>
        /// <param name="operation">Operation name to call</param>
        /// <param name="parameter">Parameter for operation call - datetime, block hash or address</param>
        public void Execute(string datapath, string file, string operation, string parameter)
        {
            IOutputProvider outputProvider = new ConsoleOutputProvider(); 
            //check file
            if (!String.IsNullOrEmpty(file))
            { 
                try
                {
                    if(!new Helper().HasWritePermissionOnDir(file))
                    {
                        Console.Write(String.Format("ERROR 1: The file could not be written to the path specified {0}", file));
                        return;
                    }
                    outputProvider = new CSVOutputProvider(file);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(String.Format("ERROR 2: {0}",ex.Message));
                    return;
                }
            }

            //check datapath
            if (new Helper().GetFiles(datapath, new BlockStore(datapath, Network.Main).FileRegex).Count()<=0)
            {
                Console.Write(String.Format("ERROR 3: Blockchain files are not available in: {0}", datapath));
                return;
            }

            //check operation
            if (!operation.ToLower().Equals("GetBlocks".ToLower())&&
                !operation.ToLower().Equals("GetBlockTransactions".ToLower())&&
                !operation.ToLower().Equals("GetAddressTransactions".ToLower()))
            {
                Console.Write(String.Format("ERROR 4: Unknown operation {0}", operation));
                return;
            }

            //execution poin GetBlocks
            if (operation.ToLower().Equals("GetBlocks".ToLower()))
            {
                DateTime dateFrom = new DateTime();
                if (DateTime.TryParse(parameter, out dateFrom))
                {
                    try
                    {
                        GetBlocks(datapath, dateFrom, outputProvider);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(String.Format("ERROR 2: {0}", ex.Message));
                        return;
                    }
                }
                else
                {
                    Console.Write(String.Format("ERROR 5: Invalid date {0}", parameter));
                    return;
                }
            }

            //execution poin GetBlockTransactions
            if (operation.ToLower().Equals("GetBlockTransactions".ToLower()))
            {
                if (parameter.Length == 64)
                {
                    try
                    {
                        GetBlockTransactions(datapath, parameter, outputProvider);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(String.Format("ERROR 2: {0}", ex.Message));
                        return;
                    }
                }
                else
                {
                    Console.Write(String.Format("ERROR 6: Invalid block hash {0}", parameter));
                    return;
                }
            }

            //execution point GetAddressTransactions
            if (operation.ToLower().Equals("GetAddressTransactions".ToLower()))
            {
                if (parameter.Length == 34)
                {
                    try
                    {
                        GetAddressTransactions(datapath, parameter, outputProvider);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(String.Format("ERROR 2: {0}", ex.Message));
                        return;
                    }
                }
                else
                {
                    Console.Write(String.Format("ERROR 7: Invalid address {0}", parameter));
                    return;
                }
            }
        }

        /// <summary>
        /// Method for background indexing used for faster access to blocks and transactions.
        /// </summary>
        public void WatchQueue()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bcquery");
            var dbOptions = new Options();

            dbOptions.CreateIfMissing = true;
            dbOptions.Compression = CompressionType.SnappyCompression;

            using (DB storage = DB.Open(dbPath, dbOptions))
            {
                bool stop = false;
                int stopCounter = 0;
                while (!stop)
                {
                    while (!queue.IsEmpty)
                    {
                        KeyValuePair<byte[], byte[]> queueItem = new KeyValuePair<byte[], byte[]>();

                        if (queue.TryDequeue(out queueItem))
                        {
                            if (storage.Get(ReadOptions.Default, queueItem.Key).ToArray().Length==0)
                            {
                                storage.Put(WriteOptions.Default, queueItem.Key, queueItem.Value);
                            }
                        }
                    }
                    stopCounter++;
                    Thread.Sleep(1000);
                    if (stopCounter>10)
                    {
                        stop = true;
                    }
                }
            }
        }

        /// <summary>
        /// Method used to scan all blockchain files, identify transactions and execute callback method for each transaction.
        /// </summary>
        /// <param name="datapath">Path to blockchain files.</param>
        /// <param name="callback">Callback closure method, executed for each found transaction.</param>
        public void Runner(string datapath, Action<StoredBlock, Transaction> callback)
        {
            ConcurrentBag<Task> listOftasks = new ConcurrentBag<Task>();
            BlockStore store = new BlockStore(datapath, Network.Main);
            foreach (var file in new Helper().GetFiles(datapath, store.FileRegex).OrderByDescending(f => f.FullName))
            {
                listOftasks.Add(Task.Run(() =>
                {
                    foreach (var transaction in store.EnumerateFile(file).SelectMany(block => block.Item.Transactions, (block, transactions) => new { block, transactions }))
                    {
                        callback(transaction.block, transaction.transactions);
                        queue.Enqueue(new KeyValuePair<byte[], byte[]>(transaction.transactions.GetHash().ToBytes(), transaction.block.Item.GetHash().ToBytes()));
                        queue.Enqueue(new KeyValuePair<byte[], byte[]>(transaction.block.Item.GetHash().ToBytes(), System.Text.Encoding.UTF8.GetBytes(file.FullName)));
                    }
                }));
            }
            Task.WaitAll(listOftasks.ToArray());
        }

        /// <summary>
        /// Method populates index for faster block access.
        /// </summary>
        public void PrepareIndex(string datapath)
        {
            BlockStore store = new BlockStore(datapath, Network.Main);
            index = new IndexedBlockStore(new InMemoryNoSqlRepository(), store);
            index.ReIndex();
        }

        /// <summary>
        /// Method returns a list of blocks since a specified datetime.
        /// </summary>
        /// <param name="datapath">Path to blockchain files.</param>
        /// <param name="fromDate">Date used to filter blocks.</param>
        /// <param name="outputProvider">Output handler type - CSV file or console.</param>
        public void GetBlocks(string datapath, DateTime fromDate, IOutputProvider outputProvider)
        {
            ConcurrentDictionary<uint256, bool> check = new ConcurrentDictionary<uint256, bool>();

            Action<StoredBlock, Transaction> callback = (block, transaction) => {
                if (!check.ContainsKey(block.Item.GetHash()) && block.Item.Header.BlockTime.DateTime.Date >= fromDate.Date)
                {
                    check[block.Item.GetHash()] = true;
          
                    string[] message = new string[4];
                    message[0] = "";
                    message[1] = block.Item.GetHash().ToString();
                    message[2] = String.Format("{0} {1}",block.Item.Header.BlockTime.DateTime.ToShortDateString(), block.Item.Header.BlockTime.DateTime.ToShortTimeString());
                    message[3] = block.Item.Transactions.Count.ToString();
                    outputProvider.WriteLine(message);
                }
            };
            Thread queueThread = new Thread(() => {
                WatchQueue();
            });
            Thread runnerThread = new Thread(() =>
            {
                Runner(datapath, callback);
            });

            runnerThread.Start();
            queueThread.Start();
            runnerThread.Join();
            queueThread.Join();
        }

        /// <summary>
        /// Method returns a list of transactions of a specified block.
        /// </summary>
        /// <param name="datapath">Path to blockchain files.</param>
        /// <param name="hash">Hex string of block hash used as filter. </param>
        /// <param name="outputProvider">Output handler type - CSV file or console.</param>
        public void GetBlockTransactions(string datapath, string hash, IOutputProvider outputProvider)
        {
            Action<StoredBlock, Transaction> callback = (block, transaction) => { };
            Thread queueThread = new Thread(() => {
                WatchQueue();
            });
            Thread runnerThread = new Thread(() =>
            {
                Runner(datapath, callback);
            });
            Thread indexerThread = new Thread(() =>
            {
                PrepareIndex(datapath);
            });

            indexerThread.Start();
            runnerThread.Start();
            queueThread.Start();
            runnerThread.Join();
            queueThread.Join();
            indexerThread.Join();

            uint256 blockhash;
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bcquery");
            var dbOptions = new Options();

            dbOptions.CreateIfMissing = true;
            dbOptions.Compression = CompressionType.SnappyCompression;

            using (DB storage = DB.Open(dbPath, dbOptions))
            {
                if (uint256.TryParse(hash, out blockhash))
                {
                    foreach (var transaction in index.Get(blockhash).Transactions)
                    {
                        foreach (var input in transaction.Inputs)
                        {
                            if (input.PrevOut.Hash != uint256.Zero)
                            {
                                Block tempBlock = index.Get(new uint256(storage.Get(ReadOptions.Default, input.PrevOut.Hash.ToBytes()).ToArray()));
                                Money btcAmount = tempBlock.Transactions.Where(tx => tx.GetHash() == input.PrevOut.Hash).SelectMany(tx => tx.Outputs).ToArray()[input.PrevOut.N].Value;
                                string[] message = new string[6];
                                message[0] = transaction.GetHash().ToString();
                                message[1] = System.Text.Encoding.UTF8.GetString(storage.Get(ReadOptions.Default, tempBlock.GetHash().ToBytes()).ToArray());
                                message[2] = "INPUT";
                                message[3] = String.Format("{0}", input.ScriptSig.GetDestinationAddress(Network.Main));
                                message[3] = message[3].Trim().Length == 0 ? "<NOT IDENTIFIED>" : message[3];
                                message[4] = input.PrevOut.Hash.ToString();
                                message[5] = btcAmount.ToString();
                                outputProvider.WriteLine(message);
                            }
                        }
                        foreach (var output in transaction.Outputs)
                        {
                            string[] message = new string[6];
                            message[0] = transaction.GetHash().ToString(); 
                            message[1] = System.Text.Encoding.UTF8.GetString(storage.Get(ReadOptions.Default, blockhash.ToBytes()).ToArray());
                            message[2] = "OUTPUT";
                            message[3] = String.Format("{0}", output.ScriptPubKey.GetDestinationAddress(Network.Main));
                            message[3] = message[3].Trim().Length == 0 ? "<NOT IDENTIFIED>" : message[3];
                            message[4] = transaction.GetHash().ToString();
                            message[5] = output.Value.ToString();
                            outputProvider.WriteLine(message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method returns a list of transactions (including unconfirmed transactions) of a specified address.
        /// </summary>
        /// <param name="datapath">Path to blockchain files.</param>
        /// <param name="address">Hex string of address used as filter. </param>
        /// <param name="outputProvider">Output handler type - CSV file or console.</param>
        public void GetAddressTransactions(string datapath, string address, IOutputProvider outputProvider)
        {
            ConcurrentDictionary<uint256, bool> transactions = new ConcurrentDictionary<uint256, bool>();
            Action<StoredBlock, Transaction> callback = (block, transaction) => {
                if(transaction.Inputs.Exists(i=>String.Format("{0}",i.ScriptSig.GetDestinationAddress(Network.Main))==address))
                {
                    transactions[transaction.Inputs.Where(i=> String.Format("{0}", i.ScriptSig.GetDestinationAddress(Network.Main)) == address).Select(i=>i.PrevOut.Hash).First()] = true;
                }
                if (transaction.Outputs.Exists(o => String.Format("{0}", o.ScriptPubKey.GetDestinationAddress(Network.Main)) == address))
                {
                    transactions[transaction.GetHash()] = true;
                }
            };

            Thread queueThread = new Thread(() => {
                WatchQueue();
            });
            Thread runnerThread = new Thread(() =>
            {
                Runner(datapath, callback);
            });
            Thread indexerThread = new Thread(() =>
            {
                PrepareIndex(datapath);
            });

            indexerThread.Start();
            runnerThread.Start();
            queueThread.Start();
            runnerThread.Join();
            queueThread.Join();
            indexerThread.Join();

            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bcquery");
            var dbOptions = new Options();

            dbOptions.CreateIfMissing = true;
            dbOptions.Compression = CompressionType.SnappyCompression;

            using (DB storage = DB.Open(dbPath, dbOptions))
            {
                foreach (var txhash in transactions.Keys)
                {
                    var blockhash =new uint256(storage.Get(ReadOptions.Default, txhash.ToBytes()).ToArray());
                    var transaction = index.Get(blockhash).Transactions.Where(tx => tx.GetHash() == txhash).Select(tx=>tx).First();
                    foreach (var input in transaction.Inputs)
                    {
                        if (input.PrevOut.Hash != uint256.Zero)
                        {
                            Block tempBlock = index.Get(new uint256(storage.Get(ReadOptions.Default, input.PrevOut.Hash.ToBytes()).ToArray()));
                            Money btcAmount = tempBlock.Transactions.Where(tx => tx.GetHash() == input.PrevOut.Hash).SelectMany(tx => tx.Outputs).ToArray()[input.PrevOut.N].Value;
                            string[] message = new string[6];
                            message[0] = transaction.GetHash().ToString();
                            message[1] = System.Text.Encoding.UTF8.GetString(storage.Get(ReadOptions.Default, tempBlock.GetHash().ToBytes()).ToArray());
                            message[2] = "INPUT";
                            message[3] = String.Format("{0}", input.ScriptSig.GetDestinationAddress(Network.Main));
                            message[3] = message[3].Trim().Length == 0 ? "<NOT IDENTIFIED>" : message[3];
                            message[4] = input.PrevOut.Hash.ToString();
                            message[5] = btcAmount.ToString();
                            outputProvider.WriteLine(message);
                        }
                    }
                    foreach (var output in transaction.Outputs)
                    {
                        string[] message = new string[6];
                        message[0] = transaction.GetHash().ToString();
                        message[1] = System.Text.Encoding.UTF8.GetString(storage.Get(ReadOptions.Default, blockhash.ToBytes()).ToArray());
                        message[2] = "OUTPUT";
                        message[3] = String.Format("{0}", output.ScriptPubKey.GetDestinationAddress(Network.Main));
                        message[3] = message[3].Trim().Length == 0 ? "<NOT IDENTIFIED>" : message[3];
                        message[4] = transaction.GetHash().ToString();
                        message[5] = output.Value.ToString();
                        outputProvider.WriteLine(message);
                    }
                }
            }
        }
    }
}
