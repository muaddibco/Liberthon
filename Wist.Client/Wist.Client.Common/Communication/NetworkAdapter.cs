using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Wist.BlockLattice.Core.DataModel;
using Wist.Client.Common.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Proto.Model;
using Wist.BlockLattice.Core.Serializers;

namespace Wist.Client.Common.Communication
{
    [RegisterDefaultImplementation(typeof(INetworkAdapter), Lifetime = LifetimeManagement.Singleton)]
    public class NetworkAdapter : INetworkAdapter
    {
        private ISerializersFactory _signatureSupportSerializersFactory;

        private INetworkSynchronizer _networkSynchronizer;

        private IDictionary<IPAddress, bool> _sentAckDictionary;

        public NetworkAdapter(INetworkSynchronizer networkSynchronizer, ISerializersFactory signatureSupportSerializersFactory)
        {
            _signatureSupportSerializersFactory = signatureSupportSerializersFactory;

            _networkSynchronizer = networkSynchronizer;

            _sentAckDictionary = new Dictionary<IPAddress, bool>();
        }

        #region ============ PUBLIC FUNCTIONS =============  

        public SyncBlockDescriptor GetLastSyncBlock()
        {
            return _networkSynchronizer.GetLastSyncBlock();
        }

        public TransactionalBlockEssense GetLastBlock(byte[] senderKey)
        {
            return _networkSynchronizer.GetLastBlock(senderKey);
        }

        public bool SendTransaction(BlockBase block, BlockBase registerBlock)
        {
            ISerializer transactionSerializer = _signatureSupportSerializersFactory.Create(block);

            ISerializer registerSerializer = _signatureSupportSerializersFactory.Create(registerBlock);

            _networkSynchronizer.SendData(transactionSerializer, registerSerializer);

            return true;
        }

        #endregion

        #region ============ PRIVATE FUNCTIONS ============ 


        #endregion


    }
}
