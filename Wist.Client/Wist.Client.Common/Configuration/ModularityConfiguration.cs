using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Configuration;

namespace Wist.Client.Common.Configuration
{
    [RegisterExtension(typeof(IConfigurationSection), Lifetime = LifetimeManagement.Singleton)]
    public class ModularityConfiguration : ConfigurationSectionBase, IModularityConfiguration
    {
        public ModularityConfiguration(IApplicationContext applicationContext) : base(applicationContext, "modularity")
        {
        }

        public string[] Modules { get; set; }
    }
}
