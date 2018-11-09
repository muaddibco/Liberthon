using Wist.Core.Configuration;

namespace Wist.Client.Common.Configuration
{
    public interface IModularityConfiguration : IConfigurationSection
    {
        [Optional]
        string[] Modules { get; set; }
    }
}
