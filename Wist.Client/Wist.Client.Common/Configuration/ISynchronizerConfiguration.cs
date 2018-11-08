using System;
using System.Collections.Generic;
using System.Text;
using Wist.Core.Configuration;

namespace Wist.Client.Common.Configuration
{
    public interface ISynchronizerConfiguration : IConfigurationSection
    {
        string SyncNodeKey { get; set; }
        string RegistryNodeKey { get; set; }
        string StorageNodeKey { get; set; }
        string[] Nodes { get; set; }
    }
}
