using CommonServiceLocator;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Exceptions;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Parsers.Registry
{
    [RegisterExtension(typeof(IBlockParser), Lifetime = LifetimeManagement.Singleton)]
    public class RegistryFullBlockParser : SyncedBlockParserBase
    {
        private readonly RegistryRegisterBlockParser _registryRegisterBlockParser;
        private readonly Lazy<IBlockParsersRepositoriesRepository> _blockParsersRepositoriesRepository;

        public RegistryFullBlockParser(IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationRepository) : base(identityKeyProvidersRegistry, hashCalculationRepository)
        {
            _registryRegisterBlockParser = new RegistryRegisterBlockParser(identityKeyProvidersRegistry, hashCalculationRepository);
            _blockParsersRepositoriesRepository = new Lazy<IBlockParsersRepositoriesRepository>(() => ServiceLocator.Current.GetInstance<IBlockParsersRepositoriesRepository>());
        }

        public override ushort BlockType => BlockTypes.Registry_FullBlock;

        public override PacketType PacketType => PacketType.Registry;

        protected override Memory<byte> ParseSynced(ushort version, Memory<byte> spanBody, out SyncedBlockBase syncedBlockBase)
        {
            if (version == 1)
            {
                RegistryFullBlock transactionsFullBlock = new RegistryFullBlock();
                ushort itemsCount = BinaryPrimitives.ReadUInt16LittleEndian(spanBody.Span);

                transactionsFullBlock.TransactionHeaders = new SortedList<ushort, ITransactionRegistryBlock>(itemsCount);
                int readBytes = 2;

                if (itemsCount > 0)
                {
                    for (int i = 0; i < itemsCount; i++)
                    {
                        ushort order = BinaryPrimitives.ReadUInt16LittleEndian(spanBody.Span.Slice(readBytes));
                        readBytes += 2;

                        BlockParserBase.GetPacketAndBlockTypes(spanBody.Slice(readBytes), out PacketType packetType, out ushort blockType);
                        IBlockParsersRepository blockParsersRepository = _blockParsersRepositoriesRepository.Value.GetBlockParsersRepository(packetType);
                        IBlockParser blockParser = blockParsersRepository.GetInstance(blockType);
                        BlockBase block = blockParser.Parse(spanBody.Slice(readBytes));

                        readBytes += block?.RawData.Length ?? 0;

                        if (block is ITransactionRegistryBlock transactionRegistryBlock)
                        {
                            transactionsFullBlock.TransactionHeaders.Add(order, transactionRegistryBlock);
                        }
                    }
                }

                transactionsFullBlock.ShortBlockHash = spanBody.Slice(readBytes, Globals.DEFAULT_HASH_SIZE).ToArray();
                readBytes += Globals.DEFAULT_HASH_SIZE;

                syncedBlockBase = transactionsFullBlock;

                return spanBody.Slice(readBytes);
            }

            throw new BlockVersionNotSupportedException(version, BlockType);
        }
    }
}
