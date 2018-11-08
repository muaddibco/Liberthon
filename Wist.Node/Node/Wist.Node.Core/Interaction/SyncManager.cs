using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Wist.BlockLattice.Core;
using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.DataModel.Synchronization;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Interfaces;
using Wist.Core.HashCalculations;
using Wist.Core.Logging;
using Wist.Core.Synchronization;
using Wist.Proto.Model;

namespace Wist.Node.Core.Interaction
{
    public class SyncManagerImpl : SyncManager.SyncManagerBase
    {
        private readonly ISynchronizationContext _synchronizationContext;
        private readonly ILogger _logger;
        private readonly IChainDataService _syncChainDataService;
        private readonly IChainDataService _registryChainDataService;
        private readonly IChainDataService _transactionalDataService;
        private readonly IChainDataService _utxoConfidentialDataService;
        private readonly IHashCalculation _hashCalculation;

        public SyncManagerImpl(ISynchronizationContext synchronizationContext, IChainDataServicesManager chainDataServicesManager, IHashCalculationsRepository hashCalculationsRepository, ILogger logger)
        {
            _synchronizationContext = synchronizationContext;
            _logger = logger;
            _syncChainDataService = chainDataServicesManager.GetChainDataService(PacketType.Synchronization);
            _registryChainDataService = chainDataServicesManager.GetChainDataService(PacketType.Registry);
            _transactionalDataService = chainDataServicesManager.GetChainDataService(PacketType.Transactional);
            _utxoConfidentialDataService = chainDataServicesManager.GetChainDataService(PacketType.UtxoConfidential);
            _hashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
        }

        public override Task<SyncBlockDescriptor> GetLastSyncBlock(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new SyncBlockDescriptor
            {
                Height = _synchronizationContext?.LastBlockDescriptor.BlockHeight ?? 0,
                Hash = ByteString.CopyFrom(_synchronizationContext?.LastBlockDescriptor.Hash ?? new byte[Globals.DEFAULT_HASH_SIZE])
            });
        }

        public override async Task GetDeltaSyncBlocks(ByHeightRequest request, IServerStreamWriter<SyncBlockDescriptor> responseStream, ServerCallContext context)
        {
            IEnumerable<BlockBase> blocks = _syncChainDataService.GetAll(new BlockTypeLowHeightKey(BlockTypes.Synchronization_ConfirmedBlock, request.Height));

            foreach (BlockBase block in blocks)
            {
                await responseStream.WriteAsync(new SyncBlockDescriptor
                {
                    Height = ((SynchronizationConfirmedBlock)block).SyncBlockHeight,
                    Hash = ByteString.CopyFrom(_hashCalculation.CalculateHash(((SynchronizationConfirmedBlock)block).RawData)) //TODO: !!! need to change hash calculation in place to reading from database, otherwise DoS attack is allowed
                });
            }
        }

        public override async Task GetCombinedRegistryBlocksInfoSinceHeight(ByHeightRequest request, IServerStreamWriter<CombinedRegistryBlockInfo> responseStream, ServerCallContext context)
        {
            IEnumerable<BlockBase> blocks = _syncChainDataService.GetAll(new BlockTypeLowHeightKey(BlockTypes.Synchronization_RegistryCombinationBlock, request.Height));
            foreach (BlockBase blockBase in blocks)
            {
                SynchronizationRegistryCombinedBlock registryCombinedBlock = blockBase as SynchronizationRegistryCombinedBlock;
                CombinedRegistryBlockInfo combinedRegistryBlockInfo = new CombinedRegistryBlockInfo
                {
                    SyncBlockHeight = registryCombinedBlock.SyncBlockHeight,
                    Height = registryCombinedBlock.BlockHeight,
                    CombinedRegistryBlocksCount = (uint)registryCombinedBlock.BlockHashes.Length,
                };

                foreach (byte[] hash in registryCombinedBlock.BlockHashes)
                {
                    RegistryFullBlock registryFullBlock = (RegistryFullBlock)_registryChainDataService.Get(new SyncHashKey(registryCombinedBlock.SyncBlockHeight, hash));

                    if (registryFullBlock != null)
                    {
                        combinedRegistryBlockInfo.BlockDescriptors.Add(
                            new FullBlockDescriptor
                            {
                                SyncBlockHeight = registryCombinedBlock.SyncBlockHeight,
                                Round = registryFullBlock.BlockHeight,
                                TransactionsCount = (uint)registryFullBlock.TransactionHeaders.Count,
                                BlockHash = ByteString.CopyFrom(hash)
                            });
                    }
                }

                await responseStream.WriteAsync(combinedRegistryBlockInfo);
            }
        }

        public override async Task GetCombinedRegistryBlocksContentSinceHeight(ByHeightRequest request, IServerStreamWriter<TransactionInfo> responseStream, ServerCallContext context)
        {
            IEnumerable<BlockBase> blocks = _syncChainDataService.GetAll(new BlockTypeLowHeightKey(BlockTypes.Synchronization_RegistryCombinationBlock, request.Height));
            foreach (BlockBase blockBase in blocks)
            {
                SynchronizationRegistryCombinedBlock registryCombinedBlock = blockBase as SynchronizationRegistryCombinedBlock;
                TransactionInfo blockInfo = new TransactionInfo
                {
                    SyncBlockHeight = registryCombinedBlock.SyncBlockHeight,
                    BlockType = registryCombinedBlock.BlockType,
                    PacketType = (uint)registryCombinedBlock.PacketType,
                    Content = ByteString.CopyFrom(registryCombinedBlock.RawData.ToArray())
                };

                await responseStream.WriteAsync(blockInfo);
            }
        }

        public override Task GetAllCombinedRegistryBlocksSinceSync(ByHeightRequest request, IServerStreamWriter<CombinedRegistryBlockInfo> responseStream, ServerCallContext context)
        {
            return Task.Run(() => 
            {
                IEnumerable<BlockBase> blocks = _syncChainDataService.GetAllLastBlocksByType(BlockTypes.Synchronization_RegistryCombinationBlock).Where(b => ((SynchronizationRegistryCombinedBlock)b).SyncBlockHeight > request.Height);
                foreach (BlockBase blockBase in blocks)
                {
                    SynchronizationRegistryCombinedBlock registryCombinedBlock = blockBase as SynchronizationRegistryCombinedBlock;
                    CombinedRegistryBlockInfo combinedRegistryBlockInfo = new CombinedRegistryBlockInfo
                    {
                        SyncBlockHeight = registryCombinedBlock.SyncBlockHeight,
                        Height = registryCombinedBlock.BlockHeight,
                        CombinedRegistryBlocksCount = (uint)registryCombinedBlock.BlockHashes.Length,
                    };

                    foreach (byte[] item in registryCombinedBlock.BlockHashes)
                    {
                        combinedRegistryBlockInfo.BlockDescriptors.Add(new FullBlockDescriptor { BlockHash = ByteString.CopyFrom(item) });
                    }

                    responseStream.WriteAsync(combinedRegistryBlockInfo);
                }
            });
        }

        public override Task GetAllCombinedRegistryBlocksPerSync(ByHeightRequest request, IServerStreamWriter<CombinedRegistryBlockInfo> responseStream, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                IEnumerable<BlockBase> blocks = _syncChainDataService.GetAllLastBlocksByType(BlockTypes.Synchronization_RegistryCombinationBlock).Where(b => ((SynchronizationRegistryCombinedBlock)b).SyncBlockHeight == request.Height);
                foreach (BlockBase blockBase in blocks)
                {
                    SynchronizationRegistryCombinedBlock registryCombinedBlock = blockBase as SynchronizationRegistryCombinedBlock;
                    CombinedRegistryBlockInfo combinedRegistryBlockInfo = new CombinedRegistryBlockInfo
                    {
                        SyncBlockHeight = registryCombinedBlock.SyncBlockHeight,
                        Height = registryCombinedBlock.BlockHeight,
                        CombinedRegistryBlocksCount = (uint)registryCombinedBlock.BlockHashes.Length,
                    };

                    foreach (byte[] item in registryCombinedBlock.BlockHashes)
                    {
                        combinedRegistryBlockInfo.BlockDescriptors.Add(new FullBlockDescriptor { BlockHash = ByteString.CopyFrom(item) });
                    }

                    responseStream.WriteAsync(combinedRegistryBlockInfo);
                }
            });
        }

        public override Task GetTransactionRegistryBlockInfos(FullBlockRequest request, IServerStreamWriter<TransactionRegistryBlockInfo> responseStream, ServerCallContext context)
        {
            return Task.Run(async () => 
            {
                RegistryFullBlock registryFullBlock = (RegistryFullBlock)_registryChainDataService.Get(new DoubleHeightKey(request.SyncBlockHeight, request.Round));

                foreach (ITransactionRegistryBlock transactionRegistryBlock in registryFullBlock.TransactionHeaders.Values)
                {
                    TransactionRegistryBlockInfo blockInfo = new TransactionRegistryBlockInfo();

                    if (transactionRegistryBlock.BlockType == BlockTypes.Registry_Register)
                    {
                        blockInfo.AccountedHeader = new AccountedTransactionHeaderDescriptor
                        {
                            SyncBlockHeight = transactionRegistryBlock.SyncBlockHeight,
                            ReferencedBlockType = transactionRegistryBlock.ReferencedBlockType,
                            ReferencedPacketType = (uint)transactionRegistryBlock.ReferencedPacketType,
                            ReferencedTarget = ByteString.CopyFrom(transactionRegistryBlock.ReferencedTarget),
                            ReferencedHeight = ((RegistryRegisterBlock)transactionRegistryBlock).BlockHeight
                        };
                    }
                    else if (transactionRegistryBlock.BlockType == BlockTypes.Registry_RegisterUtxoConfidential)
                    {
                        blockInfo.UtxoHeader = new UtxoTransactionHeaderDescriptor
                        {
                            SyncBlockHeight = transactionRegistryBlock.SyncBlockHeight,
                            ReferencedBlockType = transactionRegistryBlock.ReferencedBlockType,
                            ReferencedPacketType = (uint)transactionRegistryBlock.ReferencedPacketType,
                            ReferencedTarget = ByteString.CopyFrom(transactionRegistryBlock.ReferencedTarget),
                            ReferencedTransactionKey = ByteString.CopyFrom(((RegistryRegisterUtxoConfidentialBlock)transactionRegistryBlock).ReferencedTransactionKey),
                            KeyImage = ByteString.CopyFrom(((RegistryRegisterUtxoConfidentialBlock)transactionRegistryBlock).KeyImage.Value.ToArray())
                        };

                    }

                    await responseStream.WriteAsync(blockInfo);
                }
            });
        }

        public override Task<TransactionInfo> GetFullRegistryBlock(HeightHashRequest request, ServerCallContext context)
        {
            RegistryFullBlock block = (RegistryFullBlock)_registryChainDataService.Get(new SyncHashKey(request.Height, request.Hash.ToByteArray()));

            if (block != null)
            {
                TransactionInfo transactionInfo = new TransactionInfo
                {
                    BlockType = block.BlockType,
                    PacketType = (uint)block.PacketType,
                    SyncBlockHeight = block.SyncBlockHeight,
                    Content = ByteString.CopyFrom(block.RawData.ToArray())
                };

                return Task.FromResult(transactionInfo);
            }

            return Task.FromResult(new TransactionInfo { IsEmpty = true });
        }
    }
}
