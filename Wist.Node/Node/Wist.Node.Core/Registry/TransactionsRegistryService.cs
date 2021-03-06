﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.Serializers;
using Wist.Network.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Configuration;
using Wist.Core.Cryptography;
using Wist.Core.Identity;
using Wist.Core.PerformanceCounters;
using Wist.Core.Predicates;
using Wist.Core.States;
using Wist.Core.Synchronization;
using Wist.Node.Core.PerformanceCounters;
using Wist.Core.HashCalculations;
using Wist.BlockLattice.Core;
using Wist.Core.Logging;
using Wist.Core.ExtensionMethods;

namespace Wist.Node.Core.Registry
{
    [RegisterDefaultImplementation(typeof(ITransactionsRegistryService), Lifetime = LifetimeManagement.Singleton)]
    public class TransactionsRegistryService : ITransactionsRegistryService
    {
        private readonly ISynchronizationContext _synchronizationContext;
        private readonly IPredicate _isBlockProducerPredicate;
        private readonly ITargetBlock<IRegistryMemPool> _transactionsRegistryProducingFlow;
        private readonly IRegistryMemPool _registryMemPool;
        private readonly IRegistryGroupState _registryGroupState;
        private readonly IIdentityKeyProvider _transactionHashKey;
        private readonly ICryptoService _cryptoService;
        private readonly IConfigurationService _configurationService;
        private readonly IServerCommunicationServicesRegistry _serverCommunicationServicesRegistry;
        private readonly ISerializersFactory _signatureSupportSerializersFactory;
        private readonly ITransactionsRegistryHelper _transactionsRegistryHelper;
        private readonly IHashCalculation _powCalculation;
        private readonly IHashCalculation _hashCalculation;
        private readonly ILogger _logger;
        private IServerCommunicationService _tcpCommunicationService;
        private IServerCommunicationService _udpCommunicationService;
        private readonly NodeCountersService _nodeCountersService;
        private Timer _timer;
        private IDisposable _syncContextUnsubscriber;

        public TransactionsRegistryService(IStatesRepository statesRepository, IPredicatesRepository predicatesRepository, IRegistryMemPool registryMemPool, 
            IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, ICryptoService cryptoService, IConfigurationService configurationService, 
            IServerCommunicationServicesRegistry serverCommunicationServicesRegistry, IPerformanceCountersRepository performanceCountersRepository, 
            ISerializersFactory signatureSupportSerializersFactory, ITransactionsRegistryHelper transactionsRegistryHelper, IHashCalculationsRepository hashCalculationsRepository, ILoggerService loggerService)
        {
            _synchronizationContext = statesRepository.GetInstance<ISynchronizationContext>();
            _registryGroupState = statesRepository.GetInstance<IRegistryGroupState>();
            _isBlockProducerPredicate = predicatesRepository.GetInstance("IsBlockProducer");
            _transactionHashKey = identityKeyProvidersRegistry.GetTransactionsIdenityKeyProvider();
            _cryptoService = cryptoService;
            _configurationService = configurationService;
            _serverCommunicationServicesRegistry = serverCommunicationServicesRegistry;
            _signatureSupportSerializersFactory = signatureSupportSerializersFactory;
            _transactionsRegistryHelper = transactionsRegistryHelper;
            _powCalculation = hashCalculationsRepository.Create(Globals.POW_TYPE);
            _hashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
            _logger = loggerService.GetLogger(nameof(TransactionsRegistryService));
            TransformBlock<IRegistryMemPool, SortedList<ushort, ITransactionRegistryBlock>> deduplicateAndOrderTransactionRegisterBlocksBlock = new TransformBlock<IRegistryMemPool, SortedList<ushort, ITransactionRegistryBlock>>((Func<IRegistryMemPool, SortedList<ushort, ITransactionRegistryBlock>>)DeduplicateAndOrderTransactionRegisterBlocks);
            TransformBlock<SortedList<ushort, ITransactionRegistryBlock>, RegistryFullBlock> produceTransactionsFullBlock = new TransformBlock<SortedList<ushort, ITransactionRegistryBlock>, RegistryFullBlock>((Func<SortedList<ushort, ITransactionRegistryBlock>, RegistryFullBlock>)ProduceTransactionsFullBlock);
            ActionBlock<Tuple<RegistryFullBlock, RegistryShortBlock>> sendTransactionsBlocks = new ActionBlock<Tuple<RegistryFullBlock, RegistryShortBlock>>((Action<Tuple<RegistryFullBlock, RegistryShortBlock>>)SendTransactionsBlocks);
            TransformBlock<RegistryFullBlock, Tuple<RegistryFullBlock, RegistryShortBlock>> produceTransactionsShortBlock = new TransformBlock<RegistryFullBlock, Tuple<RegistryFullBlock, RegistryShortBlock>>((Func<RegistryFullBlock, Tuple<RegistryFullBlock, RegistryShortBlock>>)ProduceTransactionsShortBlock);

            deduplicateAndOrderTransactionRegisterBlocksBlock.LinkTo(produceTransactionsFullBlock);
            produceTransactionsFullBlock.LinkTo(produceTransactionsShortBlock);
            produceTransactionsShortBlock.LinkTo(sendTransactionsBlocks);

            _transactionsRegistryProducingFlow = deduplicateAndOrderTransactionRegisterBlocksBlock;


            _registryMemPool = registryMemPool;

            _nodeCountersService = performanceCountersRepository.GetInstance<NodeCountersService>();
        }

        public void Initialize()
        {
            _tcpCommunicationService = _serverCommunicationServicesRegistry.GetInstance(_configurationService.Get<IRegistryConfiguration>().TcpServiceName);
            _udpCommunicationService = _serverCommunicationServicesRegistry.GetInstance(_configurationService.Get<IRegistryConfiguration>().UdpServiceName);
        }

        public void Start()
        {
            _syncContextUnsubscriber = _synchronizationContext.SubscribeOnStateChange(new ActionBlock<string>((Action<string>)OnSyncContextChanged));
        }

        public void Stop()
        {
            StopTimer();
            _syncContextUnsubscriber?.Dispose();
        }

        private void OnSyncContextChanged(string propName)
        {
            RecalculateProductionTimer();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _registryGroupState.Round++;

            if (_isBlockProducerPredicate.Evaluate())
            {
                //_registryGroupState.WaitLastBlockConfirmationReceived();
                _transactionsRegistryProducingFlow.Post(_registryMemPool);
            }
        }

        private void RecalculateProductionTimer()
        {
            StopTimer();

            _registryGroupState.Round = 0;
            //_registryGroupState.ToggleLastBlockConfirmationReceived();

            _timer = new Timer(5000);
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        private SortedList<ushort, ITransactionRegistryBlock> DeduplicateAndOrderTransactionRegisterBlocks(IRegistryMemPool memPool)
        {
            SortedList<ushort, ITransactionRegistryBlock> transactionRegisterBlocks = memPool.DequeueBulk(-1);

            //TODO: Remove debug code
            if(transactionRegisterBlocks.Count > 0)
            {

            }

            return transactionRegisterBlocks;
        }

        private RegistryFullBlock ProduceTransactionsFullBlock(SortedList<ushort, ITransactionRegistryBlock> transactionRegisterBlocks)
        {
            SynchronizationDescriptor synchronizationDescriptor = _synchronizationContext.LastBlockDescriptor;
            ulong syncBlockHeight = synchronizationDescriptor?.BlockHeight ?? 0;
            byte[] hash = synchronizationDescriptor?.Hash ?? new byte[Globals.DEFAULT_HASH_SIZE];
            byte[] pow = _powCalculation.CalculateHash(hash);

            _logger.Debug($"ProduceTransactionsFullBlock synchronizationDescriptor[{syncBlockHeight}].Hash = {hash.ToHexString()}; POW = {pow.ToHexString()}");

            RegistryFullBlock transactionsFullBlock = new RegistryFullBlock
            {
                SyncBlockHeight = syncBlockHeight,
                PowHash = pow,
                BlockHeight = (ulong)_registryGroupState.Round,
                TransactionHeaders = transactionRegisterBlocks
            };

            //_nodeCountersService.RegistryBlockLastSize.RawValue = transactionRegisterBlocks.Count;
            //_nodeCountersService.RegistryBlockLastSize.NextSample();

            return transactionsFullBlock;
        }

        private void SendTransactionsBlocks(Tuple<RegistryFullBlock, RegistryShortBlock> tuple)
        {
            RegistryFullBlock transactionsFullBlock = tuple.Item1;
            RegistryShortBlock transactionsShortBlock = tuple.Item2;

            ISerializer fullBlockSerializer = _signatureSupportSerializersFactory.Create(transactionsFullBlock);
            ISerializer shortBlockSerializer = _signatureSupportSerializersFactory.Create(transactionsShortBlock);

            shortBlockSerializer.FillBodyAndRowBytes();
            transactionsFullBlock.ShortBlockHash = _hashCalculation.CalculateHash(transactionsShortBlock.RawData);

            _logger.Debug($"Sending FullBlock with {transactionsFullBlock.TransactionHeaders.Count} transactions and ShortBlock with {transactionsShortBlock.TransactionHeaderHashes.Count} hashes at round {transactionsFullBlock.BlockHeight}");

            _tcpCommunicationService.PostMessage(_registryGroupState.SyncLayerNode, fullBlockSerializer);
            _tcpCommunicationService.PostMessage(_registryGroupState.GetAllNeighbors(), shortBlockSerializer);
        }

        private Tuple<RegistryFullBlock, RegistryShortBlock> ProduceTransactionsShortBlock(RegistryFullBlock transactionsFullBlock)
        {
            RegistryShortBlock transactionsShortBlock = new RegistryShortBlock
            {
                SyncBlockHeight = transactionsFullBlock.SyncBlockHeight,
                Nonce = transactionsFullBlock.Nonce,
                PowHash = transactionsFullBlock.PowHash, // _powCalculation.CalculateHash(_synchronizationContext.LastBlockDescriptor?.Hash ?? new byte[Globals.DEFAULT_HASH_SIZE]),
                BlockHeight = transactionsFullBlock.BlockHeight,
                TransactionHeaderHashes = new SortedList<ushort, IKey>(transactionsFullBlock.TransactionHeaders.ToDictionary(i => i.Key, i => _transactionsRegistryHelper.GetTransactionRegistryTwiceHashedKey(i.Value)))
            };

            Tuple<RegistryFullBlock, RegistryShortBlock> tuple = new Tuple<RegistryFullBlock, RegistryShortBlock>(transactionsFullBlock, transactionsShortBlock);

            return tuple;
        }

        private void SendTransactionsShortBlock(Tuple<RegistryFullBlock, RegistryShortBlock> tuple)
        {
            RegistryShortBlock transactionsShortBlock = tuple.Item2;
            ISerializer signatureSupportSerializer = _signatureSupportSerializersFactory.Create(transactionsShortBlock);
            _tcpCommunicationService.PostMessage(_registryGroupState.GetAllNeighbors(), signatureSupportSerializer);
        }
    }
}
