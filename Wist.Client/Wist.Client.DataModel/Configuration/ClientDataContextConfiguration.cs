using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Configuration;

namespace Wist.Client.DataModel.Configuration
{
    [RegisterExtension(typeof(IConfigurationSection), Lifetime = LifetimeManagement.Singleton)]
    public class ClientDataContextConfiguration : ConfigurationSectionBase, IClientDataContextConfiguration
    {
        public const string SECTION_NAME = "clientDataContext";

        public ClientDataContextConfiguration(IApplicationContext applicationContext) : base(applicationContext, SECTION_NAME)
        {
        }

        public string ConnectionString { get; set; }
    }
}
