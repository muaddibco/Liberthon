using System.IO;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Serializers.UtxoConfidential
{
    [RegisterExtension(typeof(ISerializer), Lifetime = LifetimeManagement.TransientPerResolve)]
    public class NonQuantitativeAssetTransferBlockSerializer : UtxoConfidentialSerializerBase<NonQuantitativeAssetTransferBlock>
    {
        public NonQuantitativeAssetTransferBlockSerializer(IUtxoConfidentialCryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository) 
            : base(PacketType.UtxoConfidential, BlockTypes.UtxoConfidential_NonQuantitativeAssetTransfer, cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository)
        {
        }

        protected override void WriteBody(BinaryWriter bw)
        {
            bw.Write(_block.AssetCommitment);
            bw.Write((ushort)_block.SurjectionProof.AssetCommitments.Length);
            for (int i = 0; i < _block.SurjectionProof.AssetCommitments.Length; i++)
            {
                bw.Write(_block.SurjectionProof.AssetCommitments[i]);
            }

            bw.Write(_block.SurjectionProof.Rs.E);

            for (int i = 0; i < _block.SurjectionProof.AssetCommitments.Length; i++)
            {
                bw.Write(_block.SurjectionProof.Rs.S[i]);
            }

            bw.Write(_block.EcdhTuple.Mask);
        }
    }
}
