using System.IO;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Serializers.Signed.Registry
{
    [RegisterExtension(typeof(ISerializer), Lifetime = LifetimeManagement.TransientPerResolve)]
    public class RegistryFullBlockSerializer : SyncSupportSerializerBase<RegistryFullBlock>
    {
        private readonly RegistryRegisterBlockSerializer _transactionRegisterBlockSerializer;
        private readonly RegistryRegisterUtxoConfidentialBlockSerializer _registryRegisterUtxoConfidentialBlockSerializer;

        public RegistryFullBlockSerializer(ICryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository) 
            : base(PacketType.Registry, BlockTypes.Registry_FullBlock, cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository)
        {
            _transactionRegisterBlockSerializer = new RegistryRegisterBlockSerializer(cryptoService, identityKeyProvidersRegistry, hashCalculationsRepository);
            _registryRegisterUtxoConfidentialBlockSerializer = new RegistryRegisterUtxoConfidentialBlockSerializer(identityKeyProvidersRegistry, hashCalculationsRepository);
        }

        protected override void WriteBody(BinaryWriter bw)
        {
            base.WriteBody(bw);

            bw.Write((ushort)_block.TransactionHeaders.Count);

            foreach (var item in _block.TransactionHeaders)
            {
                bw.Write(item.Key);
                if (item.Value.BlockType == BlockTypes.Registry_Register)
                {
                    _transactionRegisterBlockSerializer.Initialize(item.Value as BlockBase);
                    bw.Write(_transactionRegisterBlockSerializer.GetBytes());
                }
                else if(item.Value.BlockType == BlockTypes.Registry_RegisterUtxoConfidential)
                {
                    _registryRegisterUtxoConfidentialBlockSerializer.Initialize(item.Value as BlockBase);
                    bw.Write(_registryRegisterUtxoConfidentialBlockSerializer.GetBytes());
                }
            }

            bw.Write(_block.ShortBlockHash);
        }
    }
}
