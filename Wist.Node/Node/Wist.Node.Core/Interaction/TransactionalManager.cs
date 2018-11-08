using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Wist.BlockLattice.Core;
using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Interfaces;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;
using Wist.Core.Logging;
using Wist.Proto.Model;

namespace Wist.Node.Core.Interaction
{
    public class TransactionalChainManagerImpl : TransactionalChainManager.TransactionalChainManagerBase
    {
        private readonly ILogger _logger;
        private readonly IIdentityKeyProvider _identityKeyProvider;
        private readonly IChainDataService _registryChainDataService;
        private readonly IChainDataService _transactionalDataService;
        private readonly IChainDataService _utxoConfidentialDataService;
        private readonly IHashCalculation _hashCalculation;

        public TransactionalChainManagerImpl(IChainDataServicesManager chainDataServicesManager, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository, ILogger logger)
        {
            _logger = logger;
            _transactionalDataService = chainDataServicesManager.GetChainDataService(PacketType.Transactional);
            _registryChainDataService = chainDataServicesManager.GetChainDataService(PacketType.Registry);
            _utxoConfidentialDataService = chainDataServicesManager.GetChainDataService(PacketType.UtxoConfidential);
            _identityKeyProvider = identityKeyProvidersRegistry.GetInstance();
            _hashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
        }

        public override Task<TransactionalBlockEssense> GetLastTransactionalBlock(TransactionalBlockRequest request, ServerCallContext context)
        {
            if (request.PublicKey == null)
            {
                throw new ArgumentNullException(nameof(request.PublicKey));
            }

            byte[] keyBytes = request.PublicKey.ToByteArray();

            if (keyBytes.Length != Globals.NODE_PUBLIC_KEY_SIZE)
            {
                throw new ArgumentException($"Public key size must be of {Globals.NODE_PUBLIC_KEY_SIZE} bytes");
            }

            IKey key = _identityKeyProvider.GetKey(keyBytes);
            TransactionalBlockBase transactionalBlockBase = (TransactionalBlockBase)_transactionalDataService.GetLastBlock(key);

            TransactionalBlockEssense transactionalBlockEssense = new TransactionalBlockEssense
            {
                Height = transactionalBlockBase?.BlockHeight ?? 0,
                //TODO: need to reconsider hash calculation here since it is potential point of DoS attack
                Hash = ByteString.CopyFrom(transactionalBlockBase != null ? _hashCalculation.CalculateHash(transactionalBlockBase.RawData) : new byte[Globals.DEFAULT_HASH_SIZE]),
                UpToDateFunds = transactionalBlockBase?.UptodateFunds ?? 0
            };

            return Task.FromResult(transactionalBlockEssense);
        }

        public override Task GetTransactionInfos(FullBlockRequest request, IServerStreamWriter<TransactionInfo> responseStream, ServerCallContext context)
        {
            return Task.Run(async () =>
            {
                RegistryFullBlock registryFullBlock = (RegistryFullBlock)_registryChainDataService.Get(new DoubleHeightKey(request.SyncBlockHeight, request.Round));
                foreach (ITransactionRegistryBlock transactionRegistryBlock in registryFullBlock.TransactionHeaders.Values)
                {
                    try
                    {

                        BlockBase blockBase = null;
                        if (transactionRegistryBlock.BlockType == BlockTypes.Registry_Register)
                        {
                            blockBase = _transactionalDataService.Get(new SyncHashKey(transactionRegistryBlock.SyncBlockHeight, transactionRegistryBlock.ReferencedBodyHash));
                        }
                        else if (transactionRegistryBlock.BlockType == BlockTypes.Registry_RegisterUtxoConfidential)
                        {
                            blockBase = _utxoConfidentialDataService.Get(new SyncHashKey(transactionRegistryBlock.SyncBlockHeight, transactionRegistryBlock.ReferencedBodyHash));
                        }

                        if (blockBase != null)
                        {
                            await responseStream.WriteAsync(
                                new TransactionInfo
                                {
                                    SyncBlockHeight = transactionRegistryBlock.SyncBlockHeight,
                                    PacketType = (uint)transactionRegistryBlock.ReferencedPacketType,
                                    BlockType = (uint)transactionRegistryBlock.ReferencedBlockType,
                                    Content = ByteString.CopyFrom(blockBase.RawData.ToArray())
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to retrieve block for SyncBlockHeight {request.SyncBlockHeight} and Round {request.Round}", ex);
                    }
                }
            });
        }
    }
}
