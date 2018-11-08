using System;
using System.IO;
using Wist.BlockLattice.Core.DataModel;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Serializers.Signed
{
    public abstract class SignatureSupportSerializerBase<T> : SerializerBase<T> where T : SignedBlockBase
    {
        protected readonly ICryptoService _cryptoService;
        protected readonly IIdentityKeyProvider _transactionKeyIdentityKeyProvider;
        protected readonly IHashCalculation _transactionKeyHashCalculation;

        public SignatureSupportSerializerBase(PacketType packetType, ushort blockType, ICryptoService cryptoService, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry, IHashCalculationsRepository hashCalculationsRepository)
            : base(packetType, blockType)
        {
                _cryptoService = cryptoService;
                _transactionKeyIdentityKeyProvider = identityKeyProvidersRegistry.GetTransactionsIdenityKeyProvider();
                _transactionKeyHashCalculation = hashCalculationsRepository.Create(HashType.MurMur);
        }

        protected virtual void WriteHeader(BinaryWriter bw)
        {
            bw.Write((ushort)PacketType);
        }

        protected abstract void WriteBody(BinaryWriter bw);

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

            WriteBody(_binaryWriter);

            bodyLength = _memoryStream.Position - pos;
        }

        private void FinalizeTransaction(long pos, long bodyLength)
        {
            _memoryStream.Seek(pos, SeekOrigin.Begin);

            byte[] body = _binaryReader.ReadBytes((int)bodyLength);
            byte[] signature = _cryptoService.Sign(body);
            byte[] signer = _cryptoService.PublicKey.Value.ToArray();

            _binaryWriter.Write(signature);
            _binaryWriter.Write(_cryptoService.PublicKey.Value.ToArray());

            Memory<byte> memory = _memoryStream.ToArray();

            _block.Signature = memory.Slice(memory.Length - signature.Length - signer.Length, signature.Length);
            _block.Signer = _cryptoService.PublicKey;
            _block.RawData = memory;
            _block.BodyBytes = memory.Slice((int)pos, (int)bodyLength);
            //_block.NonHeaderBytes = memory.Slice((int)pos);

            byte[] hash = _transactionKeyHashCalculation.CalculateHash(_block.RawData.ToArray());
            _block.Key = _transactionKeyIdentityKeyProvider.GetKey(hash);
        }
        
        #endregion Private Functions
    }
}
