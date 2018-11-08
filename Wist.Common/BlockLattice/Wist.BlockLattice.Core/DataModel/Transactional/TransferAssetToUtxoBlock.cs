using Wist.BlockLattice.Core.Enums;
using Wist.Core.Cryptography;

namespace Wist.BlockLattice.Core.DataModel.Transactional
{
    public class TransferAssetToUtxoBlock : TransactionalBlockBase
    {
        public override ushort Version => 1;

        public override ushort BlockType => BlockTypes.Transaction_TransferAssetsToUtxo;

        /// <summary>
        /// P = Hs(r * A) * G + B where A is receiver's Public View Key and B is receiver's Public Spend Key
        /// </summary>
        public byte[] DestinationKey { get; set; }

        /// <summary>
        /// R = r * G. 'r' can be erased after transaction sent unless sender wants to proof he sent funds to particular destination address.
        /// </summary>
        public byte[] TransactionPublicKey { get; set; }

        public byte[] AssetId { get; set; }

        /// <summary>
        /// C = x * G + I, where I is elliptic curve point representing assert id
        /// </summary>
        public byte[] AssetCommitment { get; set; }

        public SurjectionProof SurjectionProof { get; set; }

        /// <summary>
        /// Contains encrypted blinding factor of AssetCommitment: x` = x ^ (r * A). To decrypt receiver makes (R * a) ^ x` = x.
        /// </summary>
        public EcdhTupleCA EcdhTuple { get; set; }
    }
}
