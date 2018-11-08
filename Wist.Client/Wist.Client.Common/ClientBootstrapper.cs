using CommonServiceLocator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity;
using Wist.Client.Common.Configuration;
using Wist.Client.Common.Services;
using Wist.Core.Architecture;
using Wist.Core.Configuration;
using Wist.Core.Cryptography;
using Wist.Core.ExtensionMethods;
using Wist.Core.Modularity;
using Wist.Core.States;

namespace Wist.Client.Common
{
    public class ClientBootstrapper : Bootstrapper
    {
        private bool _modulesConfigured;

        public ClientBootstrapper(CancellationToken ct) : base(ct)
        {
        }

        protected override IEnumerable<string> EnumerateCatalogItems(string rootFolder)
        {
            return base.EnumerateCatalogItems(rootFolder).Union(new string[] { "Wist.Network.dll", "Wist.Client.DataModel.dll", "Wist.Crypto.dll", "Wist.BlockLattice.Core.dll", "Wist.Client.Common.dll" });
        }

        public new IUnityContainer CreateContainer()
        {
            Container = base.CreateContainer();

            return Container;
        }

        public new void ConfigureServiceLocator()
        {
            base.ConfigureServiceLocator();
        }

        public new void ConfigureContainer()
        {
            base.ConfigureContainer();
        }

        public void Initialize()
        {
            base.InitializeConfiguration();
            base.RunInitializers();

            ObtainConfiguredModules();
        }

        public override void Run(IDictionary<string, string> args = null)
        {
            base.Run(args);

            _log.Info("Starting Modules");

            ObtainConfiguredModules();

            IStatesRepository statesRepository = ServiceLocator.Current.GetInstance<IStatesRepository>();
            IClientState clientState = statesRepository.GetInstance<IClientState>();
            ICryptoService cryptoService = ServiceLocator.Current.GetInstance<ICryptoService>();

            if(args.ContainsKey("secretKey"))
            {
                byte[] secretKey = args["secretKey"].HexStringToByteArray();
                clientState.InitializeAccountBased(secretKey);
                cryptoService.Initialize(secretKey);
            }

            try
            {
                IModulesRepository modulesRepository = ServiceLocator.Current.GetInstance<IModulesRepository>();
                foreach (IModule module in modulesRepository.GetBulkInstances())
                {
                    module.Start();
                }
            }
            finally
            {
                _log.Info("Modules started");
            }
        }

        private void ObtainConfiguredModules()
        {
            if(_modulesConfigured)
            {
                return;
            }

            _modulesConfigured = true;
            IConfigurationService configurationService = ServiceLocator.Current.GetInstance<IConfigurationService>();
            IModularityConfiguration modularityConfiguration = configurationService.Get<IModularityConfiguration>();
            IModulesRepository modulesRepository = ServiceLocator.Current.GetInstance<IModulesRepository>();

            string[] moduleNames = modularityConfiguration.Modules;
            if (moduleNames != null)
            {
                foreach (string moduleName in moduleNames)
                {
                    try
                    {
                        IModule module = modulesRepository.GetInstance(moduleName);
                        modulesRepository.RegisterInstance(module);
                        module.Initialize(_cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Failed to register Module with name '{moduleName}'.", ex);
                    }
                }
            }
        }
    }
}
