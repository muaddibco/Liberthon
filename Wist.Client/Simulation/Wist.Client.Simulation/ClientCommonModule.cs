using System;
using Wist.Client.Common.Communication;
using Wist.Client.Common.Entities;
using Wist.Client.Common.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Cryptography;
using Wist.Core.Logging;
using Wist.Core.Modularity;
using Wist.Crypto.ConfidentialAssets;

namespace Wist.Client.Simulation
{
    [RegisterExtension(typeof(IModule), Lifetime = LifetimeManagement.Singleton)]
    public class ClientCommonModule : ModuleBase
    {
        private readonly INetworkSynchronizer _networkSynchronizer;
        private readonly IWalletManager _walletManager;

        public ClientCommonModule(ILoggerService loggerService, INetworkSynchronizer networkSynchronizer, IWalletManager walletManager) : base(loggerService)
        {
            _networkSynchronizer = networkSynchronizer;
            _walletManager = walletManager;
        }

        public override string Name => nameof(ClientCommonModule);

        public override void Start()
        {
            _networkSynchronizer.Start();

            byte[][] assetIds = new byte[][] { CryptoHelper.GetRandomSeed(), CryptoHelper.GetRandomSeed(), CryptoHelper.GetRandomSeed() };
            _walletManager.IssueAssets("Vote for President", assetIds, new string[] { "Asset 1", "Asset 2", "Asset 3" }, 5005);

            Console.WriteLine("Check that Assets Issuance passed successfully and press <Enter>. Otherwise type 'exit' and press <Enter>.");
            string cmd = Console.ReadLine();

            if("exit".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            ConfidentialAccount confidentialAccount = new ConfidentialAccount
            {
                PublicViewKey = ConfidentialAssetsHelper.GetTrancationKey(CryptoHelper.GetRandomSeed()),
                PublicSpendKey = ConfidentialAssetsHelper.GetTrancationKey(CryptoHelper.GetRandomSeed())
            };

            _walletManager.SendAssetToUtxo(assetIds, 1, 5005, confidentialAccount);
        }

        protected override void InitializeInner()
        {
            _networkSynchronizer.Initialize(_cancellationToken);
        }
    }
}
