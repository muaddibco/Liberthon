﻿using System;
using System.IO;
using System.Linq;
using Wist.BlockLattice.Core.DataModel.UtxoConfidential;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Cryptography;
using Wist.Core.ExtensionMethods;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Serializers.UtxoConfidential
{
    public abstract class UtxoConfidentialSerializerBase<T> : SerializerBase<T> where T : UtxoConfidentialBase
    {
        protected readonly IIdentityKeyProvider _transactionKeyIdentityKeyProvider;
        protected readonly IHashCalculation _transactionKeyHashCalculation;

        protected int _prevSecretKeyIndex;
        protected byte[] _prevSecretKey;

        public UtxoConfidentialSerializerBase(PacketType packetType, ushort blockType, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository) 
            : base(packetType, blockType)
        {
            _transactionKeyIdentityKeyProvider = identityKeyProvidersRegistry.GetTransactionsIdenityKeyProvider();
            _transactionKeyHashCalculation = hashCalculationsRepository.Create(HashType.MurMur);
        }

        protected virtual void WriteHeader(BinaryWriter bw)
        {
            bw.Write((ushort)PacketType);
            bw.Write(_block.SyncBlockHeight);
            bw.Write(_block.Nonce);
            bw.Write(_block.PowHash);
        }

        public override void FillBodyAndRowBytes()
        {
            if (_block == null || _bytesFilled)
            {
                return;
            }

            FillHeader();

            FillBody(out long pos, out long bodyLength);

            FinalizeTransaction(pos, bodyLength);

            _bytesFilled = true;
        }

        protected abstract void WriteBody(BinaryWriter bw);

        #region Private Functions

        private void FillHeader()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);
            _memoryStream.SetLength(0);

            WriteHeader(_binaryWriter);
        }

        private void FillBody(out long pos, out long bodyLength)
        {
            pos = _memoryStream.Position;
            _binaryWriter.Write(_block.Version);
            _binaryWriter.Write(_block.BlockType);
            _binaryWriter.Write(_block.TagId);
            _binaryWriter.Write(_block.KeyImage.Value.ToArray());
            _binaryWriter.Write(_block.DestinationKey);
            _binaryWriter.Write(_block.TransactionPublicKey);

            WriteBody(_binaryWriter);

            bodyLength = _memoryStream.Position - pos;
        }

        private void FinalizeTransaction(long pos, long bodyLength)
        {
            _memoryStream.Seek(pos, SeekOrigin.Begin);

            byte[] body = _binaryReader.ReadBytes((int)bodyLength);

            RingSignature[] ringSignatures = _block.Signatures; // _cryptoService.Sign(body, _block.KeyImage.Value.ToArray(), _block.PublicKeys, _prevSecretKey, _prevSecretKeyIndex);

            _binaryWriter.Write((ushort)ringSignatures.Length);

            foreach (IKey key in _block.PublicKeys)
            {
                _binaryWriter.Write(key.ArraySegment.Array, key.ArraySegment.Offset, key.ArraySegment.Count);
            }

            foreach (var signature in ringSignatures)
            {
                _binaryWriter.Write(signature.C);
                _binaryWriter.Write(signature.R);
            }

            Memory<byte> memory = _memoryStream.ToArray();

            _block.RawData = memory;
            _block.BodyBytes = memory.Slice((int)pos, (int)bodyLength);
            //_block.NonHeaderBytes = memory.Slice((int)pos);

            byte[] hash = _transactionKeyHashCalculation.CalculateHash(_block.RawData.ToArray());
            _block.Key = _transactionKeyIdentityKeyProvider.GetKey(hash);
        }

        #endregion Private Functions
    }
}
