using System;
using System.Collections.Generic;
using System.Text;
using Wist.BlockLattice.Core.Enums;

namespace Wist.BlockLattice.Core.DataModel.Transactional
{
    public abstract class TransactionalBlockBase : SyncedLinkedBlockBase
    {
        public override PacketType PacketType => PacketType.Transactional;

        public ulong TagId { get; set; }

        /// <summary>
        /// Up to date funds at last transactional block
        /// </summary>
        public ulong UptodateFunds { get; set; }
        public byte[][] AssetIds { get; set; }
        public ulong[] AssetAmounts { get; set; }
    }
}
