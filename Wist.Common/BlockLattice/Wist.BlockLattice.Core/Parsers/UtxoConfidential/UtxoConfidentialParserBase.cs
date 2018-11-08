using System;
using System.Buffers.Binary;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Cryptography;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Parsers.UtxoConfidential
{
    public abstract class UtxoConfidentialParserBase : BlockParserBase
    {
        public UtxoConfidentialParserBase(IIdentityKeyProvidersRegistry identityKeyProvidersRegistry) : base(identityKeyProvidersRegistry)
        {
        }

        public override PacketType PacketType => PacketType.UtxoConfidential;

        protected override BlockBase ParseBlockBase(ushort version, Memory<byte> spanBody, out Memory<byte> spanPostBody)
        {
            int readBytes = 0;

            ulong tagId = BinaryPrimitives.ReadUInt64LittleEndian(spanBody.Span.Slice(readBytes));
            readBytes += 8;
            Memory<byte> keyImage = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE);
            readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

            byte[] destinationKey = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
            readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

            byte[] transactionPublicKey = spanBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
            readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

            spanPostBody = ParseUtxoConfidential(version, spanBody.Slice(readBytes), out UtxoConfidentialBase utxoConfidentialBase);

            ushort ringSignaturesCount = BinaryPrimitives.ReadUInt16LittleEndian(spanPostBody.Span);

            utxoConfidentialBase.TagId = tagId;
            utxoConfidentialBase.KeyImage = _entityIdentityKeyProvider.GetKey(keyImage);
            utxoConfidentialBase.DestinationKey = destinationKey;
            utxoConfidentialBase.TransactionPublicKey = transactionPublicKey;
            utxoConfidentialBase.PublicKeys = new IKey[ringSignaturesCount];
            utxoConfidentialBase.Signatures = new RingSignature[ringSignaturesCount];

            readBytes = 2;

            for (int i = 0; i < ringSignaturesCount; i++)
            {
                byte[] publicKey = spanPostBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                IKey key = _entityIdentityKeyProvider.GetKey(spanPostBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE));
                utxoConfidentialBase.PublicKeys[i] = key;
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;
            }

            for (int i = 0; i < ringSignaturesCount; i++)
            {
                byte[] c = spanPostBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                byte[] r = spanPostBody.Slice(readBytes, Globals.NODE_PUBLIC_KEY_SIZE).ToArray();
                readBytes += Globals.NODE_PUBLIC_KEY_SIZE;

                RingSignature ringSignature = new RingSignature { C = c, R = r };
                utxoConfidentialBase.Signatures[i] = ringSignature;
            }

            return utxoConfidentialBase;
        }

        protected override Memory<byte> SliceInitialBytes(Memory<byte> span, out Memory<byte> spanHeader)
        {
            Memory<byte> span1 = base.SliceInitialBytes(span, out spanHeader);

            spanHeader = span.Slice(0, spanHeader.Length + Globals.SYNC_BLOCK_HEIGHT_LENGTH + Globals.NONCE_LENGTH + Globals.POW_HASH_SIZE);

            return span1.Slice(Globals.SYNC_BLOCK_HEIGHT_LENGTH + Globals.NONCE_LENGTH + Globals.POW_HASH_SIZE);
        }

        protected override Memory<byte> FillBlockBaseHeader(BlockBase blockBase, Memory<byte> spanHeader)
        {
            UtxoConfidentialBase utxoConfidentialBase = (UtxoConfidentialBase)blockBase;

            spanHeader = base.FillBlockBaseHeader(blockBase, spanHeader);

            utxoConfidentialBase.SyncBlockHeight = BinaryPrimitives.ReadUInt64LittleEndian(spanHeader.Span);
            utxoConfidentialBase.Nonce = BinaryPrimitives.ReadUInt32LittleEndian(spanHeader.Slice(Globals.SYNC_BLOCK_HEIGHT_LENGTH).Span);
            utxoConfidentialBase.PowHash = spanHeader.Slice(Globals.SYNC_BLOCK_HEIGHT_LENGTH + Globals.NONCE_LENGTH, Globals.POW_HASH_SIZE).ToArray();

            return spanHeader.Slice(Globals.SYNC_BLOCK_HEIGHT_LENGTH + Globals.NONCE_LENGTH + Globals.POW_HASH_SIZE);
        }

        protected abstract Memory<byte> ParseUtxoConfidential(ushort version, Memory<byte> spanBody, out UtxoConfidentialBase utxoConfidentialBase);
    }
}
