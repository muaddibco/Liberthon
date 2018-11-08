using Google.Protobuf;
using Grpc.Core;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Wist.BlockLattice.Core;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.DataModel.Synchronization;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Parsers;
using Wist.BlockLattice.Core.Serializers;
using Wist.Client.Common.Configuration;
using Wist.Client.Common.Entities;
using Wist.Client.Common.Interfaces;
using Wist.Client.Common.Services;
using Wist.Client.DataModel.Services;
using Wist.Core;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Communication;
using Wist.Core.Configuration;
using Wist.Core.Cryptography;
using Wist.Core.ExtensionMethods;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;
using Wist.Core.Logging;
using Wist.Core.States;
using Wist.Crypto.ConfidentialAssets;
using Wist.Network.Configuration;
using Wist.Network.Interfaces;
using Wist.Network.Topology;
using Wist.Proto.Model;

namespace Wist.Client.Common.Communication
{
    [RegisterDefaultImplementation(typeof(INetworkSynchronizer), Lifetime = LifetimeManagement.Singleton)]
    public class NetworkSynchronizer : INetworkSynchronizer
    {
        private readonly IBlockCreator _blockCreator;
        private readonly ICommunicationService _communicationService;
        private readonly ICryptoService _cryptoService;
        private readonly IDataAccessService _dataAccessService;
        private readonly IHashCalculation _defaultHashCalculation;
        private readonly ISynchronizerConfiguration _synchronizerConfiguration;
        private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
        private readonly INodesResolutionService _nodesResolutionService;
        private readonly IIdentityKeyProvider _identityKeyProvider;
        private readonly IClientState _clientState;
        private readonly ILogger _logger;
        private readonly CommunicationConfigurationBase _communicationConfiguration;
        private readonly object _sync = new object();
        private bool _isProcessing;

        private CancellationToken _cancellationToken;
        private bool _isInitialized;
        private Entities.SyncBlockDescriptor _lastSyncDescriptor;
        private RegistryCombinedBlockDescriptor _lastCombinedBlockDescriptor;
        private SyncManager.SyncManagerClient _syncLayerSyncManagerClient;
        private TransactionalChainManager.TransactionalChainManagerClient _storageLayerSyncManagerClient;

        private IKey _registryNodeKey;
        private IKey _storageNodeKey;

        public NetworkSynchronizer(IBlockCreator blockCreator, IClientCommunicationServiceRepository clientCommunicationServiceRepository, ICryptoService cryptoService,
            IDataAccessService dataAccessService, IHashCalculationsRepository hashCalculationsRepository, IConfigurationService configurationService, 
            IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, INodesResolutionService nodesResolutionService,
            IStatesRepository statesRepository, ILoggerService loggerService)
        {
            _cryptoService = cryptoService;
            _dataAccessService = dataAccessService;
            _blockCreator = blockCreator;
            _communicationService = clientCommunicationServiceRepository.GetInstance("TcpClientCommunicationService");
            _defaultHashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
            _synchronizerConfiguration = configurationService.Get<ISynchronizerConfiguration>();
            _communicationConfiguration = (CommunicationConfigurationBase)configurationService["generalTcpCommunication"];
            _blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
            _nodesResolutionService = nodesResolutionService;
            _identityKeyProvider = identityKeyProvidersRegistry.GetInstance();
            _clientState = statesRepository.GetInstance<IClientState>();
            _logger = loggerService.GetLogger(nameof(NetworkSynchronizer));
        }

        #region ============ PUBLIC FUNCTIONS =============  

        public DateTime LastSyncTime { get; set; }

        public bool SendData(ISerializer transferBlockSerializer, ISerializer registerSerializer)
        {
            try
            {
                _communicationService.PostMessage(_registryNodeKey, registerSerializer);

                _communicationService.PostMessage(_storageNodeKey, transferBlockSerializer);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ApproveDataSent()
        {
            return true;
        }

        public void Initialize(CancellationToken cancellationToken)
        {
            if(_isInitialized)
            {
                return;
            }

            lock (_sync)
            {
                if(_isInitialized)
                {
                    return;
                }

                _logger.Info("Initialization Started");

                try
                {
                    _communicationService.Init(new Network.Communication.SocketSettings(_communicationConfiguration.MaxConnections, _communicationConfiguration.ReceiveBufferSize, _communicationConfiguration.ListeningPort, System.Net.Sockets.AddressFamily.InterNetwork));

                    if (_dataAccessService.GetLastSyncBlock(out ulong syncBlockHeight, out byte[] syncBlockHash))
                    {
                        _lastSyncDescriptor = new Entities.SyncBlockDescriptor(syncBlockHeight, syncBlockHash);
                    }
                    else
                    {
                        _lastSyncDescriptor = new Entities.SyncBlockDescriptor(0, new byte[32]);
                    }

                    if (_dataAccessService.GetLastRegistryCombinedBlock(out ulong combinedBlockHeight, out byte[] combinedBlockContent))
                    {
                        _lastCombinedBlockDescriptor = new RegistryCombinedBlockDescriptor(combinedBlockHeight, combinedBlockContent, _defaultHashCalculation.CalculateHash(combinedBlockContent));
                    }
                    else
                    {
                        _lastCombinedBlockDescriptor = new RegistryCombinedBlockDescriptor(0, null, new byte[32]);
                    }

                    foreach (string nodeDescriptor in _synchronizerConfiguration.Nodes)
                    {
                        string[] pair = nodeDescriptor.Split(':');
                        byte[] keyBytes = pair[0].HexStringToByteArray();
                        IKey nodeKey = _identityKeyProvider.GetKey(keyBytes);
                        IPAddress ipAddress = IPAddress.Parse(pair[1]);
                        NodeAddress nodeAddress = new NodeAddress(nodeKey, ipAddress);
                        _nodesResolutionService.UpdateSingleNode(nodeAddress);
                    }

                    IKey syncNodeKey = _identityKeyProvider.GetKey(_synchronizerConfiguration.SyncNodeKey.HexStringToByteArray());
                    _registryNodeKey = _identityKeyProvider.GetKey(_synchronizerConfiguration.RegistryNodeKey.HexStringToByteArray());
                    _storageNodeKey = _identityKeyProvider.GetKey(_synchronizerConfiguration.StorageNodeKey.HexStringToByteArray());

                    IPAddress syncNodeAddress = _nodesResolutionService.ResolveNodeAddress(syncNodeKey);
                    IPAddress storageNodeAddress = _nodesResolutionService.ResolveNodeAddress(_storageNodeKey);

                    Channel syncLayerChannel = new Channel(syncNodeAddress.ToString(), 5050, ChannelCredentials.Insecure);
                    Channel storageLayerChannel = new Channel(storageNodeAddress.ToString(), 5050, ChannelCredentials.Insecure);

                    _syncLayerSyncManagerClient = new SyncManager.SyncManagerClient(syncLayerChannel);
                    _storageLayerSyncManagerClient = new TransactionalChainManager.TransactionalChainManagerClient(storageLayerChannel);


                    _cancellationToken = cancellationToken;
                    _isInitialized = true;

                    _logger.Info($"Last Sync Block Height = {_lastSyncDescriptor.Height}; Last Registry Combined Block Height = {_lastCombinedBlockDescriptor.Height}");
                }
                catch(Exception ex)
                {
                    _logger.Error("Failure during initializtion", ex);
                }
                finally
                {
                    _logger.Info("Initialization completed");
                }
            }
        }

        public void Start()
        {
            _logger.Info("Started");

            _communicationService.Start();

            PeriodicTaskFactory.Start(async () => 
            {
                if(_isProcessing)
                {
                    return;
                }

                lock(_sync)
                {
                    if(_isProcessing)
                    {
                        return;
                    }

                    _isProcessing = true;
                }

                try
                {
                    Proto.Model.SyncBlockDescriptor syncBlockDescriptor = _syncLayerSyncManagerClient.GetLastSyncBlock(new Empty());
                    if (_lastSyncDescriptor.Height < syncBlockDescriptor.Height)
                    {
                        _lastSyncDescriptor = new Entities.SyncBlockDescriptor(syncBlockDescriptor.Height, syncBlockDescriptor.Hash.ToByteArray());
                        _dataAccessService.UpdateLastSyncBlock(syncBlockDescriptor.Height, syncBlockDescriptor.Hash.ToByteArray());
                    }

                    ulong lastCombinedBlockHeight = _lastCombinedBlockDescriptor.Height;
                    SynchronizationRegistryCombinedBlock lastCombinedBlock = null;
                    AsyncServerStreamingCall<TransactionInfo> asyncCall = _syncLayerSyncManagerClient.GetCombinedRegistryBlocksContentSinceHeight(new ByHeightRequest { Height = _lastCombinedBlockDescriptor.Height });
                    while (await asyncCall.ResponseStream.MoveNext(_cancellationToken))
                    {
                        SynchronizationRegistryCombinedBlock combinedBlock = GetRegistryCombinedBlock(asyncCall.ResponseStream.Current);

                        if(combinedBlock == null)
                        {
                            continue;
                        }

                        try
                        {
                            _dataAccessService.UpdateLastRegistryCombinedBlock(combinedBlock.BlockHeight, combinedBlock.RawData.ToArray());
                            if (lastCombinedBlockHeight < combinedBlock.BlockHeight)
                            {
                                lastCombinedBlockHeight = combinedBlock.BlockHeight;
                                lastCombinedBlock = combinedBlock;
                            }

                            foreach (byte[] fullRegistryBlockHash in combinedBlock.BlockHashes)
                            {
                                await UpdateTransactionsByFullRegistryBlock(combinedBlock, fullRegistryBlockHash);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Failure during obtaining transactions at Registry Combined Block with height {combinedBlock.BlockHeight}", ex);
                        }
                    }

                    if (lastCombinedBlock != null)
                    {
                        _lastCombinedBlockDescriptor = new RegistryCombinedBlockDescriptor(lastCombinedBlock.BlockHeight, lastCombinedBlock.RawData.ToArray(), _defaultHashCalculation.CalculateHash(lastCombinedBlock.RawData));
                        _dataAccessService.UpdateLastRegistryCombinedBlock(_lastCombinedBlockDescriptor.Height, _lastCombinedBlockDescriptor.Hash);
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error("Failure during updating blockchain", ex);
                }
                finally
                {
                    _isProcessing = false;
                }
            }, 5000, cancelToken: _cancellationToken);
        }

        #endregion

        #region ============ PRIVATE FUNCTIONS ============ 

        private SynchronizationRegistryCombinedBlock GetRegistryCombinedBlock(TransactionInfo blockInfo)
        {
            try
            {
                SynchronizationRegistryCombinedBlock combinedBlock;
                IBlockParsersRepository blockParsersRepository = _blockParsersRepositoriesRepository.GetBlockParsersRepository((PacketType)blockInfo.PacketType);
                IBlockParser blockParser = blockParsersRepository.GetInstance((ushort)blockInfo.BlockType);
                BlockBase blockBase = blockParser.Parse(blockInfo.Content.ToByteArray());
                combinedBlock = (SynchronizationRegistryCombinedBlock)blockBase;
                return combinedBlock;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failure during deserealization of block of PacketType = {(PacketType)blockInfo.PacketType} and  BlockType = {blockInfo.BlockType} at Sync Block Height {blockInfo.SyncBlockHeight}", ex);
            }

            return null;
        }

        private async Task UpdateTransactionsByFullRegistryBlock(SynchronizationRegistryCombinedBlock combinedBlock, byte[] fullRegistryBlockHash)
        {
            TransactionInfo registryFullBlockInfo = _syncLayerSyncManagerClient.GetFullRegistryBlock(new HeightHashRequest { Height = combinedBlock.SyncBlockHeight, Hash = ByteString.CopyFrom(fullRegistryBlockHash) });

            if(registryFullBlockInfo.IsEmpty)
            {
                return;
            }

            IBlockParsersRepository registryFullBlockParserRepo = _blockParsersRepositoriesRepository.GetBlockParsersRepository((PacketType)registryFullBlockInfo.PacketType);
            IBlockParser registryFullBlockParser = registryFullBlockParserRepo.GetInstance((ushort)registryFullBlockInfo.BlockType);

            RegistryFullBlock registryFullBlock = (RegistryFullBlock)registryFullBlockParser.Parse(registryFullBlockInfo.Content.ToByteArray());

            AsyncServerStreamingCall<TransactionInfo> asyncTransactionInfosStream = _storageLayerSyncManagerClient.GetTransactionInfos(new FullBlockRequest { SyncBlockHeight = registryFullBlock.SyncBlockHeight, Round = registryFullBlock.BlockHeight });
            while (await asyncTransactionInfosStream.ResponseStream.MoveNext(_cancellationToken))
            {
                TransactionInfo transactionInfo = asyncTransactionInfosStream.ResponseStream.Current;
                IBlockParsersRepository transactionBlockParserRepo = _blockParsersRepositoriesRepository.GetBlockParsersRepository((PacketType)transactionInfo.PacketType);
                IBlockParser transactionBlockParser = transactionBlockParserRepo.GetInstance((ushort)transactionInfo.BlockType);
                BlockBase transactionBlockBase = transactionBlockParser.Parse(transactionInfo.Content.ToByteArray());

                UpdateTransaction(transactionBlockBase, combinedBlock.BlockHeight);
            }
        }

        private void UpdateTransaction(BlockBase blockBase, ulong registryCombinedBlockHeight)
        {
            switch (blockBase.PacketType)
            {
                case PacketType.Transactional:
                    UpdateTransactionalTransaction(blockBase as TransactionalBlockBase, registryCombinedBlockHeight);
                    break;
                case PacketType.UtxoConfidential:
                    UpdateUtxoConfidentialTransaction(blockBase as UtxoConfidentialBase, registryCombinedBlockHeight);
                    break;
            }
        }

        private void UpdateTransactionalTransaction(TransactionalBlockBase transactionalBlockBase, ulong registryCombinedBlockHeight)
        {
            switch (transactionalBlockBase.BlockType)
            {
                case BlockTypes.Transaction_IssueAssets:
                    ProcessIssueAssetsBlock(transactionalBlockBase as IssueAssetsBlock, registryCombinedBlockHeight);
                    break;
                case BlockTypes.Transaction_TransferAssetsToUtxo:
                    ProcessTransferAssetsToUtxo(transactionalBlockBase as TransferAssetToUtxoBlock, registryCombinedBlockHeight);
                    break;
                case BlockTypes.Transaction_TransferFunds:
                    ProcessTranferFunds(transactionalBlockBase as TransferFundsBlock, registryCombinedBlockHeight);
                    break;
            }
        }

        private void UpdateUtxoConfidentialTransaction(UtxoConfidentialBase utxoConfidentialBase, ulong registryCombinedBlockHeight)
        {
            switch (utxoConfidentialBase.BlockType)
            {
                case BlockTypes.UtxoConfidential_NonQuantitativeTransitionAssetTransfer:
                    ProcessNonQuantitativeTransitionAssetTransfer(utxoConfidentialBase as NonQuantitativeTransitionAssetTransferBlock, registryCombinedBlockHeight);
                    break;
            }
        }

        private void ProcessIssueAssetsBlock(IssueAssetsBlock issueAssetsBlock, ulong registryCombinedBlockHeight)
        {
            _dataAccessService.StoreIncomingTransactionalBlock(issueAssetsBlock.SyncBlockHeight, registryCombinedBlockHeight, issueAssetsBlock.BlockHeight,
                issueAssetsBlock.BlockType, issueAssetsBlock.Signer.Value.Span, issueAssetsBlock.TagId, issueAssetsBlock.RawData.Span, null);
        }

        private void ProcessTransferAssetsToUtxo(TransferAssetToUtxoBlock block, ulong registryCombinedBlockHeight)
        {
            if (_clientState.IsConfidential())
            {
                bool isToMe = ConfidentialAssetsHelper.IsDestinationKeyMine(block.DestinationKey, block.TransactionPublicKey, _clientState.GetSecretViewKey(), _clientState.GetPublicSpendKey());

                if(isToMe)
                {
                    byte[] otsk = ConfidentialAssetsHelper.GetOTSK(block.TransactionPublicKey, _clientState.GetSecretViewKey(), _clientState.GetSecretSpendKey());
                    byte[] assetId = ConfidentialAssetsHelper.GetAssetIdFromEcdhTupleCA(block.EcdhTuple, block.TransactionPublicKey, _clientState.GetSecretViewKey());
                    _dataAccessService.StoreUtxoUnspentOutputs(block.SyncBlockHeight, registryCombinedBlockHeight, block.BlockType,
                        block.RawData.Span, block.TransactionPublicKey, ConfidentialAssetsHelper.GenerateKeyImage(otsk), block.TagId, block.AssetCommitment, block.DestinationKey, 1, assetId);
                }
                else
                {
                    _dataAccessService.StoreUtxoOutput(block.TagId, block.AssetCommitment, block.DestinationKey);
                }
            }
        }

        private void ProcessTranferFunds(TransferFundsBlock transferFundsBlock, ulong registryCombinedBlockHeight)
        {
            if(!_clientState.IsConfidential() && _clientState.GetPublicKeyHash().Equals32(transferFundsBlock.TargetOriginalHash))
            {
                _dataAccessService.StoreIncomingTransactionalBlock(transferFundsBlock.SyncBlockHeight, registryCombinedBlockHeight, transferFundsBlock.BlockHeight, 
                    transferFundsBlock.BlockType, transferFundsBlock.Signer.Value.Span, transferFundsBlock.TagId, transferFundsBlock.RawData.Span, transferFundsBlock.TargetOriginalHash);
            }
        }

        private void ProcessNonQuantitativeTransitionAssetTransfer(NonQuantitativeTransitionAssetTransferBlock block, ulong registryCombinedBlockHeight)
        {
            if (!_clientState.IsConfidential() && _clientState.GetPublicKeyHash().Equals32(block.DestinationKey))
            {
                _dataAccessService.StoreIncomingTransitionUtxoTransactionBlock(block.SyncBlockHeight, registryCombinedBlockHeight, block.BlockType, block.TagId, block.RawData.Span, 
                    block.KeyImage.Value.Span, block.AssetCommitment, block.DestinationKey);
            }
        }

        public Proto.Model.SyncBlockDescriptor GetLastSyncBlock()
        {
            return _syncLayerSyncManagerClient.GetLastSyncBlock(new Empty());
        }

        public TransactionalBlockEssense GetLastBlock(byte[] key)
        {
            return _storageLayerSyncManagerClient.GetLastTransactionalBlock(new TransactionalBlockRequest { PublicKey = ByteString.CopyFrom(key) });
        }

        #endregion

    }
}
