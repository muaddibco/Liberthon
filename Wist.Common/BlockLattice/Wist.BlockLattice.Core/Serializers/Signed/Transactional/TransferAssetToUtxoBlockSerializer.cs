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
    public class TransferAssetToUtxoBlockSerializer : TransactionalSerializerBase<TransferAssetToUtxoBlock>
    {
        public TransferAssetToUtxoBlockSerializer(ICryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository) 
            : base(PacketType.Transactional, BlockTypes.Transaction_TransferAssetsToUtxo, cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository)
        {
        }

        protected override void WriteBody(BinaryWriter bw)
        {
            base.WriteBody(bw);

            bw.Write(_block.DestinationKey);
            bw.Write(_block.TransactionPublicKey);
            bw.Write(_block.AssetId);
            bw.Write(_block.AssetCommitment);
            for (int i = 0; i < _block.SurjectionProof.AssetCommitments.Length; i++)
            {
                bw.Write(_block.SurjectionProof.AssetCommitments[i]);
            }
            bw.Write(_block.SurjectionProof.Rs.E);
            for (int i = 0; i < _block.SurjectionProof.Rs.S.Length; i++)
            {
                bw.Write(_block.SurjectionProof.Rs.S[i]);
            }
            bw.Write(_block.EcdhTuple.Mask);
            bw.Write(_block.EcdhTuple.AssetId);
        }
    }
}
