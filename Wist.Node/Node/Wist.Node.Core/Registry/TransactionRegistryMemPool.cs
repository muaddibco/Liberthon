using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Timers;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.ExtensionMethods;
using Wist.Core.Identity;
using Wist.Core.Logging;
using Wist.Core.States;
using Wist.Core.Synchronization;
using Wist.BlockLattice.Core;
using Wist.BlockLattice.Core.DataModel.Registry.SourceKeys;
using Wist.Core.HashCalculations;
using System.Runtime.CompilerServices;

namespace Wist.Node.Core.Registry
{
    //TODO: add performance counter for measuring MemPool size

    /// <summary>
    /// MemPool is needed for following purposes:
    ///  1. Source for building transactions registry block
    ///  2. Repository for comparing transactions registry key arrived with transactions registry block from another participant
    ///  
    ///  When created Transaction Registry Block gets approved by corresponding node from Sync layer transaction enumerated there must be removed from the Pool
    /// </summary>
    [RegisterDefaultImplementation(typeof(IRegistryMemPool), Lifetime = LifetimeManagement.Singleton)]
    public class TransactionRegistryMemPool : IRegistryMemPool
    {
        private readonly Dictionary<ulong, Dictionary<ITransactionSourceKey, List<IKey>>> _transactionKeyBySourceKeys;
        private readonly Dictionary<ulong, SortedDictionary<int, ITransactionRegistryBlock>> _transactionRegisterBlocksOrdered;
        private readonly Dictionary<ulong, Dictionary<IKey, int>> _transactionOrderByTransactionKey;
        private readonly Dictionary<ulong, Dictionary<IKey, ITransactionSourceKey>> _transactionSourceKeyByTransactionKey;
        private readonly Dictionary<ulong, int> _transactionsIndicies;

        // Key of this dictionary is hash of concatenation of Public Key of sender and Height of transaction

        private readonly Dictionary<ulong, Dictionary<ulong, HashSet<RegistryShortBlock>>> _transactionsShortBlocks;
        private readonly IIdentityKeyProvider _transactionHashKey;
        private readonly ICryptoService _cryptoService;
        private readonly ITransactionsRegistryHelper _transactionsRegistryHelper;
        private readonly ILogger _logger;
        //private readonly Timer _timer;
        private readonly ISynchronizationContext _synchronizationContext;
        private readonly IHashCalculation _hashCalculation;
        private int _oldValue;
        private readonly object _sync = new object();

        public TransactionRegistryMemPool(ILoggerService loggerService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, ICryptoService cryptoService, 
            IStatesRepository statesRepository, ITransactionsRegistryHelper transactionsRegistryHelper, IHashCalculationsRepository hashCalculationsRepository)
        {
            _oldValue = 0;
            //_timer = new Timer(1000);
            //_timer.Elapsed += (s, e) =>
            //{
            //    if (_synchronizationContext.LastBlockDescriptor != null && _transactionRegisterBlocksOrdered.ContainsKey(_synchronizationContext.LastBlockDescriptor.BlockHeight))
            //    {
            //        _logger.Info($"MemPoolCount total = {_transactionRegisterBlocksOrdered[_synchronizationContext.LastBlockDescriptor.BlockHeight].Count};  delta = {_transactionRegisterBlocksOrdered[_synchronizationContext.LastBlockDescriptor.BlockHeight].Count - _oldValue}");
            //        _oldValue = _transactionRegisterBlocksOrdered[_synchronizationContext.LastBlockDescriptor.BlockHeight].Count;
            //    }
            //};
            //_timer.Start();

            _transactionHashKey = identityKeyProvidersRegistry.GetTransactionsIdenityKeyProvider();
            _logger = loggerService.GetLogger(nameof(TransactionRegistryMemPool));
            _transactionsIndicies = new Dictionary<ulong, int>();
            _transactionRegisterBlocksOrdered = new Dictionary<ulong, SortedDictionary<int, ITransactionRegistryBlock>>();
            _transactionKeyBySourceKeys = new Dictionary<ulong, Dictionary<ITransactionSourceKey, List<IKey>>>();
            _transactionsShortBlocks = new Dictionary<ulong, Dictionary<ulong, HashSet<RegistryShortBlock>>>();
            _transactionOrderByTransactionKey = new Dictionary<ulong, Dictionary<IKey, int>>();
            _transactionSourceKeyByTransactionKey = new Dictionary<ulong, Dictionary<IKey, ITransactionSourceKey>>();
            _cryptoService = cryptoService;
            _transactionsRegistryHelper = transactionsRegistryHelper;
            _synchronizationContext = statesRepository.GetInstance<ISynchronizationContext>();
            _hashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
        }

        public bool EnqueueTransactionRegisterBlock(ITransactionRegistryBlock transactionRegisterBlock)
        {
            lock (_sync)
            {
                AscertainRoundInitialized(transactionRegisterBlock.SyncBlockHeight);

                IKey transactionTwiceHashedKey = _transactionsRegistryHelper.GetTransactionRegistryTwiceHashedKey(transactionRegisterBlock);


                if (!_transactionOrderByTransactionKey[transactionRegisterBlock.SyncBlockHeight].ContainsKey(transactionTwiceHashedKey))
                {
                    _transactionRegisterBlocksOrdered[transactionRegisterBlock.SyncBlockHeight].Add(_transactionsIndicies[transactionRegisterBlock.SyncBlockHeight], transactionRegisterBlock);
                    _transactionOrderByTransactionKey[transactionRegisterBlock.SyncBlockHeight].Add(transactionTwiceHashedKey, _transactionsIndicies[transactionRegisterBlock.SyncBlockHeight]);

                    if (!_transactionKeyBySourceKeys[transactionRegisterBlock.SyncBlockHeight].ContainsKey(transactionRegisterBlock.TransactionSourceKey))
                    {
                        _transactionKeyBySourceKeys[transactionRegisterBlock.SyncBlockHeight].Add(transactionRegisterBlock.TransactionSourceKey, new List<IKey>());
                    }

                    _transactionKeyBySourceKeys[transactionRegisterBlock.SyncBlockHeight][transactionRegisterBlock.TransactionSourceKey].Add(transactionTwiceHashedKey);
                    _transactionSourceKeyByTransactionKey[transactionRegisterBlock.SyncBlockHeight].Add(transactionTwiceHashedKey, transactionRegisterBlock.TransactionSourceKey);

                    _transactionsIndicies[transactionRegisterBlock.SyncBlockHeight]++;
                    return true;
                }
            }

            return false;
        }

        public bool EnqueueTransactionsShortBlock(RegistryShortBlock transactionsShortBlock)
        {
            lock (_sync)
            {
                AscertainRoundInitialized(transactionsShortBlock.SyncBlockHeight);

                if (!_transactionsShortBlocks[transactionsShortBlock.SyncBlockHeight].ContainsKey(transactionsShortBlock.BlockHeight))
                {
                    _transactionsShortBlocks[transactionsShortBlock.SyncBlockHeight].Add(transactionsShortBlock.BlockHeight, new HashSet<RegistryShortBlock>());
                }

                return _transactionsShortBlocks[transactionsShortBlock.SyncBlockHeight][transactionsShortBlock.BlockHeight].Add(transactionsShortBlock);
            }
        }

        public byte[] GetConfidenceMask(RegistryShortBlock transactionsShortBlock, out byte[] bitMask)
        {
            lock (_sync)
            {
                bool[] bools = transactionsShortBlock.TransactionHeaderHashes.Select(kvp => _transactionOrderByTransactionKey[transactionsShortBlock.SyncBlockHeight].ContainsKey(kvp.Value)).ToArray();
                BitArray bitArray = bools.ToBitArray();

                bitMask = new byte[bitArray.Length / 8 + ((bitArray.Length % 8 > 0) ? 1 : 0)];

                bitArray.CopyTo(bitMask, 0);

                BigInteger bigIntegerSum = new BigInteger();
                int i = 0;
                foreach (var key in transactionsShortBlock.TransactionHeaderHashes.Keys)
                {
                    if (bools[i++])
                    {
                        int transactionHeaderOrder = _transactionOrderByTransactionKey[transactionsShortBlock.SyncBlockHeight][transactionsShortBlock.TransactionHeaderHashes[key]];
                        ITransactionRegistryBlock registryRegisterBlock = _transactionRegisterBlocksOrdered[transactionsShortBlock.SyncBlockHeight][transactionHeaderOrder];
                        IKey registryRegisterBlockKey = _transactionsRegistryHelper.GetTransactionRegistryHashKey(registryRegisterBlock);
                        BigInteger bigInteger = new BigInteger(registryRegisterBlockKey.Value.ToArray());
                        bigIntegerSum += bigInteger;
                    }
                }

                byte[] sumBytes = bigIntegerSum.ToByteArray().Take(Globals.TRANSACTION_KEY_HASH_SIZE).ToArray();
                byte[] returnValue = new byte[Globals.TRANSACTION_KEY_HASH_SIZE];
                Array.Copy(sumBytes, 0, returnValue, 0, sumBytes.Length);
                return returnValue;
            }
        }

        public void ClearByConfirmed(RegistryShortBlock transactionsShortBlock)
        {
            lock (_sync)
            {
                if (_transactionOrderByTransactionKey.ContainsKey(transactionsShortBlock.SyncBlockHeight))
                {
                    IEnumerable<IKey> mutualTransactionKeys = _transactionOrderByTransactionKey[transactionsShortBlock.SyncBlockHeight].Keys.Where(k => transactionsShortBlock.TransactionHeaderHashes.Values.Contains(k)).ToList();

                    foreach (IKey transactionKey in mutualTransactionKeys)
                    {
                        _transactionKeyBySourceKeys[transactionsShortBlock.SyncBlockHeight].Remove(_transactionSourceKeyByTransactionKey[transactionsShortBlock.SyncBlockHeight][transactionKey]);

                        _transactionSourceKeyByTransactionKey[transactionsShortBlock.SyncBlockHeight].Remove(transactionKey);
                        _transactionRegisterBlocksOrdered[transactionsShortBlock.SyncBlockHeight].Remove(_transactionOrderByTransactionKey[transactionsShortBlock.SyncBlockHeight][transactionKey]);
                        _transactionOrderByTransactionKey[transactionsShortBlock.SyncBlockHeight].Remove(transactionKey);
                        _transactionsShortBlocks[transactionsShortBlock.SyncBlockHeight].Remove(transactionsShortBlock.BlockHeight);
                    }
                }
            }
        }

        //TODO: need to understand whether it is needed to pass height of Sync Block or automatically take latest one?
        public SortedList<ushort, ITransactionRegistryBlock> DequeueBulk(int maxCount)
        {
            SortedList<ushort, ITransactionRegistryBlock> items = new SortedList<ushort, ITransactionRegistryBlock>();
            lock (_sync)
            {
                ulong syncBlockHeight = _synchronizationContext.LastBlockDescriptor?.BlockHeight ?? 0;

                if (_transactionRegisterBlocksOrdered.ContainsKey(syncBlockHeight))
                {
                    ushort order = 0;

                    foreach (int orderKey in _transactionRegisterBlocksOrdered[syncBlockHeight].Keys)
                    {
                        ITransactionRegistryBlock transactionRegisterBlock = _transactionRegisterBlocksOrdered[syncBlockHeight][orderKey];

                        items.Add(order++, transactionRegisterBlock);

                        if (order == ushort.MaxValue)
                        {
                            break;
                        }
                    }
                }
            }

            _logger.Debug($"MemPool returns {items.Count} items");
            return items;
        }

        public RegistryShortBlock GetRegistryShortBlockByHash(ulong syncBlockHeight, ulong round, byte[] hash)
        {
            if (!_transactionsShortBlocks.ContainsKey(syncBlockHeight))
            {
                return null;
            }

            if(!_transactionsShortBlocks[syncBlockHeight].ContainsKey(round))
            {
                return null;
            }

            RegistryShortBlock registryShortBlock = _transactionsShortBlocks[syncBlockHeight][round].FirstOrDefault(s => _hashCalculation.CalculateHash(s.RawData).Equals32(hash));

            return registryShortBlock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AscertainRoundInitialized(ulong syncBlockHeight)
        {
            //TODO: need to implement mechanism of erasing items related to old sync blocks
            if (!_transactionsIndicies.ContainsKey(syncBlockHeight))
            {
                _transactionsIndicies.Add(syncBlockHeight, 0);
                _transactionRegisterBlocksOrdered.Add(syncBlockHeight, new SortedDictionary<int, ITransactionRegistryBlock>());
                _transactionKeyBySourceKeys.Add(syncBlockHeight, new Dictionary<ITransactionSourceKey, List<IKey>>(new TransactionSourceKeyComparer()));
                _transactionOrderByTransactionKey.Add(syncBlockHeight, new Dictionary<IKey, int>());
                _transactionSourceKeyByTransactionKey.Add(syncBlockHeight, new Dictionary<IKey, ITransactionSourceKey>());
                _transactionsShortBlocks.Add(syncBlockHeight, new Dictionary<ulong, HashSet<RegistryShortBlock>>());
            }
        }
    }
}
