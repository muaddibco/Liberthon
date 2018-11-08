using System;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Exceptions;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Identity;
using Wist.Core.HashCalculations;

namespace Wist.BlockLattice.Core.Parsers.Transactional
{
    [RegisterExtension(typeof(IBlockParser), Lifetime = LifetimeManagement.Singleton)]
    public class TransferFundsBlockParser : TransactionalBlockParserBase
    {
        public TransferFundsBlockParser(IHashCalculationsRepository proofOfWorkCalculationRepository, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) 
            : base(proofOfWorkCalculationRepository, identityKeyProvidersRegistry)
        {
        }

        public override ushort BlockType => BlockTypes.Transaction_TransferFunds;

        protected override Memory<byte> ParseTransactional(ushort version, Memory<byte> spanBody, uint assetsCount, out TransactionalBlockBase transactionalBlockBase)
        {
            TransactionalBlockBase block = null;

            if (version == 1)
            {
                byte[] target = spanBody.Slice(0, Globals.DEFAULT_HASH_SIZE).ToArray();

                block = new TransferFundsBlock
                {
                    TargetOriginalHash = target
                };

                transactionalBlockBase = block;

                return spanBody.Slice(Globals.DEFAULT_HASH_SIZE);
            }

            throw new BlockVersionNotSupportedException(version, BlockType);
        }
    }
}
