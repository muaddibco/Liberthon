using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel.Nodes;
using Wist.Core.Architecture;
using Wist.Core.Identity;

namespace Wist.BlockLattice.Core.Interfaces
{
    [ServiceContract]
    public interface INodesDataService : IDataService<Node, UniqueKey>
    {
    }
}
