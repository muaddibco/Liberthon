using System.IO;
using System.Text;
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
    public class IssueAssetsBlockSerializer : TransactionalSerializerBase<IssueAssetsBlock>
    {
        public IssueAssetsBlockSerializer(ICryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository) 
            : base(PacketType.Transactional, BlockTypes.Transaction_IssueAssets, cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository)
        {
        }

        protected override void WriteBody(BinaryWriter bw)
        {
            base.WriteBody(bw);

            uint count = (uint)_block.IssuedAssetIds.Length;
            bw.Write(count);

            for (int i = 0; i < count; i++)
            {
                bw.Write(_block.IssuedAssetIds[i]);
            }

            for (int i = 0; i < count; i++)
            {
                byte strLen = (byte)_block.IssuedAssetInfo[i].Length;
                bw.Write(strLen);
                bw.Write(Encoding.ASCII.GetBytes(_block.IssuedAssetInfo[i].Substring(0, strLen)));
            }

            byte strLen2 = (byte)_block.IssuanceInfo.Length;
            bw.Write(strLen2);
            bw.Write(Encoding.ASCII.GetBytes(_block.IssuanceInfo.Substring(0, strLen2)));
        }
    }
}
