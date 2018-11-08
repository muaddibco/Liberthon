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
    public class NonQuantitativeTransitionAssetTransferBlockSerializer : UtxoConfidentialSerializerBase<NonQuantitativeTransitionAssetTransferBlock>
    {
        public NonQuantitativeTransitionAssetTransferBlockSerializer(IUtxoConfidentialCryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository)
            : base(PacketType.UtxoConfidential, BlockTypes.UtxoConfidential_NonQuantitativeAssetTransfer, cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository)
        {
        }

        protected override void WriteBody(BinaryWriter bw)
        {
            WriteCommitmentAndProof(bw, _block.AssetCommitment, _block.SurjectionProof);
            WriteCommitmentAndProof(bw, _block.AffiliationCommitment, _block.AffiliationSurjectionProof);

            bw.Write((ushort)_block.AffiliationPseudoKeys.Length);
            for (int i = 0; i < _block.AffiliationPseudoKeys.Length; i++)
            {
                bw.Write(_block.AffiliationPseudoKeys[i]);
            }

            bw.Write(_block.AffiliationBorromeanSignature.E);
            bw.Write((ushort)_block.AffiliationBorromeanSignature.S.Length);
            for (int i = 0; i < _block.AffiliationBorromeanSignature.S.Length; i++)
            {
                bw.Write(_block.AffiliationBorromeanSignature.S[i]);
            }

            WriteCommitmentAndProof(bw, null, _block.AffiliationEvidenceSurjectionProof);

            bw.Write(_block.EcdhTuple.Mask);
            bw.Write(_block.EcdhTuple.AssetId);
        }

        private void WriteCommitmentAndProof(BinaryWriter bw, byte[] commitment, SurjectionProof surjectionProof)
        {
            if (commitment != null)
            {
                bw.Write(commitment);
            }

            bw.Write((ushort)surjectionProof.AssetCommitments.Length);
            for (int i = 0; i < surjectionProof.AssetCommitments.Length; i++)
            {
                bw.Write(surjectionProof.AssetCommitments[i]);
            }

            bw.Write(surjectionProof.Rs.E);

            for (int i = 0; i < surjectionProof.AssetCommitments.Length; i++)
            {
                bw.Write(surjectionProof.Rs.S[i]);
            }
        }
    }
}
