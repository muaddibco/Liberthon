using System.IO;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Serializers.Signed.Transactional
{
    [RegisterExtension(typeof(ISerializer), Lifetime = LifetimeManagement.TransientPerResolve)]
    public class AcceptAssetTransitionBlockSerializer : TransactionalSerializerBase<AcceptAssetTransitionBlock>
    {
        public AcceptAssetTransitionBlockSerializer(ICryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository) 
            : base(PacketType.Transactional, BlockTypes.Transaction_AcceptAssetTransition, cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository)
        {
        }

        protected override void WriteBody(BinaryWriter bw)
        {
            base.WriteBody(bw);

            bw.Write(_block.AcceptedTransactionKey);
            bw.Write(_block.AcceptedCommitment);
            bw.Write(_block.AcceptedBlindingFactor);
            bw.Write(_block.AcceptedAssetId);
        }
    }
}
