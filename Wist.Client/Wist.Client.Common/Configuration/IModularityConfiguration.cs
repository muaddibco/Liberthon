using Wist.Core.Configuration;

namespace Wist.Client.Common.Configuration
{
    public interface IModularityConfiguration : IConfigurationSection
    {
        string[] Modules { get; set; }
    }
}
