using System;
using System.Threading;
using System.Threading.Tasks;
using Wist.BlockLattice.Core.Serializers;
using Wist.Core.Architecture;
using Wist.Proto.Model;

namespace Wist.Client.Common.Communication
{
    [ServiceContract]
    public interface INetworkSynchronizer
    {
        DateTime LastSyncTime { get; set; }

        bool SendData(ISerializer transferBlockSerializer, ISerializer signatureSupportSerializer);

        bool ApproveDataSent();

        void Initialize(CancellationToken cancellationToken);

        void Start();

        SyncBlockDescriptor GetLastSyncBlock();
        TransactionalBlockEssense GetLastBlock(byte[] key);
    }
}