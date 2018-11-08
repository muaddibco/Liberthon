using System;
using System.Buffers.Binary;
using Wist.BlockLattice.Core.DataModel.Registry;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.BlockLattice.Core.Exceptions;
using Wist.BlockLattice.Core.Parsers.UtxoConfidential;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Parsers.Registry
{
    [RegisterExtension(typeof(IBlockParser), Lifetime = LifetimeManagement.Singleton)]
    public class RegistryRegisterUtxoConfidentialBlockParser : UtxoConfidentialParserBase
    {
        public RegistryRegisterUtxoConfidentialBlockParser(IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) : base(identityKeyProvidersRegistry)
        {
        }

        public override PacketType PacketType => PacketType.Registry;

        public override ushort BlockType => BlockTypes.Registry_RegisterUtxoConfidential;

        protected override Memory<byte> ParseUtxoConfidential(ushort version, Memory<byte> spanBody, out UtxoConfidentialBase utxoConfidentialBase)
        {
            if(version == 1)
            {
                int readBytes = 0;

                PacketType referencedPacketType = (PacketType)BinaryPrimitives.ReadUInt16LittleEndian(spanBody.Span);
                readBytes += 2;

                ushort referencedBlockType = BinaryPrimitives.ReadUInt16LittleEndian(spanBody.Span.Slice(readBytes));
                readBytes += 2;

                byte[] referencedBlockHash = spanBody.Slice(readBytes, Globals.DEFAULT_HASH_SIZE).ToArray();
                readBytes += Globals.DEFAULT_HASH_SIZE;

                byte[] referencedTarget = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                byte[] referencedTransactionKey = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                RegistryRegisterUtxoConfidentialBlock registryRegisterUtxoConfidentialBlock = new RegistryRegisterUtxoConfidentialBlock
                {
                    ReferencedPacketType = referencedPacketType,
                    ReferencedBlockType = referencedBlockType,
                    ReferencedBodyHash = referencedBlockHash,
                    DestinationKey = referencedTarget,
                    TransactionPublicKey = referencedTransactionKey
                };

                utxoConfidentialBase = registryRegisterUtxoConfidentialBlock;

                return spanBody.Slice(4 + Globals.DEFAULT_HASH_SIZE + Globals.NODE_PUBLIC_KEY_SIZE);
            }

            throw new BlockVersionNotSupportedException(version, BlockType);
        }
    }
}
