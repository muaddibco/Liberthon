using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity;
using Wist.Core.Architecture;

namespace Wist.Client.Wpf
{
    internal class ClientBootstrapper : Bootstrapper
    {
        private CancellationToken token;

        public ClientBootstrapper(CancellationToken token) : base(token)
        {
            this.token = token;
        }
        protected override IEnumerable<string> EnumerateCatalogItems(string rootFolder)
        {
            return base.EnumerateCatalogItems(rootFolder).Concat(new string[] { "Wist.Client.DataModel.dll", "Wist.Client.Common.dll","Wist.Crypto.dll", "Wist.BlockLattice.Core.dll" });
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
        }
    }
}