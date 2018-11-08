using System;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Identity;
using Wist.Core.HashCalculations;
using System.Buffers.Binary;

namespace Wist.BlockLattice.Core.Parsers.Transactional
{
    public abstract class TransactionalBlockParserBase : SyncLinkedBlockParserBase
    {
        private readonly IHashCalculationsRepository _proofOfWorkCalculationRepository;

        public TransactionalBlockParserBase(IHashCalculationsRepository proofOfWorkCalculationRepository, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) 
            : base(identityKeyProvidersRegistry, proofOfWorkCalculationRepository)
        {
            _proofOfWorkCalculationRepository = proofOfWorkCalculationRepository;
        }

        public override PacketType PacketType => PacketType.Transactional;

        protected override Memory<byte> ParseSyncLinked(ushort version, Memory<byte> spanBody, out SyncedLinkedBlockBase syncedBlockBase)
        {
            int readBytes = 0;
            ulong tagId = BinaryPrimitives.ReadUInt64LittleEndian(spanBody.Span.Slice(readBytes));
            readBytes += 8;
            ulong funds = BinaryPrimitives.ReadUInt64LittleEndian(spanBody.Span.Slice(readBytes));
            readBytes += 8;
            uint assetsCount = BinaryPrimitives.ReadUInt32LittleEndian(spanBody.Span.Slice(readBytes));
            readBytes += 4;

            byte[][] assetIds = new byte[assetsCount][];
            ulong[] assetAmounts = new ulong[assetsCount];

            for (int i = 0; i < assetsCount; i++)
            {
                assetIds[i] = spanBody.Slice(readBytes, 32).ToArray();
                readBytes += 32;
            }

            for (int i = 0; i < assetsCount; i++)
            {
                assetAmounts[i] = BinaryPrimitives.ReadUInt64LittleEndian(spanBody.Slice(readBytes).Span);
                readBytes += 8;
            }

            Memory<byte> spanPostBody = ParseTransactional(version, spanBody.Slice(readBytes), assetsCount, out TransactionalBlockBase transactionalBlockBase);
            transactionalBlockBase.TagId = tagId;
            transactionalBlockBase.UptodateFunds = funds;
            transactionalBlockBase.AssetIds = assetIds;
            transactionalBlockBase.AssetAmounts = assetAmounts;
            syncedBlockBase = transactionalBlockBase;

            return spanPostBody;
        }

        protected abstract Memory<byte> ParseTransactional(ushort version, Memory<byte> spanBody, uint assetsCount, out TransactionalBlockBase transactionalBlockBase);
    }
}
