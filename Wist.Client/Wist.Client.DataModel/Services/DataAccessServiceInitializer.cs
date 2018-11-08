using Wist.Core;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;

namespace Wist.Client.DataModel.Services
{
    [RegisterExtension(typeof(IInitializer), Lifetime = LifetimeManagement.Singleton)]
    public class DataAccessServiceInitializer : IInitializer
    {
        private readonly IDataAccessService _dataAccessService;

        public DataAccessServiceInitializer(IDataAccessService dataAccessService)
        {
            _dataAccessService = dataAccessService;
        }
        public ExtensionOrderPriorities Priority => ExtensionOrderPriorities.Normal;

        public bool Initialized { get; private set; }

        public void Initialize()
        {
            if (!Initialized)
            {
                _dataAccessService.Initialize();
                Initialized = true;
            }
        }
    }
}
