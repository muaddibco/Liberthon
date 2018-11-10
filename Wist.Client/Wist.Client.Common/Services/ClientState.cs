using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Wist.BlockLattice.Core;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.HashCalculations;
using Wist.Core.States;

namespace Wist.Client.Common.Services
{
    [RegisterExtension(typeof(IState), Lifetime = LifetimeManagement.Singleton)]
    public class ClientState : IClientState
    {
        private readonly Subject<string> _subject = new Subject<string>();
        private readonly ICryptoService _cryptoService;
        private readonly IHashCalculation _defaultHashCalculation;

        private bool _isConfidential;
        private byte[] _secretKey;
        private byte[] _secretViewKey;
        private byte[] _publicViewKey;
        private byte[] _publicSpendKey;
        private byte[] _publicSpendKeyHash;

        public string Name => nameof(IClientState);

        public ClientState(ICryptoService cryptoService, IHashCalculationsRepository hashCalculationsRepository)
        {
            _cryptoService = cryptoService;
            _defaultHashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
        }

        public byte[] GetPublicViewKey()
        {
            return _publicViewKey;
        }

        public byte[] GetPublicSpendKey()
        {
            return _publicSpendKey;
        }

        public byte[] GetSecretViewKey()
        {
            return _secretViewKey;
        }

        public void InitializeConfidential(byte[] secretViewKey, byte[] secretSpendKey)
        {
            _isConfidential = true;
            _secretViewKey = secretViewKey;
            _publicViewKey = _cryptoService.GetPublicKeyFromSecretKey(secretViewKey);
            _secretKey = secretSpendKey;
            _publicSpendKey = _cryptoService.GetPublicKeyFromSecretKey(_secretKey);
        }

        public IDisposable SubscribeOnStateChange(ITargetBlock<string> targetBlock)
        {
            return _subject.Subscribe(targetBlock.AsObserver());
        }

        public void InitializeAccountBased(byte[] seed)
        {
            _isConfidential = false;
            _secretKey = seed;
            _publicSpendKey = _cryptoService.GetPublicKeyFromSeed(_secretKey);
            _publicSpendKeyHash = _defaultHashCalculation.CalculateHash(_publicSpendKey);
        }

        public bool IsConfidential()
        {
            return _isConfidential;
        }

        public byte[] GetPublicKey()
        {
            return _publicSpendKey;
        }

        public byte[] GetPublicKeyHash()
        {
            return _publicSpendKeyHash;
        }

        public byte[] GetSecretSpendKey() => _secretKey;
    }
}
