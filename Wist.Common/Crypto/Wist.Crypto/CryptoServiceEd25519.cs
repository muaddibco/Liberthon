using Chaos.NaCl;
using Chaos.NaCl.Internal.Ed25519Ref10;
using System;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.Identity;

namespace Wist.Crypto
{
    [RegisterDefaultImplementation(typeof(ICryptoService), Lifetime = LifetimeManagement.Singleton)]
    public class CryptoServiceEd25519 : ICryptoService
    {
        private byte[] _expandedPrivateKey;
        private readonly IHashCalculation _transactionKeyCalculation;
        private readonly IIdentityKeyProvider _identityKeyProvider;

        public CryptoServiceEd25519(IHashCalculationsRepository hashCalculationRepository, IIdentityKeyProvidersRegistry identityKeyProvidersRegistry)
        {
            _transactionKeyCalculation = hashCalculationRepository.Create(HashType.MurMur);
            _identityKeyProvider = identityKeyProvidersRegistry.GetInstance();
        }

        public IKey PublicKey { get; private set; }

        public byte[] ComputeTransactionKey(byte[] bytes) => _transactionKeyCalculation.CalculateHash(bytes);
        public byte[] ComputeTransactionKey(Memory<byte> bytes) => _transactionKeyCalculation.CalculateHash(bytes);

        public void Initialize(byte[] privateKey)
        {
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            Ed25519.KeyPairFromSeed(out byte[] publicKey, out _expandedPrivateKey, privateKey);

            PublicKey = _identityKeyProvider.GetKey(publicKey);
        }

        public byte[] Sign(byte[] message)
        {
            return Ed25519.Sign(message, _expandedPrivateKey);
        }

        public bool Verify(byte[] signature, byte[] message, byte[] publickKey)
        {
            return Ed25519.Verify(signature, message, publickKey);
        }

        public byte[] GetPublicKeyFromSecretKey(byte[] secretKey)
        {
            GroupOperations.ge_scalarmult_base(out GroupElementP3 p3, secretKey, 0);
            byte[] pk = new byte[32];

            GroupOperations.ge_p3_tobytes(pk, 0, ref p3);

            return pk;
        }

        public byte[] GetPublicKeyFromSeed(byte[] seed)
        {
            return Ed25519.PublicKeyFromSeed(seed);
        }
    }

}
