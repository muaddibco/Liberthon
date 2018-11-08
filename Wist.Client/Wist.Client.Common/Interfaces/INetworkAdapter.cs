using Wist.BlockLattice.Core.DataModel;
using Wist.Core.Architecture;
using Wist.Proto.Model;

namespace Wist.Client.Common.Interfaces
{
    [ServiceContract]
    public interface INetworkAdapter
    {
        bool SendTransaction(BlockBase block, BlockBase registerBlock);
        TransactionalBlockEssense GetLastBlock(byte[] senderKey);
        SyncBlockDescriptor GetLastSyncBlock();
    }
}
