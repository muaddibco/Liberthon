using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Configuration;

namespace Wist.Client.Common.Configuration
{
    [RegisterExtension(typeof(IConfigurationSection), Lifetime = LifetimeManagement.Singleton)]
    public class SynchronizerConfiguration : ConfigurationSectionBase, ISynchronizerConfiguration
    {
        public const string SECTION_NAME = "synchronizer";

        public SynchronizerConfiguration(IApplicationContext applicationContext) : base(applicationContext, SECTION_NAME)
        {
        }

        public string SyncNodeKey { get; set; }
        public string RegistryNodeKey { get; set; }
        public string StorageNodeKey { get; set; }
        public string[] Nodes { get; set; }
    }
}
