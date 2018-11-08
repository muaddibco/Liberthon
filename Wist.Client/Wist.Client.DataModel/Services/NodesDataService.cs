using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel.Nodes;
using Wist.BlockLattice.Core.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;

namespace Wist.Client.DataModel.Services
{
    [RegisterDefaultImplementation(typeof(INodesDataService), Lifetime = LifetimeManagement.Singleton)]
    public class NodesDataService : INodesDataService
    {
        public void Add(Node item)
        {
        }

        public Node Get(UniqueKey key)
        {
            return null;
        }

        public IEnumerable<Node> GetAll()
        {
            return Enumerable.Empty<Node>();
        }

        public IEnumerable<Node> GetAll(UniqueKey key)
        {
            return Enumerable.Empty<Node>();
        }

        public void Initialize()
        {
        }

        public void Update(UniqueKey key, Node item)
        {
        }
    }
}
