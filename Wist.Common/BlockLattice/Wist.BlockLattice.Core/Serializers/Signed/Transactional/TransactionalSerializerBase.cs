using System.IO;
using Wist.BlockLattice.Core.DataModel.Transactional;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Serializers.Signed.Transactional
{
    public abstract class TransactionalSerializerBase<T> : SyncLinkedSupportSerializerBase<T> where T : TransactionalBlockBase
    {
        public TransactionalSerializerBase(PacketType packetType, ushort blockType, ICryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository) 
            : base(packetType, blockType, cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository)
        {
        }

        protected override void WriteBody(BinaryWriter bw)
        {
            base.WriteBody(bw);

            bw.Write(_block.TagId);

            bw.Write(_block.UptodateFunds);

            uint assetCount = (uint)(_block.AssetIds?.Length ?? 0);
            bw.Write(assetCount);
            if (assetCount > 0)
            {
                for (int i = 0; i < _block.AssetIds.Length; i++)
                {
                    bw.Write(_block.AssetIds[i]);
                }
                for (int i = 0; i < _block.AssetAmounts.Length; i++)
                {
                    bw.Write(_block.AssetAmounts[i]);
                }
            }
        }
    }
}
