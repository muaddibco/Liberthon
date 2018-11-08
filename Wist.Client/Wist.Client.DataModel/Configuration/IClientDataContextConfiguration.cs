using Wist.Core.Configuration;

namespace Wist.Client.DataModel.Configuration
{
    public interface IClientDataContextConfiguration : IConfigurationSection
    {
        string ConnectionString { get; set; }
    }
}
