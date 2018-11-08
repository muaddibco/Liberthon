using Chaos.NaCl;
using System;
using Wist.BlockLattice.Core.Enums;
using Wist.Core.Identity;
using Wist.Tests.Core.Fixtures;
using Xunit;
using NSubstitute;
using Wist.Core.HashCalculations;
using Wist.Core.Cryptography;
using Wist.BlockLattice.Core.Parsers;
using CommonServiceLocator;
using Unity;
using Wist.Crypto.HashCalculations;

namespace Wist.BlockLattice.Core.Tests
{
    public abstract class TestBase : IClassFixture<DependencyInjectionSupportFixture>
    {
        protected IIdentityKeyProvider _transactionHashKeyProvider;
        protected IIdentityKeyProvider _identityKeyProvider;
        protected IIdentityKeyProvidersRegistry _identityKeyProvidersRegistry;
        protected IHashCalculationsRepository _hashCalculationRepository;
        protected IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
        protected IBlockParsersRepository _blockParsersRepository;
        protected ICryptoService _cryptoService;
        protected byte[] _privateKey;
        protected byte[] _publicKey;
        protected byte[] _expandedPrivateKey;

        public TestBase()
        {
            _transactionHashKeyProvider = Substitute.For<IIdentityKeyProvider>();
            _identityKeyProvider = Substitute.For<IIdentityKeyProvider>();
            _identityKeyProvidersRegistry = Substitute.For<IIdentityKeyProvidersRegistry>();
            _hashCalculationRepository = Substitute.For<IHashCalculationsRepository>();
            _blockParsersRepositoriesRepository = Substitute.For<IBlockParsersRepositoriesRepository>();
            _blockParsersRepository = Substitute.For<IBlockParsersRepository>();
            _cryptoService = Substitute.For<ICryptoService>();

            _identityKeyProvidersRegistry.GetInstance().Returns(_identityKeyProvider);
            _identityKeyProvidersRegistry.GetTransactionsIdenityKeyProvider().Returns(_transactionHashKeyProvider);
            _identityKeyProvider.GetKey(null).ReturnsForAnyArgs(c => new Key32() { Value = c.Arg<Memory<byte>>() });
            _transactionHashKeyProvider.GetKey(null).ReturnsForAnyArgs(c => new Key16() { Value = c.Arg<Memory<byte>>() });
            _hashCalculationRepository.Create(HashType.MurMur).Returns(new MurMurHashCalculation());
            _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Registry).ReturnsForAnyArgs(_blockParsersRepository);

            _privateKey = CryptoHelper.GetRandomSeed();
            Ed25519.KeyPairFromSeed(out _publicKey, out _expandedPrivateKey, _privateKey);

            _cryptoService.Sign(null).ReturnsForAnyArgs(c => Ed25519.Sign(c.Arg<byte[]>(), _expandedPrivateKey));
            _cryptoService.PublicKey.Returns(new Key32() { Value = _publicKey });


            ServiceLocator.Current.GetInstance<IUnityContainer>().RegisterInstance(_blockParsersRepositoriesRepository);
        }
    }
}
