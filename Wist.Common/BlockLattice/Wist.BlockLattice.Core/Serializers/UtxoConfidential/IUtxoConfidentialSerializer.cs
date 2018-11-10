using Wist.BlockLattice.Core.DataModel.UtxoConfidential;

namespace Wist.BlockLattice.Core.Serializers.UtxoConfidential
{
    public interface IUtxoConfidentialSerializer : ISerializer
    {
        void Initialize(UtxoConfidentialBase utxoConfidentialBase, byte[] prevSecretKey, int prevSecretKeyIndex);
    }
}
