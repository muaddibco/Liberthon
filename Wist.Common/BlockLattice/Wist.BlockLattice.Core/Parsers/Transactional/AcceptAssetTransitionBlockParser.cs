using System;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Exceptions;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Parsers.Transactional
{
    [RegisterExtension(typeof(IBlockParser), Lifetime = LifetimeManagement.Singleton)]
    public class AcceptAssetTransitionBlockParser : TransactionalBlockParserBase
    {
        public AcceptAssetTransitionBlockParser(IHashCalculationsRepository proofOfWorkCalculationRepository, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) 
            : base(proofOfWorkCalculationRepository, identityKeyProvidersRegistry)
        {
        }

        public override ushort BlockType => BlockTypes.Transaction_AcceptAssetTransition;

        protected override Memory<byte> ParseTransactional(ushort version, Memory<byte> spanBody, uint assetsCount, out TransactionalBlockBase transactionalBlockBase)
        {
            if (version == 1)
            {
                int readBytes = 0;

                byte[] acceptedTransactionKey = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                byte[] acceptedCommitment = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                byte[] acceptedBlindingFactor = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                byte[] acceptedAssetId = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                AcceptAssetTransitionBlock acceptAssetTransitionBlock = new AcceptAssetTransitionBlock
                {
                    AcceptedTransactionKey = acceptedTransactionKey,
                    AcceptedCommitment = acceptedCommitment,
                    AcceptedBlindingFactor = acceptedBlindingFactor,
                    AcceptedAssetId = acceptedAssetId
                };

                transactionalBlockBase = acceptAssetTransitionBlock;

                return spanBody.Slice(readBytes);
            }

            throw new BlockVersionNotSupportedException(version, BlockType);
        }
    }
}
